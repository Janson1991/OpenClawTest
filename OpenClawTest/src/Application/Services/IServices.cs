using SkuSearch.Application.DTOs;

namespace SkuSearch.Application.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct = default);
}

public interface IAiQueryParser
{
    Task<ParsedQuery> ParseAsync(string userInput, CancellationToken ct = default);
}

public interface IEmbeddingService
{
    Task<float[]>       GetEmbeddingAsync(string text, CancellationToken ct = default);
    Task<List<float[]>> GetBatchEmbeddingsAsync(List<string> texts, CancellationToken ct = default);
}
