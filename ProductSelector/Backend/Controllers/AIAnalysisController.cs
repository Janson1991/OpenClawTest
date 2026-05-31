using Microsoft.AspNetCore.Mvc;
using ProductSelector.Models;
using ProductSelector.Services;

namespace ProductSelector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIAnalysisController : ControllerBase
{
    private readonly IAIAnalysisService _aiAnalysisService;
    private readonly IProductService _productService;
    
    public AIAnalysisController(IAIAnalysisService aiAnalysisService, IProductService productService)
    {
        _aiAnalysisService = aiAnalysisService;
        _productService = productService;
    }
    
    [HttpPost("analyze/{productId}")]
    public async Task<ActionResult<string>> AnalyzeProduct(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            return NotFound("商品不存在");
        
        var analysis = await _aiAnalysisService.AnalyzeProductAsync(product);
        return Ok(analysis);
    }
    
    [HttpPost("batch-analyze")]
    public async Task<ActionResult<List<ProductAnalysis>>> BatchAnalyze([FromQuery] int limit = 20)
    {
        var products = await _productService.GetProductsAsync(pageSize: limit);
        var analyses = await _aiAnalysisService.BatchAnalyzeAsync(products);
        return Ok(analyses);
    }
    
    [HttpGet("market-report")]
    public async Task<ActionResult<string>> GetMarketReport([FromQuery] int limit = 50)
    {
        var products = await _productService.GetProductsAsync(pageSize: limit);
        var report = await _aiAnalysisService.GenerateMarketReportAsync(products);
        return Ok(report);
    }
    
    [HttpGet("recommendations")]
    public async Task<ActionResult<List<ProductRecommendation>>> GetRecommendations([FromQuery] int limit = 20)
    {
        var products = await _productService.GetProductsAsync(pageSize: limit);
        var recommendations = await _aiAnalysisService.GetRecommendationsAsync(products);
        return Ok(recommendations);
    }
    
    [HttpPost("compare")]
    public async Task<ActionResult<ProductComparison>> CompareProducts([FromQuery] int product1Id, [FromQuery] int product2Id)
    {
        var product1 = await _productService.GetProductByIdAsync(product1Id);
        var product2 = await _productService.GetProductByIdAsync(product2Id);
        
        if (product1 == null || product2 == null)
            return NotFound("商品不存在");
        
        var comparison = await _aiAnalysisService.CompareProductsAsync(product1, product2);
        return Ok(comparison);
    }
    
    [HttpGet("category-analysis")]
    public async Task<ActionResult> GetCategoryAnalysis([FromQuery] string? category = null)
    {
        var products = await _productService.GetProductsAsync(category: category);
        
        var analysis = products
            .GroupBy(p => p.Category ?? "未知")
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count(),
                AveragePrice = g.Average(p => p.Price),
                AverageSales = g.Average(p => p.SalesCount),
                AverageRating = g.Average(p => p.Rating),
                TopProduct = g.OrderByDescending(p => p.SalesCount).FirstOrDefault()?.Name
            })
            .OrderByDescending(a => a.Count)
            .ToList();
        
        return Ok(analysis);
    }
    
    [HttpGet("price-trend")]
    public async Task<ActionResult> GetPriceTrend([FromQuery] int productId)
    {
        // TODO: 实现价格趋势分析
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            return NotFound("商品不存在");
        
        var trend = new
        {
            ProductId = product.Id,
            ProductName = product.Name,
            CurrentPrice = product.Price,
            OriginalPrice = product.OriginalPrice,
            Trend = "下降", // TODO: 从PriceHistory表获取真实数据
            SuggestedAction = product.Price < (product.OriginalPrice ?? product.Price) * 0.7m 
                ? "建议关注" 
                : "价格正常"
        };
        
        return Ok(trend);
    }
}
