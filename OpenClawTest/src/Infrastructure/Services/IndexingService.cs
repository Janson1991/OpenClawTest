using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SkuSearch.Application.Services;
using SkuSearch.Domain.Entities;
using SkuSearch.Infrastructure.Data;

namespace SkuSearch.Infrastructure.Services;

public class IndexingService
{
    private readonly AppDbContext     _db;
    private readonly QdrantClient     _qdrant;
    private readonly IEmbeddingService _embedding;
    private readonly ILogger<IndexingService> _logger;

    public const string CollectionName = "sku_vectors";
    public const ulong  VectorSize     = 1536;   // OpenAI text-embedding-3-small
    // public const ulong VectorSize   = 1024;   // Ollama bge-m3 用这个
    private const int   BatchSize      = 200;

    public IndexingService(
        AppDbContext db,
        QdrantClient qdrant,
        IEmbeddingService embedding,
        ILogger<IndexingService> logger)
    {
        _db        = db;
        _qdrant    = qdrant;
        _embedding = embedding;
        _logger    = logger;
    }

    /// <summary>全量建索引（首次或重建）</summary>
    public async Task BuildFullIndexAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("===== 开始全量建立向量索引 =====");
        var sw = Stopwatch.StartNew();

        await EnsureCollectionAsync(recreate: false);

        // skudetail2: 未删除 + 有名称的商品（State 和 AutoState 各种组合都可能有效）
        var total = await Task.Run(() =>
            _db.Skus.CountAsync(s =>
                !s.Deleted &&
                s.State == 1, ct), ct);

        var processed = 0;
        var failed    = 0;

        _logger.LogInformation("待处理商品总数: {Total}", total);

        for (int page = 0; !ct.IsCancellationRequested; page++)
        {
            var batch = await _db.Skus
                .Where(s => !s.Deleted && s.State == 1)
                .OrderBy(s => s.RecordId)
                .Skip(page * BatchSize)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (!batch.Any()) break;

            try
            {
                await ProcessBatchAsync(batch, ct);
                processed += batch.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "第 {Page} 批处理失败，跳过继续", page);
                failed += batch.Count;
                await Task.Delay(2000, ct);
                continue;
            }

            var percent = (double)processed / total * 100;
            var eta = processed > 0
                ? TimeSpan.FromSeconds(sw.Elapsed.TotalSeconds / processed * (total - processed))
                : TimeSpan.Zero;

