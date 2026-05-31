using System.Text;
using ProductSelector.Models;

namespace ProductSelector.Services;

public interface IDecisionSupportService
{
    Task<RiskAssessment> AssessRiskAsync(Product product);
    Task<ProfitCalculation> CalculateProfitAsync(ProfitCalculationRequest request);
    Task<List<MarketOpportunity>> FindOpportunitiesAsync(string? category = null);
    Task<CompetitorAnalysis> AnalyzeCompetitorsAsync(Product product);
    Task<DecisionRecommendation> GetDecisionRecommendationAsync(DecisionRequest request);
}

public class DecisionSupportService : IDecisionSupportService
{
    private readonly ILogger<DecisionSupportService> _logger;
    
    public DecisionSupportService(ILogger<DecisionSupportService> logger)
    {
        _logger = logger;
    }
    
    public async Task<RiskAssessment> AssessRiskAsync(Product product)
    {
        var riskFactors = new List<RiskFactor>();
        decimal overallRisk = 0;
        
        // 竞争度分析
        var competitionRisk = AssessCompetitionRisk(product);
        riskFactors.Add(competitionRisk);
        overallRisk += competitionRisk.Score;
        
        // 价格风险分析
        var priceRisk = AssessPriceRisk(product);
        riskFactors.Add(priceRisk);
        overallRisk += priceRisk.Score;
        
        // 销量稳定性分析
        var salesRisk = AssessSalesRisk(product);
        riskFactors.Add(salesRisk);
        overallRisk += salesRisk.Score;
        
        // 评价质量分析
        var reviewRisk = AssessReviewRisk(product);
        riskFactors.Add(reviewRisk);
        overallRisk += reviewRisk.Score;
        
        // 计算总体风险等级
        overallRisk /= riskFactors.Count;
        var riskLevel = overallRisk switch
        {
            < 30 => RiskLevel.Low,
            < 60 => RiskLevel.Medium,
            < 80 => RiskLevel.High,
            _ => RiskLevel.VeryHigh
        };
        
        return new RiskAssessment
        {
            ProductId = product.Id,
            ProductName = product.Name,
            OverallRisk = overallRisk,
            RiskLevel = riskLevel,
            RiskFactors = riskFactors,
            Recommendations = GenerateRiskRecommendations(riskLevel, riskFactors),
            AnalysisTime = DateTime.UtcNow
        };
    }
    
    public async Task<ProfitCalculation> CalculateProfitAsync(ProfitCalculationRequest request)
    {
        // 基础成本计算
        var sellingPrice = request.SellingPrice;
        var costPrice = request.CostPrice;
        var quantity = request.Quantity;
        
        // 平台费用（假设1688）
        var platformFee = sellingPrice * 0.05m; // 5%平台费
        
        // 物流成本（假设每件5元）
        var shippingCost = 5m * quantity;
        
        // 广告费用（假设销售额的10%）
        var advertisingCost = sellingPrice * quantity * 0.10m;
        
        // 退货成本（假设退货率5%，每件退货成本10元）
        var returnRate = 0.05m;
        var returnCostPerItem = 10m;
        var returnCost = quantity * returnRate * returnCostPerItem;
        
        // 包装成本（假设每件2元）
        var packagingCost = 2m * quantity;
        
        // 总成本
        var totalCost = (costPrice * quantity) + platformFee + shippingCost + 
                       advertisingCost + returnCost + packagingCost;
        
        // 总收入
        var totalRevenue = sellingPrice * quantity;
        
        // 净利润
        var netProfit = totalRevenue - totalCost;
        
        // 利润率
        var profitMargin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0;
        
        // ROI
        var roi = totalCost > 0 ? (netProfit / totalCost) * 100 : 0;
        
        // 盈亏平衡点
        var breakEvenQuantity = totalCost > 0 && sellingPrice > costPrice 
            ? (decimal)Math.Ceiling((double)(totalCost / (sellingPrice - costPrice))) 
            : 0;
        
        return new ProfitCalculation
        {
            SellingPrice = sellingPrice,
            CostPrice = costPrice,
            Quantity = quantity,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            NetProfit = netProfit,
            ProfitMargin = profitMargin,
            ROI = roi,
            BreakEvenQuantity = breakEvenQuantity,
            CostBreakdown = new CostBreakdown
            {
                ProductCost = costPrice * quantity,
                PlatformFee = platformFee,
                ShippingCost = shippingCost,
                AdvertisingCost = advertisingCost,
                ReturnCost = returnCost,
                PackagingCost = packagingCost
            },
            Recommendations = GenerateProfitRecommendations(profitMargin, roi)
        };
    }
    
