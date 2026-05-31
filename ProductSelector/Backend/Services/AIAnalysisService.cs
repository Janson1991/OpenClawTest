using System.Text;
using ProductSelector.Models;

namespace ProductSelector.Services;

public interface IAIAnalysisService
{
    Task<string> AnalyzeProductAsync(Product product);
    Task<List<ProductAnalysis>> BatchAnalyzeAsync(List<Product> products);
    Task<string> GenerateMarketReportAsync(List<Product> products);
    Task<List<ProductRecommendation>> GetRecommendationsAsync(List<Product> products);
    Task<ProductComparison> CompareProductsAsync(Product product1, Product product2);
}

public class AIAnalysisService : IAIAnalysisService
{
    private readonly ILogger<AIAnalysisService> _logger;
    
    public AIAnalysisService(ILogger<AIAnalysisService> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> AnalyzeProductAsync(Product product)
    {
        var analysis = new StringBuilder();
        
        // 基础数据分析
        analysis.AppendLine($"## 商品分析报告 - {product.Name}");
        analysis.AppendLine();
        
        // 价格分析
        if (product.OriginalPrice.HasValue && product.OriginalPrice > 0)
        {
            var discount = (1 - product.Price / product.OriginalPrice.Value) * 100;
            analysis.AppendLine($"### 价格分析");
            analysis.AppendLine($"- 当前价格: ¥{product.Price:F2}");
            analysis.AppendLine($"- 原价: ¥{product.OriginalPrice.Value:F2}");
            analysis.AppendLine($"- 折扣: {discount:F1}%");
            analysis.AppendLine();
        }
        
        // 销量分析
        analysis.AppendLine($"### 销量分析");
        analysis.AppendLine($"- 累计销量: {product.SalesCount:N0}");
        analysis.AppendLine($"- 评价数量: {product.ReviewCount:N0}");
        if (product.ReviewCount > 0)
        {
            var reviewRatio = (decimal)product.SalesCount / product.ReviewCount;
            analysis.AppendLine($"- 销量评价比: {reviewRatio:F1}:1");
        }
        analysis.AppendLine();
        
        // 评分分析
        analysis.AppendLine($"### 评分分析");
        analysis.AppendLine($"- 评分: {product.Rating}/5.0");
        analysis.AppendLine($"- 评分等级: {GetRatingLevel(product.Rating)}");
        analysis.AppendLine();
        
        // 综合评估
        analysis.AppendLine($"### 综合评估");
        var score = CalculateProductScore(product);
        analysis.AppendLine($"- 综合得分: {score:F1}/100");
        analysis.AppendLine($"- 推荐指数: {GetRecommendationLevel(score)}");
        
        return await Task.FromResult(analysis.ToString());
    }
    
    public async Task<List<ProductAnalysis>> BatchAnalyzeAsync(List<Product> products)
    {
        var analyses = new List<ProductAnalysis>();
        
        foreach (var product in products)
        {
            var score = CalculateProductScore(product);
            analyses.Add(new ProductAnalysis
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Score = score,
                Recommendation = GetRecommendationLevel(score),
                Strengths = GetStrengths(product),
                Weaknesses = GetWeaknesses(product),
                MarketPosition = GetMarketPosition(product)
            });
        }
        
        return await Task.FromResult(analyses.OrderByDescending(a => a.Score).ToList());
    }
    