            _logger.LogInformation(
                "进度: {Processed}/{Total} ({Percent:F1}%) | 耗时: {Elapsed} | 预计剩余: {ETA}",
                processed, total, percent,
                sw.Elapsed.ToString(@"hh\:mm\:ss"),
                eta.ToString(@"hh\:mm\:ss"));
        }

        sw.Stop();
        _logger.LogInformation(
            "===== 索引完成: 成功 {OK} / 失败 {FAIL} / 总耗时 {Elapsed} =====",
            processed, failed, sw.Elapsed);
    }

    /// <summary>增量更新单个商品</summary>
    public async Task UpsertSkuAsync(SkuDetail sku, CancellationToken ct = default)
    {
        var text      = BuildSearchText(sku);
        var embedding = await _embedding.GetEmbeddingAsync(text, ct);

        await _qdrant.UpsertAsync(CollectionName,
        [
            new PointStruct
            {
                Id      = new PointId { Num = (ulong)sku.RecordId },
                Vectors = new Vectors  { Vector = new Vector { Data = { embedding } } },
                Payload =
                {
                    ["goods_id"]     = sku.GoodsId,
                    ["sku_id"]       = sku.SkuId,
                    ["shop_id"]      = (long)sku.ShopId,
                    ["name"]         = sku.Name       ?? "",
                    ["spu_item_name"]= sku.SpuItemName ?? "",
                    ["brand_name"]   = sku.BrandName  ?? "",
                    ["goods_type"]   = sku.GoodsType  ?? "",
                    ["check_status"] = sku.CheckStatus ?? "",
                    ["price_sale"]   = (double)(sku.PriceSale ?? 0),
                    ["price_market"] = (double)(sku.PriceMarket ?? 0),
                    ["state"]        = (long)sku.State,
                    ["auto_state"]   = (long)sku.AutoState
                }
            }
        ], cancellationToken: ct);
    }

    /// <summary>删除商品向量</summary>
    public async Task DeleteSkuAsync(long recordId, CancellationToken ct = default)
    {
        await _qdrant.DeleteAsync(
            CollectionName,
            new PointIdsList
            {
                Ids = { new PointId { Num = (ulong)recordId } }
            },
            cancellationToken: ct);
    }

    /// <summary>获取集合状态信息</summary>
    public async Task<CollectionInfo> GetCollectionInfoAsync()
        => await _qdrant.GetCollectionInfoAsync(CollectionName);

    // ──────────────────────────────────────────────
    //  私有方法
    // ──────────────────────────────────────────────

    private async Task ProcessBatchAsync(List<SkuDetail> batch, CancellationToken ct)
    {
        var texts      = batch.Select(BuildSearchText).ToList();
        var embeddings = await _embedding.GetBatchEmbeddingsAsync(texts, ct);

        var points = batch.Zip(embeddings)
            .Select(pair => new PointStruct
            {
                Id      = new PointId { Num = (ulong)pair.First.RecordId },
                Vectors = new Vectors  { Vector = new Vector { Data = { pair.Second } } },
                Payload =
                {
                    ["goods_id"]      = pair.First.GoodsId,
                    ["sku_id"]        = pair.First.SkuId,
                    ["shop_id"]       = (long)pair.First.ShopId,
                    ["name"]          = pair.First.Name        ?? "",
                    ["spu_item_name"] = pair.First.SpuItemName ?? "",
                    ["brand_name"]    = pair.First.BrandName   ?? "",
                    ["goods_type"]    = pair.First.GoodsType   ?? "",
                    ["check_status"]  = pair.First.CheckStatus ?? "",
                    ["price_sale"]    = (double)(pair.First.PriceSale ?? 0),
                    ["price_market"]  = (double)(pair.First.PriceMarket ?? 0),
                    ["state"]         = (long)pair.First.State,
                    ["auto_state"]    = (long)pair.First.AutoState
                }
            })
            .ToList();

        await _qdrant.UpsertAsync(CollectionName, points, cancellationToken: ct);
    }

    /// <summary>
    /// 拼接用于 Embedding 的文本
    /// skudetail2 数据特点：Name 是最核心字段，其他字段经常为 NULL
    /// 搜索文本 = Name（完整商品标题）+ SpuItemName + BrandName + GoodsType
    /// </summary>
    private static string BuildSearchText(SkuDetail sku)
    {
        var parts = new List<string>();

        // Name 是商品全标题，包含品牌、品类、规格等所有信息，最重要
        if (!string.IsNullOrWhiteSpace(sku.Name))
            parts.Add(sku.Name);

        // SpuItemName 补充规格信息
        if (!string.IsNullOrWhiteSpace(sku.SpuItemName))
            parts.Add(sku.SpuItemName);

        // BrandName 补充品牌（有些 Name 里已包含品牌）
        if (!string.IsNullOrWhiteSpace(sku.BrandName) && sku.BrandId > 0)
            parts.Add(sku.BrandName);

        // GoodsType 补充类型
        if (!string.IsNullOrWhiteSpace(sku.GoodsType))
            parts.Add(sku.GoodsType);

        return string.Join(" ", parts);
        // 例: "言艺茶具套装紫砂功夫茶具陶瓷旅行整套茶壶茶杯茶海实木茶盘茶台"
        // 例: "威克多Victor 胜利纳米7羽毛球拍 高刚碳素3U全面型羽毛球拍单拍 HX-7SP 金色 已穿线"
    }

    private async Task EnsureCollectionAsync(bool recreate = false)
    {
        var collections = await _qdrant.ListCollectionsAsync();
        var exists      = collections.Any(c => c == CollectionName);

        if (exists && recreate)
        {
            _logger.LogWarning("删除旧集合: {Name}", CollectionName);
            await _qdrant.DeleteCollectionAsync(CollectionName);
            exists = false;
        }

        if (!exists)
        {
            await _qdrant.CreateCollectionAsync(CollectionName, new VectorParams
            {
                Size     = VectorSize,
                Distance = Distance.Cosine,
                OnDisk   = true
            });

            // shop_id payload 索引，支持按店铺过滤
            await _qdrant.CreatePayloadIndexAsync(
                CollectionName, "shop_id", PayloadSchemaType.Integer);

            _logger.LogInformation("Qdrant 集合创建成功: {Name} (维度={Size})",
                CollectionName, VectorSize);
        }
        else
        {
            _logger.LogInformation("集合已存在，增量更新模式: {Name}", CollectionName);
        }
    }
}
