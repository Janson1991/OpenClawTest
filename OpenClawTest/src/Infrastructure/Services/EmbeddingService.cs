using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkuSearch.Application.Services;

namespace SkuSearch.Infrastructure.Services;

/// <summary>
/// Ollama 本地 Embedding（完全离线）
/// 运行: ollama pull bge-m3 && ollama serve
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
        _http.Timeout     = TimeSpan.FromSeconds(120);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var results = await GetBatchEmbeddingsAsync([text], ct);
        return results[0];
    }

    public async Task<List<float[]>> GetBatchEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        // Ollama 有两个版本的 API，自动适配：
        //   新版 (0.4.0+): POST /api/embed  { model, input: [...] }
        //   旧版:          POST /api/embeddings  { model, prompt: "..." }

        try
        {
            // 先试新版 API（支持批量）
            return await CallNewApiAsync(texts, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("新版 /api/embed 不可用，降级到旧版 /api/embeddings");
            return await CallOldApiAsync(texts, ct);
        }
    }

    /// <summary>新版 API: POST /api/embed（支持批量 input）</summary>
    private async Task<List<float[]>> CallNewApiAsync(List<string> texts, CancellationToken ct)
    {
        var requestBody = new { model = _model, input = texts };
        var response = await _http.PostAsJsonAsync("/api/embed", requestBody, ct);

        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Ollama /api/embed 返回 {response.StatusCode}: {content}");

        var result = System.Text.Json.JsonSerializer.Deserialize<OllamaNewEmbedResponse>(content);
        if (result?.Embeddings == null || result.Embeddings.Count == 0)
            throw new InvalidOperationException($"Ollama 返回空向量。响应: {content[..Math.Min(200, content.Length)]}");

        return result.Embeddings;
    }

    /// <summary>旧版 API: POST /api/embeddings（逐条处理）</summary>
    private async Task<List<float[]>> CallOldApiAsync(List<string> texts, CancellationToken ct)
    {
        var results = new List<float[]>();

        foreach (var text in texts)
        {
            var requestBody = new { model = _model, prompt = text };
            var response = await _http.PostAsJsonAsync("/api/embeddings", requestBody, ct);

            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Ollama /api/embeddings 返回 {response.StatusCode}: {content}");

            var result = System.Text.Json.JsonSerializer.Deserialize<OllamaOldEmbedResponse>(content);
            if (result?.Embedding == null || result.Embedding.Length == 0)
                throw new InvalidOperationException($"Ollama 返回空向量。响应: {content[..Math.Min(200, content.Length)]}");

            results.Add(result.Embedding);
        }

        return results;
    }

    // 新版响应: { "embeddings": [[0.1, 0.2, ...]] }
    private record OllamaNewEmbedResponse(List<float[]> Embeddings);

    // 旧版响应: { "embedding": [0.1, 0.2, ...] }
    private record OllamaOldEmbedResponse(float[] Embedding);
}
