using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkuSearch.Application.DTOs;
using SkuSearch.Application.Services;

namespace SkuSearch.Infrastructure.Services;

/// <summary>
/// 使用 OpenAI / 兼容接口（如 DeepSeek、通义千问）解析用户意图
/// </summary>
public class AiQueryParser : IAiQueryParser
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AiQueryParser> _logger;

    public AiQueryParser(HttpClient http, IConfiguration config, ILogger<AiQueryParser> logger)
    {
        _http   = http;
        _config = config;
        _logger = logger;

        _http.BaseAddress = new Uri(_config["AI:BaseUrl"] ?? "https://api.openai.com");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _config["AI:ApiKey"]);
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<ParsedQuery> ParseAsync(string userInput, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(userInput);

        try
        {
            var requestBody = new
            {
                model    = _config["AI:Model"] ?? "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens  = 512
            };

            var response = await _http.PostAsJsonAsync("/v1/chat/completions", requestBody, ct);
            response.EnsureSuccessStatusCode();

            var json    = await response.Content.ReadAsStringAsync(ct);
            var doc     = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            return ParseResult(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI 解析失败，降级为关键词直接搜索");
            return new ParsedQuery(
                Keywords:   [userInput],
                Synonyms:   [],
                Categories: [],
                Attributes: new Dictionary<string, string>()
            );
        }
    }

    private static string BuildPrompt(string userInput)
    {
        // 用普通字符串拼接，避免 C# 花括号转义问题
        return @"你是电商商品搜索助手，擅长理解用户的购物意图。

用户输入：""" + userInput + @"""

请分析用户意图并返回严格的 JSON 格式（不要有其他任何文字）：
{
  ""keywords"":   [""直接相关词1"", ""直接相关词2""],
  ""synonyms"":   [""同义词或相关商品类型1"", ""同义词或相关商品类型2""],
  ""categories"": [""商品分类1"", ""商品分类2""],
  ""attributes"": {""属性名"": ""属性值""}
}

示例1：用户输入""户外用品""
{""keywords"":[""户外""],""synonyms"":[""帐篷"",""睡袋"",""露营椅"",""防潮垫"",""头灯"",""登山杖"",""野营""],""categories"":[""户外运动"",""露营装备""],""attributes"":{}}

示例2：用户输入""给孩子买双跑鞋""
{""keywords"":[""跑鞋"",""童鞋""],""synonyms"":[""运动鞋"",""儿童跑步鞋"",""儿童训练鞋""],""categories"":[""童鞋"",""运动鞋""],""attributes"":{""适用人群"":""儿童""}}

只返回 JSON，不要 markdown 代码块，不要解释。";
    }

    private static ParsedQuery ParseResult(string jsonContent)
    {
        try
        {
            // 清除可能的 markdown 包裹
            var cleaned = jsonContent.Trim()
                .TrimStart('`').TrimEnd('`')
                .Replace("```json", "").Replace("```", "").Trim();

            var doc = JsonDocument.Parse(cleaned);
            var root = doc.RootElement;

            return new ParsedQuery(
                Keywords:   ParseStringList(root, "keywords"),
                Synonyms:   ParseStringList(root, "synonyms"),
                Categories: ParseStringList(root, "categories"),
                Attributes: ParseStringDict(root,  "attributes")
            );
        }
        catch
        {
            return new ParsedQuery([], [], [], new Dictionary<string, string>());
        }
    }

    private static List<string> ParseStringList(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var el)) return [];
        return el.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static Dictionary<string, string> ParseStringDict(JsonElement root, string key)
    {
        var dict = new Dictionary<string, string>();
        if (!root.TryGetProperty(key, out var el)) return dict;
        foreach (var prop in el.EnumerateObject())
            dict[prop.Name] = prop.Value.GetString() ?? "";
        return dict;
    }
}
