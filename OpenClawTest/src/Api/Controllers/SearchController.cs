using Microsoft.AspNetCore.Mvc;
using SkuSearch.Application.DTOs;
using SkuSearch.Application.Services;

namespace SkuSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _search;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService search, ILogger<SearchController> logger)
    {
        _search = search;
        _logger = logger;
    }

    /// <summary>
    /// 智能商品搜索（文本 / 语音转文字后调用）
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SearchResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search(
        [FromBody] SearchRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Query))
            return BadRequest(new { error = "搜索词不能为空" });

        var result = await _search.SearchAsync(req, ct);
        return Ok(result);
    }

    /// <summary>
    /// 快速搜索（GET）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResponse), 200)]
    public async Task<IActionResult> QuickSearch(
        [FromQuery] string q,
        [FromQuery] int    top       = 20,
        [FromQuery] int?   shopId    = null,
        [FromQuery] int?   uCatId1   = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "参数 q 不能为空" });

        var result = await _search.SearchAsync(
            new SearchRequest(q, top, shopId, uCatId1), ct);
        return Ok(result);
    }
}
