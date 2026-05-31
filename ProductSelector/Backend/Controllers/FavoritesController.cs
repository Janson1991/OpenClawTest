using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductSelector.Models;
using ProductSelector.Services;
using System.Security.Claims;

namespace ProductSelector.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favoritesService;
    private readonly IProductService _productService;
    
    public FavoritesController(IFavoritesService favoritesService, IProductService productService)
    {
        _favoritesService = favoritesService;
        _productService = productService;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<UserFavorite>>> GetFavorites()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var favorites = await _favoritesService.GetUserFavoritesAsync(userId);
        return Ok(favorites);
    }
    
    [HttpPost("add/{productId}")]
    public async Task<IActionResult> AddFavorite(int productId, [FromQuery] string? note = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var result = await _favoritesService.AddFavoriteAsync(userId, productId, note);
        
        if (!result)
        {
            return BadRequest("已收藏或商品不存在");
        }
        
        return Ok(new { message = "收藏成功" });
    }
    
    [HttpDelete("remove/{productId}")]
    public async Task<IActionResult> RemoveFavorite(int productId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var result = await _favoritesService.RemoveFavoriteAsync(userId, productId);
        
        if (!result)
        {
            return NotFound("未收藏该商品");
        }
        
        return Ok(new { message = "取消收藏成功" });
    }
    
    [HttpGet("check/{productId}")]
    public async Task<ActionResult<bool>> CheckFavorite(int productId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var isFavorite = await _favoritesService.IsFavoriteAsync(userId, productId);
        return Ok(isFavorite);
    }
    
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetFavoritesCount()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var count = await _favoritesService.GetFavoritesCountAsync(userId);
        return Ok(count);
    }
}