    public async Task<string> GenerateMarketReportAsync(List<Product> products)
    {
        var report = new StringBuilder();
        
        report.AppendLine("# 市场分析报告");
        report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        // 总体统计
        report.AppendLine("## 总体统计");
        report.AppendLine($"- 分析商品数: {products.Count}");
        report.AppendLine($"- 平均价格: ¥{products.Average(p => p.Price):F2}");
        report.AppendLine($"- 平均销量: {products.Average(p => p.SalesCount):N0}");
        report.AppendLine($"- 平均评分: {products.Average(p => p.Rating):F2}");
        report.AppendLine();
        
        // 价格分布
        report.AppendLine("## 价格分布");
        var priceRanges = new Dictionary<string, int>
        {
            ["0-50元"] = products.Count(p => p.Price <= 50),
            ["50-100元"] = products.Count(p => p.Price > 50 && p.Price <= 100),
            ["100-200元"] = products.Count(p => p.Price > 100 && p.Price <= 200),
            ["200元以上"] = products.Count(p => p.Price > 200)
        };
        
        foreach (var range in priceRanges)
        {
            var percentage = (decimal)range.Value / products.Count * 100;
            report.AppendLine($"- {range.Key}: {range.Value}个 ({percentage:F1}%)");
        }
        report.AppendLine();
        
        // 销量排行
        report.AppendLine("## 销量TOP5");
        var topProducts = products.OrderByDescending(p => p.SalesCount).Take(5);
        var rank = 1;
        foreach (var product in topProducts)
        {
            report.AppendLine($"{rank}. {product.Name} - 销量:{product.SalesCount:N0}, 价格:¥{product.Price:F2}");
            rank++;
        }
        report.AppendLine();
        
        // 平台分布
        report.AppendLine("## 平台分布");
        var platformGroups = products.GroupBy(p => p.Platform ?? "未知");
        foreach (var group in platformGroups)
        {
            var percentage = (decimal)group.Count() / products.Count * 100;
            report.AppendLine($"- {group.Key}: {group.Count()}个 ({percentage:F1}%)");
        }
        report.AppendLine();
        
        // 分类分布
        report.AppendLine("## 分类分布");
        var categoryGroups = products.GroupBy(p => p.Category ?? "未知");
        foreach (var group in categoryGroups.OrderByDescending(g => g.Count()))
        {
            var percentage = (decimal)group.Count() / products.Count * 100;
            report.AppendLine($"- {group.Key}: {group.Count()}个 ({percentage:F1}%)");
        }
        
        return await Task.FromResult(report.ToString());
    }
    
    public async Task<List<ProductRecommendation>> GetRecommendationsAsync(List<Product> products)
    {
        var recommendations = new List<ProductRecommendation>();
        
        foreach (var product in products)
        {
            var score = CalculateProductScore(product);
            var reasons = new List<string>();
            
            // 基于规则的推荐逻辑
            if (product.SalesCount > 10000)
                reasons.Add("高销量商品，市场验证充分");
            
            if (product.Rating >= 4.5m)
                reasons.Add("用户评分高，品质可靠");
            
            if (product.OriginalPrice.HasValue && product.OriginalPrice > product.Price * 1.5m)
                reasons.Add("折扣力度大，性价比高");
            
            if (product.ReviewCount > 1000)
                reasons.Add("评价数量多，用户反馈充分");
            
            if (score > 70)
            {
                recommendations.Add(new ProductRecommendation
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Score = score,
                    Reasons = reasons,
                    Action = GetRecommendedAction(score)
                });
            }
        }
        
