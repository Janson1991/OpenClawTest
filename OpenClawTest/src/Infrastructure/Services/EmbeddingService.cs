using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkuSearch.Application.Services;

namespace SkuSearch.Infrastructure.Services;

/// <summary>
/// OpenAI text-embedding-3-small（在线，效果最好）
/// </summary>
public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAiEmbeddingService(HttpClient http, IConfiguration config)
    {
        _http  = new HttpClient();
        _model = config["AI:EmbeddingModel"] ?? "text-embedding-3-small";

        _http.BaseAddress = new Uri(config["AI:BaseUrl"] ?? "https://api.openai.com");
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config["AI:ApiKey"]);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var results = await GetBatchEmbeddingsAsync([text], ct);
        return results[0];
    }

    public async Task<List<float[]>> GetBatchEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/paas/v4//embeddings", new
        {
            model = _model,
            input = texts
        }, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return result!.Data
            .OrderBy(x => x.Index)
            .Select(x => x.Embedding)
            .ToList();
    }

    private record EmbeddingResponse(List<EmbeddingItem> Data);
    private record EmbeddingItem(int Index, float[] Embedding);
}

/// <summary>
/// Ollama 本地 Embedding（完全离线，推荐 bge-m3 模型）
/// 运行方式: ollama pull bge-m3 && ollama serve
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(HttpClient http, IConfiguration config,
        ILogger<OllamaEmbeddingService> logger)
    {
        _http   = http;
        _model  = config["Ollama:EmbeddingModel"] ?? "bge-m3";
        _logger = logger;

        _http.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434");
        _http.Timeout     = TimeSpan.FromSeconds(60);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var results = await GetBatchEmbeddingsAsync([text], ct);
        return results[0];
    }

    public async Task<List<float[]>> GetBatchEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        // Ollama /api/embed 支持批量输入
        var response = await _http.PostAsJsonAsync("/embeddings", new
        {
            model = _model,
            input = texts
        }, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(ct);
        if (result?.Embeddings == null || result.Embeddings.Count == 0)
            throw new InvalidOperationException("Ollama 返回空向量");

        return result.Embeddings;
    }

    private record OllamaEmbedResponse(List<float[]> Embeddings);
}
