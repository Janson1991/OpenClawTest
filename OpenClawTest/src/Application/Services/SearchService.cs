using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SkuSearch.Application.DTOs;
using SkuSearch.Application.Services;

namespace SkuSearch.Application.Services;

public class SearchService : ISearchService
{
    private readonly IAiQueryParser     _aiParser;
    private readonly IEmbeddingService  _embedding;
    private readonly IVectorSearchRepository  _vectorRepo;
    private readonly IKeywordSearchRepository _keywordRepo;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IAiQueryParser             aiParser,
        IEmbeddingService          embedding,
        IVectorSearchRepository    vectorRepo,
        IKeywordSearchRepository   keywordRepo,
        ILogger<SearchService>     logger)
    {
        _aiParser   = aiParser;
        _embedding  = embedding;
        _vectorRepo = vectorRepo;
        _keywordRepo= keywordRepo;
        _logger     = logger;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. AI 意图解析
        var parsed = await _aiParser.ParseAsync(req.Query, ct);
        _logger.LogInformation("AI 解析 [{Query}] → 关键词:{KW} 同义词:{SYN}",
            req.Query,
            string.Join(",", parsed.Keywords),
            string.Join(",", parsed.Synonyms));

        // 2. 并行执行向量搜索 + 全文搜索
        var queryEmbedding = await _embedding.GetEmbeddingAsync(req.Query, ct);

        var vectorTask  = _vectorRepo.SearchAsync(queryEmbedding, req.TopK * 2, req.ShopId, req.MinScore, ct);
        var keywordTask = _keywordRepo.SearchAsync(parsed, req.TopK * 2, req.ShopId, ct);

        await Task.WhenAll(vectorTask, keywordTask);

        // 3. RRF 融合排序
        var merged = ReciprocalRankFusion(vectorTask.Result, keywordTask.Result);
        var items  = merged.Take(req.TopK).ToList();

        sw.Stop();
        _logger.LogInformation("搜索完成 [{Query}] → {Count} 条, 耗时 {Ms}ms",
            req.Query, items.Count, sw.ElapsedMilliseconds);

        return new SearchResponse(items, parsed, items.Count, sw.ElapsedMilliseconds);
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
                scores[item.RecordId] = scores.GetValueOrDefault(item.RecordId) + 1.0 / (k + rank);
                itemsMap.TryAdd(item.RecordId, item with { Source = source });
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

// 仓储接口
public interface IVectorSearchRepository
{
    Task<List<SkuSearchItem>> SearchAsync(
        float[] embedding, int topK, int? shopId,
        float scoreThreshold, CancellationToken ct = default);
}

public interface IKeywordSearchRepository
{
    Task<List<SkuSearchItem>> SearchAsync(
        ParsedQuery parsed, int topK, int? shopId,
        CancellationToken ct = default);
}