        return await Task.FromResult(recommendations.OrderByDescending(r => r.Score).ToList());
    }
    
    public async Task<ProductComparison> CompareProductsAsync(Product product1, Product product2)
    {
        var comparison = new ProductComparison
        {
            Product1Name = product1.Name,
            Product2Name = product2.Name,
            PriceDifference = product1.Price - product2.Price,
            SalesDifference = product1.SalesCount - product2.SalesCount,
            RatingDifference = product1.Rating - product2.Rating,
            Winner = DetermineWinner(product1, product2),
            Analysis = GenerateComparisonAnalysis(product1, product2)
        };
        
        return await Task.FromResult(comparison);
    }
    
    private decimal CalculateProductScore(Product product)
    {
        decimal score = 0;
        
        // 销量权重 (30%)
        var salesScore = Math.Min(product.SalesCount / 1000m, 30m);
        score += salesScore;
        
        // 评分权重 (25%)
        var ratingScore = product.Rating * 5m;
        score += ratingScore;
        
        // 评价数量权重 (20%)
        var reviewScore = Math.Min(product.ReviewCount / 100m, 20m);
        score += reviewScore;
        
        // 价格优势权重 (15%)
        if (product.OriginalPrice.HasValue && product.OriginalPrice > 0)
        {
            var discount = 1 - (product.Price / product.OriginalPrice.Value);
            var priceScore = discount * 15m;
            score += priceScore;
        }
        
        // 平台权重 (10%)
        var platformScore = product.Platform switch
        {
            "1688" => 8m,
            "Temu" => 10m,
            "TikTok" => 9m,
            _ => 5m
        };
        score += platformScore;
        
        return Math.Min(score, 100m);
    }
    
    private string GetRatingLevel(decimal rating)
    {
        return rating switch
        {
            >= 4.8m => "优秀",
            >= 4.5m => "良好",
            >= 4.0m => "一般",
            >= 3.5m => "较差",
            _ => "差"
        };
    }
    
    private string GetRecommendationLevel(decimal score)
    {
        return score switch
        {
            >= 80m => "强烈推荐",
            >= 60m => "推荐",
            >= 40m => "一般",
            >= 20m => "谨慎",
            _ => "不推荐"
        };
    }
    
    private string GetRecommendedAction(decimal score)
    {
        return score switch
        {
            >= 80m => "立即上架",
            >= 60m => "建议上架",
            >= 40m => "观望",
            _ => "暂不推荐"
        };
    }
    
    private List<string> GetStrengths(Product product)
    {
        var strengths = new List<string>();
        
        if (product.SalesCount > 10000)
            strengths.Add("高销量");
        
        if (product.Rating >= 4.5m)
            strengths.Add("高评分");
        
        if (product.OriginalPrice.HasValue && product.OriginalPrice > product.Price * 1.5m)
            strengths.Add("高折扣");
        
        if (product.ReviewCount > 1000)
            strengths.Add("多评价");
        
        return strengths;
    }
    
    private List<string> GetWeaknesses(Product product)
    {
        var weaknesses = new List<string>();
        
        if (product.SalesCount < 1000)
            weaknesses.Add("销量较低");
        
        if (product.Rating < 4.0m)
            weaknesses.Add("评分较低");
        
        if (product.ReviewCount < 100)
            weaknesses.Add("评价较少");
        
        return weaknesses;
    }
    
    private string GetMarketPosition(Product product)
    {
        var score = CalculateProductScore(product);
        
        return score switch
        {
            >= 80m => "领导者",
            >= 60m => "挑战者",
            >= 40m => "跟随者",
            _ => "利基者"
        };
    }
    
    private string DetermineWinner(Product product1, Product product2)
    {
        var score1 = CalculateProductScore(product1);
        var score2 = CalculateProductScore(product2);
        
        if (score1 > score2)
            return product1.Name;
        else if (score2 > score1)
            return product2.Name;
        else
            return "平局";
    }
    
    private string GenerateComparisonAnalysis(Product product1, Product product2)
    {
        var analysis = new StringBuilder();
        
        analysis.AppendLine($"## 对比分析");
        analysis.AppendLine();
        
        // 价格对比
        if (product1.Price < product2.Price)
            analysis.AppendLine($"- {product1.Name} 价格更低，具有价格优势");
        else if (product2.Price < product1.Price)
            analysis.AppendLine($"- {product2.Name} 价格更低，具有价格优势");
        else
            analysis.AppendLine("- 两者价格相同");
        
        // 销量对比
        if (product1.SalesCount > product2.SalesCount)
            analysis.AppendLine($"- {product1.Name} 销量更高，市场接受度更好");
        else if (product2.SalesCount > product1.SalesCount)
            analysis.AppendLine($"- {product2.Name} 销量更高，市场接受度更好");
        
        // 评分对比
        if (product1.Rating > product2.Rating)
            analysis.AppendLine($"- {product1.Name} 评分更高，用户满意度更好");
        else if (product2.Rating > product1.Rating)
            analysis.AppendLine($"- {product2.Name} 评分更高，用户满意度更好");
        
        return analysis.ToString();
    }
}

// 辅助模型类
public class ProductAnalysis
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public string MarketPosition { get; set; } = string.Empty;
}

public class ProductRecommendation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public List<string> Reasons { get; set; } = new();
    public string Action { get; set; } = string.Empty;
}

public class ProductComparison
{
    public string Product1Name { get; set; } = string.Empty;
    public string Product2Name { get; set; } = string.Empty;
    public decimal PriceDifference { get; set; }
    public int SalesDifference { get; set; }
    public decimal RatingDifference { get; set; }
    public string Winner { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
}
