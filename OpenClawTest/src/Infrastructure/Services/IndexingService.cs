using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SkuSearch.Application.DTOs;
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

        var total     = await Task.Run(() => _db.Skus.Count(s => s.IsActive), ct);
        var processed = 0;
        var failed    = 0;

        _logger.LogInformation("待处理商品总数: {Total}", total);

        for (int page = 0; !ct.IsCancellationRequested; page++)
        {
            var batch = _db.Skus
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .Skip(page * BatchSize)
                .Take(BatchSize)
                .ToList();

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

            // 进度输出
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
    public async Task UpsertSkuAsync(Sku sku, CancellationToken ct = default)
    {
        var text      = BuildSearchText(sku);
        var embedding = await _embedding.GetEmbeddingAsync(text, ct);

        await _qdrant.UpsertAsync(CollectionName,
        [
            new PointStruct
            {
                Id      = new PointId { Num = (ulong)sku.Id },
                Vectors = new Vectors  { Vector = new Vector { Data = { embedding } } },
                Payload =
                {
                    ["sku_code"]  = sku.SkuCode,
                    ["name"]      = sku.Name,
                    ["category"]  = sku.Category ?? "",
                    ["brand"]     = sku.Brand    ?? "",
                    ["tags"]      = sku.Tags     ?? "",
                    ["price"]     = (double)sku.Price,
                    ["image_url"] = sku.ImageUrl ?? ""
                }
            }
        ], cancellationToken: ct);
    }

    /// <summary>删除商品向量</summary>
    public async Task DeleteSkuAsync(long skuId, CancellationToken ct = default)
    {
        await _qdrant.DeleteAsync(CollectionName,
            new PointId { Num = (ulong)skuId },
            cancellationToken: ct);
    }

    /// <summary>获取集合状态信息</summary>
    public async Task<CollectionInfo> GetCollectionInfoAsync()
        => await _qdrant.GetCollectionInfoAsync(CollectionName);

    // ──────────────────────────────────────────────
    //  私有方法
    // ──────────────────────────────────────────────

    private async Task ProcessBatchAsync(List<Sku> batch, CancellationToken ct)
    {
        var texts      = batch.Select(BuildSearchText).ToList();
        var embeddings = await _embedding.GetBatchEmbeddingsAsync(texts, ct);

        var points = batch.Zip(embeddings)
            .Select(pair => new PointStruct
            {
                Id      = new PointId { Num = (ulong)pair.First.Id },
                Vectors = new Vectors  { Vector = new Vector { Data = { pair.Second } } },
                Payload =
                {
                    ["sku_code"]  = pair.First.SkuCode,
                    ["name"]      = pair.First.Name,
                    ["category"]  = pair.First.Category ?? "",
                    ["brand"]     = pair.First.Brand    ?? "",
                    ["tags"]      = pair.First.Tags     ?? "",
                    ["price"]     = (double)pair.First.Price,
                    ["image_url"] = pair.First.ImageUrl ?? ""
                }
            })
            .ToList();

        await _qdrant.UpsertAsync(CollectionName, points, cancellationToken: ct);
    }

    /// <summary>
    /// 拼接用于 Embedding 的文本，字段权重由顺序决定（靠前的权重更高）
    /// </summary>
    private static string BuildSearchText(Sku sku)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(sku.Name))     parts.Add(sku.Name);
        if (!string.IsNullOrWhiteSpace(sku.Category)) parts.Add(sku.Category);
        if (!string.IsNullOrWhiteSpace(sku.Brand))    parts.Add(sku.Brand);
        if (!string.IsNullOrWhiteSpace(sku.Tags))     parts.Add(sku.Tags);

        // 描述太长截断，避免超出 token 限制
        if (!string.IsNullOrWhiteSpace(sku.Description))
            parts.Add(sku.Description[..Math.Min(300, sku.Description.Length)]);

        return string.Join(" ", parts);
        // 例: "三人帐篷 户外装备 牧高笛 帐篷,露营,防雨,三季 适合3-4人家庭野外露营..."
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
                Distance = Distance.Cosine,   // 余弦相似度（推荐文本）
                OnDisk   = true               // 百万级数据写磁盘，节省内存
            });

            // 建 category payload 索引，支持按分类过滤
            await _qdrant.CreatePayloadIndexAsync(
                CollectionName, "category", PayloadSchemaType.Keyword);

            _logger.LogInformation("Qdrant 集合创建成功: {Name} (维度={Size})",
                CollectionName, VectorSize);
        }
        else
        {
            _logger.LogInformation("集合已存在，增量更新模式: {Name}", CollectionName);
        }
    }
}
