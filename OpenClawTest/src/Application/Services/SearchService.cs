using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SkuSearch.Application.DTOs;

namespace SkuSearch.Application.Services;

public class SearchService : ISearchService
{
    private readonly IAiQueryParser     _aiParser;
    private readonly IEmbeddingService  _embedding;
    private readonly IDistributedCache  _cache;
    private readonly ILogger<SearchService> _logger;

    // 由 Infrastructure 层实现并注入
    private readonly IVectorSearchRepository  _vectorRepo;
    private readonly IKeywordSearchRepository _keywordRepo;

    public SearchService(
        IAiQueryParser             aiParser,
        IEmbeddingService          embedding,
        IDistributedCache          cache,
        IVectorSearchRepository    vectorRepo,
        IKeywordSearchRepository   keywordRepo,
        ILogger<SearchService>     logger)
    {
        _aiParser   = aiParser;
        _embedding  = embedding;
        _cache      = cache;
        _vectorRepo = vectorRepo;
        _keywordRepo= keywordRepo;
        _logger     = logger;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. 缓存命中直接返回
        var cacheKey = $"search:{req.Query.Trim().ToLower()}:{req.Category}:{req.TopK}";
        var cached   = await _cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
        {
            _logger.LogDebug("缓存命中: {Key}", cacheKey);
            return JsonSerializer.Deserialize<SearchResponse>(cached)!;
        }

        // 2. AI 意图解析
        var parsed = await _aiParser.ParseAsync(req.Query, ct);
        _logger.LogInformation("AI 解析 [{Query}] → 关键词:{KW} 同义词:{SYN}",
            req.Query,
            string.Join(",", parsed.Keywords),
            string.Join(",", parsed.Synonyms));

        // 3. 并行执行向量搜索 + 全文搜索
        var queryEmbedding = await _embedding.GetEmbeddingAsync(req.Query, ct);

        var vectorTask  = _vectorRepo.SearchAsync(queryEmbedding, req.TopK * 2, req.Category, req.MinScore, ct);
        var keywordTask = _keywordRepo.SearchAsync(parsed, req.TopK * 2, req.Category, ct);

        await Task.WhenAll(vectorTask, keywordTask);

        // 4. RRF 融合排序
        var merged = ReciprocalRankFusion(vectorTask.Result, keywordTask.Result);
        var items  = merged.Take(req.TopK).ToList();

        sw.Stop();
        var response = new SearchResponse(items, parsed, items.Count, sw.ElapsedMilliseconds);

        // 5. 结果缓存 5 分钟
        await _cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            }, ct);

        _logger.LogInformation("搜索完成 [{Query}] → {Count} 条, 耗时 {Ms}ms",
            req.Query, items.Count, sw.ElapsedMilliseconds);

        return response;
    }

    /// <summary>
    /// Reciprocal Rank Fusion 融合排序算法
    /// 两路结果按排名倒数叠加得分，高分优先
    /// </summary>
    private static List<SkuSearchItem> ReciprocalRankFusion(
        List<SkuSearchItem> vectorList,
        List<SkuSearchItem> keywordList,
        int k = 60)
    {
        var scores   = new Dictionary<long, double>();
        var itemsMap = new Dictionary<long, SkuSearchItem>();

        void Accumulate(List<SkuSearchItem> list, string source)
        {
            foreach (var (item, rank) in list.Select((x, i) => (x, i + 1)))
            {
                scores[item.Id] = scores.GetValueOrDefault(item.Id) + 1.0 / (k + rank);
                itemsMap.TryAdd(item.Id, item with { Source = source });
            }
        }

        Accumulate(vectorList,  "vector");
        Accumulate(keywordList, "keyword");

        return scores
            .OrderByDescending(x => x.Value)
            .Select(x => itemsMap[x.Key] with { Source = "merged" })
            .ToList();
    }
}

// 仓储接口（由 Infrastructure 实现）
public interface IVectorSearchRepository
{
    Task<List<SkuSearchItem>> SearchAsync(
        float[] embedding, int topK, string? category,
        float scoreThreshold, CancellationToken ct = default);
}

public interface IKeywordSearchRepository
{
    Task<List<SkuSearchItem>> SearchAsync(
        ParsedQuery parsed, int topK, string? category,
        CancellationToken ct = default);
}
