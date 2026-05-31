using Microsoft.AspNetCore.Mvc;
using ProductSelector.Models;
using ProductSelector.Services;

namespace ProductSelector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecisionController : ControllerBase
{
    private readonly IDecisionSupportService _decisionSupportService;
    private readonly IProductService _productService;
    
    public DecisionController(IDecisionSupportService decisionSupportService, IProductService productService)
    {
        _decisionSupportService = decisionSupportService;
        _productService = productService;
    }
    
    [HttpPost("assess-risk/{productId}")]
    public async Task<ActionResult<RiskAssessment>> AssessRisk(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            return NotFound("商品不存在");
        
        var assessment = await _decisionSupportService.AssessRiskAsync(product);
        return Ok(assessment);
    }
    
    [HttpPost("calculate-profit")]
    public async Task<ActionResult<ProfitCalculation>> CalculateProfit(ProfitCalculationRequest request)
    {
        var calculation = await _decisionSupportService.CalculateProfitAsync(request);
        return Ok(calculation);
    }
    
    [HttpGet("find-opportunities")]
    public async Task<ActionResult<List<MarketOpportunity>>> FindOpportunities([FromQuery] string? category = null)
    {
        var opportunities = await _decisionSupportService.FindOpportunitiesAsync(category);
        return Ok(opportunities);
    }
    
    [HttpPost("analyze-competitors/{productId}")]
    public async Task<ActionResult<CompetitorAnalysis>> AnalyzeCompetitors(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            return NotFound("商品不存在");
        
        var analysis = await _decisionSupportService.AnalyzeCompetitorsAsync(product);
        return Ok(analysis);
    }
    
    [HttpPost("get-recommendation")]
    public async Task<ActionResult<DecisionRecommendation>> GetRecommendation(DecisionRequest request)
    {
        var recommendation = await _decisionSupportService.GetDecisionRecommendationAsync(request);
        return Ok(recommendation);
    }
    
    [HttpPost("batch-assess")]
    public async Task<ActionResult<List<RiskAssessment>>> BatchAssess([FromQuery] int limit = 10)
    {
        var products = await _productService.GetProductsAsync(pageSize: limit);
        var assessments = new List<RiskAssessment>();
        
        foreach (var product in products)
        {
            var assessment = await _decisionSupportService.AssessRiskAsync(product);
            assessments.Add(assessment);
        }
        
        return Ok(assessments.OrderByDescending(a => a.OverallRisk).ToList());
    }
    
    [HttpGet("market-overview")]
    public async Task<ActionResult> GetMarketOverview()
    {
        var products = await _productService.GetProductsAsync(pageSize: 100);
        
        var overview = new
        {
            TotalProducts = products.Count,
            AveragePrice = products.Average(p => p.Price),
            AverageSales = products.Average(p => p.SalesCount),
            AverageRating = products.Average(p => p.Rating),
            PlatformDistribution = products
                .GroupBy(p => p.Platform ?? "未知")
                .ToDictionary(g => g.Key, g => g.Count()),
            CategoryDistribution = products
                .GroupBy(p => p.Category ?? "未知")
                .ToDictionary(g => g.Key, g => g.Count()),
            RiskDistribution = new
            {
                Low = products.Count(p => p.SalesCount > 5000 && p.Rating > 4.5m),
                Medium = products.Count(p => p.SalesCount > 1000 && p.Rating > 4.0m),
                High = products.Count(p => p.SalesCount <= 1000 || p.Rating <= 4.0m)
            }
        };
        
        return Ok(overview);
    }
}