    public async Task<List<MarketOpportunity>> FindOpportunitiesAsync(string? category = null)
    {
        var opportunities = new List<MarketOpportunity>();
        
        // 模拟市场机会发现（实际应该接入真实数据）
        var sampleOpportunities = new List<MarketOpportunity>
        {
            new MarketOpportunity
            {
                Category = "智能家居配件",
                OpportunityType = "趋势上升",
                Score = 85,
                Description = "智能家居配件搜索量月增30%，但卖家数量仅增10%",
                PotentialProfit = "高",
                CompetitionLevel = "中",
                TimeWindow = "3-6个月",
                RiskLevel = "低",
                ActionSuggestion = "建议尽快进入，抢占先机"
            },
            new MarketOpportunity
            {
                Category = "宠物智能用品",
                OpportunityType = "蓝海市场",
                Score = 78,
                Description = "宠物经济持续增长，智能用品细分市场尚未饱和",
                PotentialProfit = "高",
                CompetitionLevel = "低",
                TimeWindow = "6-12个月",
                RiskLevel = "中",
                ActionSuggestion = "可以小规模测试，验证市场"
            },
            new MarketOpportunity
            {
                Category = "户外运动装备",
                OpportunityType = "季节性机会",
                Score = 72,
                Description = "夏季来临，户外运动装备需求即将爆发",
                PotentialProfit = "中",
                CompetitionLevel = "高",
                TimeWindow = "1-3个月",
                RiskLevel = "中",
                ActionSuggestion = "需要差异化，避免价格战"
            }
        };
        
        if (!string.IsNullOrEmpty(category))
        {
            sampleOpportunities = sampleOpportunities
                .Where(o => o.Category.Contains(category))
                .ToList();
        }
        
        return await Task.FromResult(sampleOpportunities.OrderByDescending(o => o.Score).ToList());
    }
    
    public async Task<CompetitorAnalysis> AnalyzeCompetitorsAsync(Product product)
    {
        var competitors = new List<Competitor>();
        
        // 模拟竞品分析（实际应该接入真实数据）
        competitors.Add(new Competitor
        {
            Name = "竞品A",
            Price = product.Price * 0.9m,
            SalesCount = product.SalesCount * 1.2m,
            Rating = product.Rating - 0.1m,
            Strengths = new List<string> { "价格更低", "销量更高" },
            Weaknesses = new List<string> { "评分略低", "评价较少" },
            Strategy = "低价策略，走量"
        });
        
        competitors.Add(new Competitor
        {
            Name = "竞品B",
            Price = product.Price * 1.1m,
            SalesCount = product.SalesCount * 0.8m,
            Rating = product.Rating + 0.2m,
            Strengths = new List<string> { "评分更高", "品质更好" },
            Weaknesses = new List<string> { "价格更高", "销量一般" },
            Strategy = "品质路线，高端定位"
        });
        
        return new CompetitorAnalysis
        {
            ProductId = product.Id,
            ProductName = product.Name,
            CompetitorCount = 15, // 假设总竞品数
            Competitors = competitors,
            MarketPosition = "跟随者",
            DifferentiationOpportunities = new List<string>
            {
                "可以强调品质优势",
                "可以做细分市场",
                "可以提供更好的服务"
            },
            PricingRecommendation = "建议定价在竞品A和竞品B之间"
        };
    }
    
