using System.ComponentModel.DataAnnotations;

namespace ProductSelector.Models;

public class Product
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? ImageUrl { get; set; }
    
    public decimal Price { get; set; }
    
    public decimal? OriginalPrice { get; set; }
    
    public int SalesCount { get; set; }
    
    public int ReviewCount { get; set; }
    
    public decimal Rating { get; set; }
    
    public string? Platform { get; set; } // 1688, Temu, TikTok
    
    public string? Category { get; set; }
    
    public string? SourceUrl { get; set; }
    
    public string? ShopName { get; set; }
    
    public string? Location { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public List<ProductTag> Tags { get; set; } = new();
}

public class ProductTag
{
    [Key]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int ProductId { get; set; }
    
    public Product? Product { get; set; }
}
