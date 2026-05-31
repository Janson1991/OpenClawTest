using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SkuSearch.Application.DTOs;
using SkuSearch.Application.Services;
using SkuSearch.Infrastructure.Data;

namespace SkuSearch.Infrastructure.Repositories;

public class QdrantVectorSearchRepository : IVectorSearchRepository
{
    private readonly QdrantClient _qdrant;

    public QdrantVectorSearchRepository(QdrantClient qdrant) => _qdrant = qdrant;

    public async Task<List<SkuSearchItem>> SearchAsync(
        float[] embedding, int topK, string? category,
        float scoreThreshold, CancellationToken ct = default)
    {
        Filter? filter = category != null
            ? new Filter
              {
                  Must =
                  {
                      new Condition
                      {
                          Field = new FieldCondition
                          {
                              Key   = "category",
                              Match = new Match { Keyword = category }
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
            withPayload:    true,
            cancellationToken: ct);

        return hits.Select(h => new SkuSearchItem(
            Id:       (long)h.Id.Num,
            SkuCode:  h.Payload["sku_code"].StringValue,
            Name:     h.Payload["name"].StringValue,
            Category: h.Payload["category"].StringValue,
            Brand:    h.Payload["brand"].StringValue,
            Price:    (decimal)h.Payload["price"].DoubleValue,
            ImageUrl: h.Payload["image_url"].StringValue,
            Score:    h.Score,
            Source:   "vector"
        )).ToList();
    }
}

public class MySqlKeywordSearchRepository : IKeywordSearchRepository
{
    private readonly AppDbContext _db;

    public MySqlKeywordSearchRepository(AppDbContext db) => _db = db;

    public async Task<List<SkuSearchItem>> SearchAsync(
        ParsedQuery parsed, int topK, string? category,
        CancellationToken ct = default)
    {
        var allKeywords = parsed.Keywords
            .Concat(parsed.Synonyms)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .ToList();

        if (!allKeywords.Any()) return [];

        // MySQL BOOLEAN 全文搜索：每个词加 * 做前缀匹配
        var searchStr = string.Join(" ", allKeywords.Select(k => $"+{k}*"));

        var sql = category != null
            ? @"SELECT id, sku_code, name, category, brand, price, image_url,
                       MATCH(name, description, tags) AGAINST({0} IN BOOLEAN MODE) AS score
                FROM skus
                WHERE MATCH(name, description, tags) AGAINST({0} IN BOOLEAN MODE)
                  AND category = {1}
                  AND is_active = 1
                ORDER BY score DESC LIMIT {2}"
            : @"SELECT id, sku_code, name, category, brand, price, image_url,
                       MATCH(name, description, tags) AGAINST({0} IN BOOLEAN MODE) AS score
                FROM skus
                WHERE MATCH(name, description, tags) AGAINST({0} IN BOOLEAN MODE)
                  AND is_active = 1
                ORDER BY score DESC LIMIT {1}";

        var rawItems = category != null
            ? await _db.SkuSearchProjections
                .FromSqlRaw(sql, searchStr, category, topK)
                .ToListAsync(ct)
            : await _db.SkuSearchProjections
                .FromSqlRaw(sql, searchStr, topK)
                .ToListAsync(ct);

        return rawItems.Select(x => new SkuSearchItem(
            Id:       x.Id,
            SkuCode:  x.SkuCode,
            Name:     x.Name,
            Category: x.Category,
            Brand:    x.Brand,
            Price:    x.Price,
            ImageUrl: x.ImageUrl,
            Score:    (float)x.Score,
            Source:   "keyword"
        )).ToList();
    }
}
