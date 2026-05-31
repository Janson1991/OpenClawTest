using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductSelector.Models;
using ProductSelector.Services;
using System.Security.Claims;

namespace ProductSelector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchHistoryController : ControllerBase
{
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly IProductService _productService;
    
    public SearchHistoryController(ISearchHistoryService searchHistoryService, IProductService productService)
    {
        _searchHistoryService = searchHistoryService;
        _productService = productService;
    }
    
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<SearchHistory>>> GetSearchHistory([FromQuery] int limit = 20)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var history = await _searchHistoryService.GetUserSearchHistoryAsync(userId, limit);
        return Ok(history);
    }
    
    [Authorize]
    [HttpPost("record")]
    public async Task<IActionResult> RecordSearch([FromQuery] string keyword, [FromQuery] int resultsCount)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _searchHistoryService.RecordSearchAsync(userId, keyword, resultsCount);
        return Ok();
    }
    
    [Authorize]
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearSearchHistory()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _searchHistoryService.ClearSearchHistoryAsync(userId);
        return Ok(new { message = "搜索历史已清空" });
    }
    
    [HttpGet("popular")]
    public async Task<ActionResult<List<string>>> GetPopularKeywords([FromQuery] int limit = 10)
    {
        var keywords = await _searchHistoryService.GetPopularKeywordsAsync(limit);
        return Ok(keywords);
    }
}