    public async Task<DecisionRecommendation> GetDecisionRecommendationAsync(DecisionRequest request)
    {
        var product = request.Product;
        var budget = request.Budget;
        var riskTolerance = request.RiskTolerance;
        var targetProfit = request.TargetProfit;
        
        // 进行多维度分析
        var riskAssessment = await AssessRiskAsync(product);
        var profitCalculation = await CalculateProfitAsync(new ProfitCalculationRequest
        {
            SellingPrice = product.Price,
            CostPrice = product.Price * 0.6m, // 假设成本价是售价的60%
            Quantity = 100
        });
        
        // 综合评估
        var score = 0m;
        var reasons = new List<string>();
        var warnings = new List<string>();
        
        // 风险评估得分
        if (riskAssessment.RiskLevel == RiskLevel.Low)
        {
            score += 30m;
            reasons.Add("风险较低");
        }
        else if (riskAssessment.RiskLevel == RiskLevel.Medium)
        {
            score += 20m;
            reasons.Add("风险中等");
        }
        else
        {
            score += 10m;
            warnings.Add("风险较高");
        }
        
        // 利润评估得分
        if (profitCalculation.ProfitMargin > 20m)
        {
            score += 30m;
            reasons.Add($"利润率高 ({profitCalculation.ProfitMargin:F1}%)");
        }
        else if (profitCalculation.ProfitMargin > 10m)
        {
            score += 20m;
            reasons.Add($"利润率中等 ({profitCalculation.ProfitMargin:F1}%)");
        }
        else
        {
            score += 10m;
            warnings.Add($"利润率偏低 ({profitCalculation.ProfitMargin:F1}%)");
        }
        
        // 预算匹配度
        var requiredInvestment = profitCalculation.CostBreakdown.ProductCost + 
                               profitCalculation.CostBreakdown.ShippingCost;
        if (budget >= requiredInvestment * 2)
        {
            score += 20m;
            reasons.Add("预算充足");
        }
        else if (budget >= requiredInvestment)
        {
            score += 15m;
            reasons.Add("预算基本够用");
        }
        else
        {
            score += 5m;
            warnings.Add("预算不足");
        }
        
        // 市场机会得分
        score += 20m; // 简化处理
        
        // 生成决策建议
        var decision = score switch
        {
            >= 80m => DecisionType.StrongBuy,
            >= 60m => DecisionType.Buy,
            >= 40m => DecisionType.Cautious,
            >= 20m => DecisionType.Watch,
            _ => DecisionType.Avoid
        };
        
        return new DecisionRecommendation
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Decision = decision,
            Score = score,
            Reasons = reasons,
            Warnings = warnings,
            DetailedAnalysis = new DetailedAnalysis
            {
                RiskAssessment = riskAssessment,
                ProfitCalculation = profitCalculation
            },
            ActionPlan = GenerateActionPlan(decision, product),
            TimeSensitive = decision == DecisionType.StrongBuy,
            AnalysisTime = DateTime.UtcNow
        };
    }
    
    private RiskFactor AssessCompetitionRisk(Product product)
    {
        // 简化的竞争度评估
        var competitionScore = product.SalesCount > 10000 ? 70m : 
                              product.SalesCount > 5000 ? 50m : 30m;
        
        return new RiskFactor
        {
            Name = "竞争度",
            Score = competitionScore,
            Description = product.SalesCount > 10000 ? "竞争激烈" : "竞争适中",
            Details = $"销量 {product.SalesCount:N0}，市场竞争程度中等"
        };
    }
    
    private RiskFactor AssessPriceRisk(Product product)
    {
        decimal priceRisk = 30m;
        
        if (product.OriginalPrice.HasValue && product.OriginalPrice > 0)
        {
            var discount = 1 - (product.Price / product.OriginalPrice.Value);
            if (discount > 0.5m)
                priceRisk = 60m; // 折扣太大，可能有问题
            else if (discount > 0.3m)
                priceRisk = 40m;
        }
        
        return new RiskFactor
        {
            Name = "价格风险",
            Score = priceRisk,
            Description = priceRisk > 50 ? "价格风险较高" : "价格风险适中",
            Details = $"当前价格 ¥{product.Price:F2}"
        };
    }
    
    private RiskFactor AssessSalesRisk(Product product)
    {
        decimal salesRisk = product.SalesCount < 1000 ? 60m : 
                           product.SalesCount < 5000 ? 40m : 20m;
        
        return new RiskFactor
        {
            Name = "销量风险",
            Score = salesRisk,
            Description = salesRisk > 50 ? "销量不稳定" : "销量稳定",
            Details = $"销量 {product.SalesCount:N0}"
        };
    }
    
    private RiskFactor AssessReviewRisk(Product product)
    {
        decimal reviewRisk = 30m;
        
        if (product.Rating < 4.0m)
            reviewRisk = 70m;
        else if (product.Rating < 4.5m)
            reviewRisk = 50m;
        else if (product.ReviewCount < 100)
            reviewRisk = 40m;
        
        return new RiskFactor
        {
            Name = "评价风险",
            Score = reviewRisk,
            Description = reviewRisk > 50 ? "评价风险较高" : "评价风险较低",
            Details = $"评分 {product.Rating}/5.0，评价数 {product.ReviewCount}"
        };
    }
    
    private List<string> GenerateRiskRecommendations(RiskLevel riskLevel, List<RiskFactor> riskFactors)
    {
        var recommendations = new List<string>();
        
        if (riskLevel == RiskLevel.Low)
        {
            recommendations.Add("风险较低，可以考虑进入");
            recommendations.Add("建议小批量测试市场反应");
        }
        else if (riskLevel == RiskLevel.Medium)
        {
            recommendations.Add("风险适中，需要谨慎评估");
            recommendations.Add("建议先小规模试水");
            recommendations.Add("密切关注市场变化");
        }
        else
        {
            recommendations.Add("风险较高，建议观望");
            recommendations.Add("如果要进入，需要充分准备");
            recommendations.Add("建议寻找差异化切入点");
        }
        
        // 基于具体风险因素的建议
        foreach (var factor in riskFactors)
        {
            if (factor.Score > 60)
            {
                recommendations.Add($"注意：{factor.Name}风险较高 - {factor.Details}");
            }
        }
        
        return recommendations;
    }
    
    private List<string> GenerateProfitRecommendations(decimal profitMargin, decimal roi)
    {
        var recommendations = new List<string>();
        
        if (profitMargin > 30m)
        {
            recommendations.Add("利润率优秀，可以考虑扩大规模");
        }
        else if (profitMargin > 20m)
        {
            recommendations.Add("利润率良好，可以稳定经营");
        }
        else if (profitMargin > 10m)
        {
            recommendations.Add("利润率一般，需要控制成本");
            recommendations.Add("建议优化供应链或提高售价");
        }
        else
        {
            recommendations.Add("利润率偏低，风险较高");
            recommendations.Add("建议重新评估定价策略");
        }
        
        if (roi > 100m)
        {
            recommendations.Add("投资回报率高，值得投入");
        }
        else if (roi > 50m)
        {
            recommendations.Add("投资回报率中等，可以接受");
        }
        else
        {
            recommendations.Add("投资回报率偏低，需要谨慎");
        }
        
        return recommendations;
    }
    
    private List<string> GenerateActionPlan(DecisionType decision, Product product)
    {
        var plan = new List<string>();
        
        switch (decision)
        {
            case DecisionType.StrongBuy:
                plan.Add("立即行动，抢占市场先机");
                plan.Add("首批建议采购100-200件测试");
                plan.Add("定价策略：略低于市场平均价10%");
                plan.Add("推广策略：重点投入广告，快速起量");
                break;
            case DecisionType.Buy:
                plan.Add("可以进入，但需要控制风险");
                plan.Add("首批建议采购50-100件测试");
                plan.Add("定价策略：与市场持平");
                plan.Add("推广策略：循序渐进，根据数据调整");
                break;
            case DecisionType.Cautious:
                plan.Add("谨慎进入，做好充分准备");
                plan.Add("首批建议采购20-50件测试");
                plan.Add("定价策略：确保足够利润空间");
                plan.Add("推广策略：精准投放，控制成本");
                break;
            case DecisionType.Watch:
                plan.Add("暂时观望，持续关注");
                plan.Add("记录市场变化，寻找更好时机");
                plan.Add("可以小规模测试，但不要大量投入");
                break;
            case DecisionType.Avoid:
                plan.Add("建议暂时避免进入");
                plan.Add("寻找其他更有潜力的机会");
                plan.Add("如果一定要做，需要差异化策略");
                break;
        }
        
        plan.Add("建议：无论什么决策，都要做好数据分析和风险控制");
        
        return plan;
    }
}

