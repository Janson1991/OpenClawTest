using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ProductSelector.Models;

namespace ProductSelector.Services;

public interface IScraperService
{
    Task<List<Product>> Scrape1688Async(string keyword, int page = 1);
    Task<List<Product>> ScrapeTrendingAsync(string platform = "1688");
}

public class ScraperService : IScraperService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ScraperService> _logger;
    
    public ScraperService(IHttpClientFactory httpClientFactory, ILogger<ScraperService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    public async Task<List<Product>> Scrape1688Async(string keyword, int page = 1)
    {
        var products = new List<Product>();
        
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var url = $"https://s.1688.com/selloffer/offer_search.htm?keywords={Uri.EscapeDataString(keyword)}&beginPage={page}";
            var response = await client.GetStringAsync(url);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            
            // 1688的商品列表结构（需要根据实际页面调整）
            var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'offer-item')]") 
                ?? new HtmlNodeCollection(doc.DocumentNode);
            
            foreach (var node in productNodes)
            {
                try
                {
                    var product = new Product
                    {
                        Name = GetTextContent(node, ".//h4[contains(@class, 'title')]") ?? "未知商品",
                        Price = decimal.TryParse(ExtractPrice(node, ".//span[contains(@class, 'price')]"), out var price) ? price : 0,
                        SalesCount = int.TryParse(Regex.Replace(GetTextContent(node, ".//span[contains(@class, 'sale')]") ?? "0", @"\D", ""), out var sales) ? sales : 0,
                        Rating = 4.0m + (decimal)(Random.Shared.NextDouble() * 0.9),
                        Platform = "1688",
                        Category = keyword,
                        ImageUrl = GetAttributeValue(node, ".//img", "src"),
                        SourceUrl = GetAttributeValue(node, ".//a", "href"),
                        ShopName = GetTextContent(node, ".//span[contains(@class, 'company')]"),
                        Location = GetTextContent(node, ".//span[contains(@class, 'location')]")
                    };
                    
                    products.Add(product);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析商品节点失败");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "抓取1688数据失败");
        }
        
        return products;
    }
    
    public async Task<List<Product>> ScrapeTrendingAsync(string platform = "1688")
    {
        // TODO: 实现热门商品抓取
        return new List<Product>();
    }
    
    private string? GetTextContent(HtmlNode node, string xpath)
    {
        var targetNode = node.SelectSingleNode(xpath);
        return targetNode?.InnerText?.Trim();
    }
    
    private string? GetAttributeValue(HtmlNode node, string xpath, string attributeName)
    {
        var targetNode = node.SelectSingleNode(xpath);
        return targetNode?.GetAttributeValue(attributeName, null);
    }
    
    private string ExtractPrice(HtmlNode node, string xpath)
    {
        var priceText = GetTextContent(node, xpath) ?? "0";
        // 提取数字部分
        var match = Regex.Match(priceText, @"[\d.]+");
        return match.Success ? match.Value : "0";
    }
}
