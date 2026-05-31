using Microsoft.EntityFrameworkCore;
using ProductSelector.Data;
using ProductSelector.Models;

namespace ProductSelector.Services;

public interface IFavoritesService
{
    Task<List<UserFavorite>> GetUserFavoritesAsync(int userId);
    Task<bool> AddFavoriteAsync(int userId, int productId, string? note = null);
    Task<bool> RemoveFavoriteAsync(int userId, int productId);
    Task<bool> IsFavoriteAsync(int userId, int productId);
    Task<int> GetFavoritesCountAsync(int userId);
}

public class FavoritesService : IFavoritesService
{
    private readonly AppDbContext _context;
    
    public FavoritesService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<UserFavorite>> GetUserFavoritesAsync(int userId)
    {
        return await _context.UserFavorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Product)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<bool> AddFavoriteAsync(int userId, int productId, string? note = null)
    {
        // 检查是否已收藏
        if (await IsFavoriteAsync(userId, productId))
        {
            return false;
        }
        
        var favorite = new UserFavorite
        {
            UserId = userId,
            ProductId = productId,
            Note = note,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.UserFavorites.Add(favorite);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> RemoveFavoriteAsync(int userId, int productId)
    {
        var favorite = await _context.UserFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
        
        if (favorite == null)
        {
            return false;
        }
        
        _context.UserFavorites.Remove(favorite);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> IsFavoriteAsync(int userId, int productId)
    {
        return await _context.UserFavorites
            .AnyAsync(f => f.UserId == userId && f.ProductId == productId);
    }
    
    public async Task<int> GetFavoritesCountAsync(int userId)
    {
        return await _context.UserFavorites
            .CountAsync(f => f.UserId == userId);
    }
}

public interface ISearchHistoryService
{
    Task<List<SearchHistory>> GetUserSearchHistoryAsync(int userId, int limit = 20);
    Task<SearchHistory> RecordSearchAsync(int userId, string keyword, int resultsCount);
    Task<bool> ClearSearchHistoryAsync(int userId);
    Task<List<string>> GetPopularKeywordsAsync(int limit = 10);
}

public class SearchHistoryService : ISearchHistoryService
{
    private readonly AppDbContext _context;
    
    public SearchHistoryService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<SearchHistory>> GetUserSearchHistoryAsync(int userId, int limit = 20)
    {
        return await _context.SearchHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.SearchedAt)
            .Take(limit)
            .ToListAsync();
    }
    
    public async Task<SearchHistory> RecordSearchAsync(int userId, string keyword, int resultsCount)
    {
        var history = new SearchHistory
        {
            UserId = userId,
            Keyword = keyword,
            ResultsCount = resultsCount,
            SearchedAt = DateTime.UtcNow
        };
        
        _context.SearchHistories.Add(history);
        await _context.SaveChangesAsync();
        
        return history;
    }
    
    public async Task<bool> ClearSearchHistoryAsync(int userId)
    {
        var histories = await _context.SearchHistories
            .Where(h => h.UserId == userId)
            .ToListAsync();
        
        _context.SearchHistories.RemoveRange(histories);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<string>> GetPopularKeywordsAsync(int limit = 10)
    {
        return await _context.SearchHistories
            .GroupBy(h => h.Keyword)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key)
            .ToListAsync();
    }
}
