using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SkuSearch.Application.DTOs;
using SkuSearch.Application.Services;
using SkuSearch.Infrastructure.Data;
using SkuSearch.Infrastructure.Services;

namespace SkuSearch.Infrastructure.Repositories;

public class QdrantVectorSearchRepository : IVectorSearchRepository
{
    private readonly QdrantClient _qdrant;

    public QdrantVectorSearchRepository(QdrantClient qdrant) => _qdrant = qdrant;

    public async Task<List<SkuSearchItem>> SearchAsync(
        float[] embedding, int topK, int? shopId,
        float scoreThreshold, CancellationToken ct = default)
    {
        Filter? filter = shopId != null
            ? new Filter
              {
                  Must =
                  {
                      new Condition
                      {
                          Field = new FieldCondition
                          {
                              Key   = "shop_id",
                              Match = new Match { Integer = shopId.Value }
                          }
                      }
                  }
              }
            : null;

        var hits = await _qdrant.SearchAsync(
            collectionName: IndexingService.CollectionName,
            vector:         embedding,
            filter:         filter,
            limit:          (ulong)topK,
            scoreThreshold: scoreThreshold,
            payloadSelector:    true,
            cancellationToken: ct);

        return hits.Select(h => new SkuSearchItem(
            RecordId:    (long)h.Id.Num,
            GoodsId:     h.Payload["goods_id"].StringValue,
            SkuId:       h.Payload["sku_id"].StringValue,
            ShopId:      (int)h.Payload["shop_id"].IntegerValue,
            Name:        h.Payload["name"].StringValue,
            SpuItemName: h.Payload["spu_item_name"].StringValue,
            BrandName:   h.Payload["brand_name"].StringValue,
            PriceSale:   (decimal)h.Payload["price_sale"].DoubleValue,
            PriceMarket: (decimal)h.Payload["price_market"].DoubleValue,
            GoodsType:   h.Payload["goods_type"].StringValue,
            CheckStatus: h.Payload["check_status"].StringValue,
            State:       (byte)h.Payload["state"].IntegerValue,
            AutoState:   (byte)h.Payload["auto_state"].IntegerValue,
            Score:       h.Score,
            Source:      "vector"
        )).ToList();
    }
}

public class MySqlKeywordSearchRepository : IKeywordSearchRepository
{
    private readonly AppDbContext _db;

    public MySqlKeywordSearchRepository(AppDbContext db) => _db = db;

    public async Task<List<SkuSearchItem>> SearchAsync(
        ParsedQuery parsed, int topK, int? shopId,
        CancellationToken ct = default)
    {
        var allKeywords = parsed.Keywords
            .Concat(parsed.Synonyms)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .ToList();

        if (!allKeywords.Any()) return [];

        // skudetail2 的 name 和 spu_item_name 做 ngram 全文搜索
        var searchStr = string.Join(" ", allKeywords.Select(k => $"+{k}*"));

        // 动态拼接 SQL（避免 SQL 注入，参数化搜索词）
        var sql = shopId != null
            ? @"SELECT RecordId, GoodsId, SkuId,  ShopId,
                       name AS Name,  SpuItemName,  BrandName, '' as ImageUrl,
                        PriceSale,  PriceMarket,
                       GoodsType, CheckStatus,
                       state AS `State`, AutoState,
                       MATCH(name, SpuItemName) AGAINST({0} IN BOOLEAN MODE) AS Score
                FROM skudetail2
                WHERE MATCH(name, SpuItemName) AGAINST({0} IN BOOLEAN MODE)
                  AND ShopId = {1}
                  AND deleted = 0
                  AND state = 1
                ORDER BY Score DESC
                LIMIT {2}"
            : @"SELECT  RecordId,  GoodsId,  SkuId,  ShopId,
                       name AS Name,  SpuItemName,  BrandName, '' as ImageUrl,
                       PriceSale,  PriceMarket,
                       GoodsType,  CheckStatus,
                       state AS `State`,  AutoState,
                       MATCH(name, SpuItemName) AGAINST({0} IN BOOLEAN MODE) AS Score
                FROM skudetail2
                WHERE MATCH(name, SpuItemName) AGAINST({0} IN BOOLEAN MODE)
                  AND deleted = 0
                  AND state = 1
                ORDER BY Score DESC
                LIMIT {1}";

        var rawItems = shopId != null
            ? await _db.SkuSearchProjections
                .FromSqlRaw(sql, searchStr, shopId, topK)
                .ToListAsync(ct)
            : await _db.SkuSearchProjections
                .FromSqlRaw(sql, searchStr, topK)
                .ToListAsync(ct);

        return rawItems.Select(x => new SkuSearchItem(
            RecordId:    x.RecordId,
            GoodsId:     x.GoodsId,
            SkuId:       x.SkuId,
            ShopId:      x.ShopId,
            Name:        x.Name,
            SpuItemName: x.SpuItemName,
            BrandName:   x.BrandName,
            PriceSale:   x.PriceSale,
            PriceMarket: null,
            GoodsType:   null,
            CheckStatus: null,
            State:       1,
            AutoState:   1,
            Score:       (float)x.Score,
            Source:      "keyword"
        )).ToList();
    }
}