// DTOs
public class RiskAssessment
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OverallRisk { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public List<RiskFactor> RiskFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalysisTime { get; set; }
}

public class RiskFactor
{
    public string Name { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

public class ProfitCalculation
{
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal ROI { get; set; }
    public int BreakEvenQuantity { get; set; }
    public CostBreakdown CostBreakdown { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class CostBreakdown
{
    public decimal ProductCost { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal AdvertisingCost { get; set; }
    public decimal ReturnCost { get; set; }
    public decimal PackagingCost { get; set; }
}

public class ProfitCalculationRequest
{
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Quantity { get; set; }
}

public class MarketOpportunity
{
    public string Category { get; set; } = string.Empty;
    public string OpportunityType { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PotentialProfit { get; set; } = string.Empty;
    public string CompetitionLevel { get; set; } = string.Empty;
    public string TimeWindow { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public string ActionSuggestion { get; set; } = string.Empty;
}

public class CompetitorAnalysis
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CompetitorCount { get; set; }
    public List<Competitor> Competitors { get; set; } = new();
    public string MarketPosition { get; set; } = string.Empty;
    public List<string> DifferentiationOpportunities { get; set; } = new();
    public string PricingRecommendation { get; set; } = string.Empty;
}

public class Competitor
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal SalesCount { get; set; }
    public decimal Rating { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public string Strategy { get; set; } = string.Empty;
}

public class DecisionRecommendation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DecisionType Decision { get; set; }
    public decimal Score { get; set; }
    public List<string> Reasons { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DetailedAnalysis DetailedAnalysis { get; set; } = new();
    public List<string> ActionPlan { get; set; } = new();
    public bool TimeSensitive { get; set; }
    public DateTime AnalysisTime { get; set; }
}

public class DetailedAnalysis
{
    public RiskAssessment RiskAssessment { get; set; } = new();
    public ProfitCalculation ProfitCalculation { get; set; } = new();
}

public class DecisionRequest
{
    public Product Product { get; set; } = new();
    public decimal Budget { get; set; }
    public string RiskTolerance { get; set; } = "medium"; // low, medium, high
    public decimal TargetProfit { get; set; }
}

public enum DecisionType
{
    StrongBuy,
    Buy,
    Cautious,
    Watch,
    Avoid
}
