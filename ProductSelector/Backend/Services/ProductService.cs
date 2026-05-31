using Microsoft.EntityFrameworkCore;
using ProductSelector.Data;
using ProductSelector.Models;

namespace ProductSelector.Services;

public interface IProductService
{
    Task<List<Product>> GetProductsAsync(string? platform = null, string? category = null, int page = 1, int pageSize = 20);
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<List<Product>> SearchProductsAsync(string keyword);
    Task<List<Product>> GetTrendingProductsAsync(int limit = 10);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    
    public ProductService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Product>> GetProductsAsync(string? platform = null, string? category = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Products.AsQueryable();
        
        if (!string.IsNullOrEmpty(platform))
            query = query.Where(p => p.Platform == platform);
        
        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);
        
        return await query
            .OrderByDescending(p => p.SalesCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }
    
    public async Task<Product> CreateProductAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        return product;
    }
    
    public async Task<Product> UpdateProductAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        
        return product;
    }
    
    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;
        
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<Product>> SearchProductsAsync(string keyword)
    {
        return await _context.Products
            .Where(p => p.Name.Contains(keyword) || 
                       (p.Category != null && p.Category.Contains(keyword)) ||
                       (p.ShopName != null && p.ShopName.Contains(keyword)))
            .OrderByDescending(p => p.SalesCount)
            .ToListAsync();
    }
    
    public async Task<List<Product>> GetTrendingProductsAsync(int limit = 10)
    {
        return await _context.Products
            .OrderByDescending(p => p.SalesCount)
            .Take(limit)
            .ToListAsync();
    }
}
