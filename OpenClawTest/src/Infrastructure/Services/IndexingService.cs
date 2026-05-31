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
    // public const ulong VectorSize   = 1536;   // OpenAI text-embedding-3-small
    // public const ulong VectorSize   = 1024;   // Ollama bge-m3
    public const ulong  VectorSize     = 2048;    // 你当前使用的模型维度
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

    /// <summary>
    /// 全量建索引（智能增量：只对新增/修改的商品调用 Embedding API）
    /// </summary>
    public async Task BuildFullIndexAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("===== 开始建立向量索引（智能增量模式）=====");
        var sw = Stopwatch.StartNew();

        await EnsureCollectionAsync(recreate: false);

        // 1. 获取 Qdrant 已有的所有 point ID 和 payload（用于对比）
        _logger.LogInformation("加载 Qdrant 已有向量数据...");
        var existingPoints = await LoadExistingPointsAsync(ct);
        _logger.LogInformation("Qdrant 已有 {Count} 条向量", existingPoints.Count);

        // 2. 从 MySQL 读取所有有效商品
        var allSkus = await _db.Skus
            .Where(s => !s.Deleted && s.State == 1)
            .OrderBy(s => s.RecordId)
            .ToListAsync(ct);

        _logger.LogInformation("MySQL 有效商品总数: {Total}", allSkus.Count);

        // 3. 对比差异，筛选需要处理的商品
        var toCreate = new List<SkuDetail>();   // Qdrant 没有的 → 新建
        var toUpdate = new List<SkuDetail>();   // Qdrant 有但数据变了 → 更新
        var skipped  = 0;

        foreach (var sku in allSkus)
        {
            var searchKey = BuildSearchText(sku);

            if (existingPoints.TryGetValue(sku.RecordId, out var existing))
            {
                // 比较搜索文本是否变化（如果商品名称/品牌等改了，需要重新 embedding）
                if (existing.SearchText == searchKey)
                {
                    skipped++;
                    continue; // 完全没变，跳过
                }
                toUpdate.Add(sku);
            }
            else
            {
                toCreate.Add(sku);
            }
        }

        _logger.LogInformation(
            "差异分析完成: 新增 {Create} / 更新 {Update} / 跳过 {Skip}",
            toCreate.Count, toUpdate.Count, skipped);

        // 4. 合并需要处理的商品（新增 + 更新），批量生成 embedding
        var toProcess = toCreate.Concat(toUpdate).ToList();
        var processed = 0;
        var failed    = 0;

        if (toProcess.Any())
        {
            _logger.LogInformation("开始处理 {Count} 个商品的 Embedding...", toProcess.Count);

            for (int i = 0; i < toProcess.Count; i += BatchSize)
            {
                if (ct.IsCancellationRequested) break;

                var batch = toProcess.Skip(i).Take(BatchSize).ToList();

                try
                {
                    await ProcessBatchAsync(batch, ct);
                    processed += batch.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "第 {Page} 批处理失败，跳过继续", i / BatchSize);
                    failed += batch.Count;
                    await Task.Delay(2000, ct);
                    continue;
                }

                var percent = (double)processed / toProcess.Count * 100;
                _logger.LogInformation(
                    "进度: {Processed}/{Total} ({Percent:F1}%)",
                    processed, toProcess.Count, percent);
            }
        }

        sw.Stop();
        _logger.LogInformation(
            "===== 索引完成: 新增+更新 {OK} / 跳过 {Skip} / 失败 {FAIL} / 耗时 {Elapsed} =====",
            processed, skipped, failed, sw.Elapsed);
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
                    ["goods_id"]      = sku.GoodsId,
                    ["sku_id"]        = sku.SkuId,
                    ["shop_id"]       = (long)sku.ShopId,
                    ["name"]          = sku.Name        ?? "",
                    ["spu_item_name"] = sku.SpuItemName ?? "",
                    ["brand_name"]    = sku.BrandName   ?? "",
                    ["goods_type"]    = sku.GoodsType   ?? "",
                    ["check_status"]  = sku.CheckStatus ?? "",
                    ["price_sale"]    = (double)(sku.PriceSale ?? 0),
                    ["price_market"]  = (double)(sku.PriceMarket ?? 0),
                    ["state"]         = (long)sku.State,
                    ["auto_state"]    = (long)sku.AutoState,
                    ["search_text"]   = text             // 存储用于增量对比
                }
            }
        ], cancellationToken: ct);
    }

    /// <summary>删除商品向量</summary>
    public async Task DeleteSkuAsync(long recordId, CancellationToken ct = default)
    {
        await _qdrant.DeleteAsync(
            CollectionName,
           (ulong)recordId,
            cancellationToken: ct);
    }

    /// <summary>获取集合状态信息</summary>
    public async Task<CollectionInfo> GetCollectionInfoAsync()
        => await _qdrant.GetCollectionInfoAsync(CollectionName);

    // ──────────────────────────────────────────────
    //  私有方法
    // ──────────────────────────────────────────────

    /// <summary>
    /// 从 Qdrant 加载所有已存在的点 ID + payload（用于增量对比）
    /// </summary>
    private async Task<Dictionary<long, (string SearchText, string Name)>> LoadExistingPointsAsync(
        CancellationToken ct)
    {
        var result = new Dictionary<long, (string, string)>();

        try
        {
            // scroll 遍历所有点，只取 id + search_text + name payload
            var points = await _qdrant.ScrollAsync(
                CollectionName,
                limit: 1000,  // 分批读取
                withPayload: true,
                cancellationToken: ct);

            while (points.Result.Points.Any())
            {
                foreach (var p in points.Result.Points)
                {
                    var searchText = p.Payload.ContainsKey("search_text")
                        ? p.Payload["search_text"].StringValue
                        : p.Payload.ContainsKey("name")
                            ? p.Payload["name"].StringValue
                            : "";
                    var name = p.Payload.ContainsKey("name")
                        ? p.Payload["name"].StringValue
                        : "";

                    result[(long)p.Id.Num] = (searchText, name);
                }

                // 获取下一批
                if (string.IsNullOrEmpty(points.Result.NextPageOffset)) break;

                points = await _qdrant.ScrollAsync(
                    CollectionName,
                    limit: 1000,
                    offset: points.Result.NextPageOffset,
                    withPayload: true,
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载 Qdrant 已有数据失败，将全量重建");
        }

        return result;
    }

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
                    ["auto_state"]    = (long)pair.First.AutoState,
                    ["search_text"]   = pair.Second            // 存文本用于增量对比
                }
            })
            .ToList();

        await _qdrant.UpsertAsync(CollectionName, points, cancellationToken: ct);
    }

    /// <summary>
    /// 拼接用于 Embedding 的文本
    /// skudetail2 数据特点：Name 是最核心字段，其他字段经常为 NULL
    /// </summary>
    private static string BuildSearchText(SkuDetail sku)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(sku.Name))
            parts.Add(sku.Name);

        if (!string.IsNullOrWhiteSpace(sku.SpuItemName))
            parts.Add(sku.SpuItemName);

        if (!string.IsNullOrWhiteSpace(sku.BrandName) && sku.BrandId > 0)
            parts.Add(sku.BrandName);

        if (!string.IsNullOrWhiteSpace(sku.GoodsType))
            parts.Add(sku.GoodsType);

        return string.Join(" ", parts);
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

            await _qdrant.CreatePayloadIndexAsync(
                CollectionName, "shop_id", PayloadSchemaType.Integer);

            _logger.LogInformation("Qdrant 集合创建成功: {Name} (维度={Size})",
                CollectionName, VectorSize);
        }
        else
        {
            _logger.LogInformation("集合已存在，智能增量模式: {Name}", CollectionName);
        }
    }
}
