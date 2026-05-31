using System.ComponentModel.DataAnnotations;

namespace ProductSelector.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? DisplayName { get; set; }
    
    [MaxLength(500)]
    public string? Avatar { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public List<UserFavorite> Favorites { get; set; } = new();
    
    public List<SearchHistory> SearchHistories { get; set; } = new();
}

public class UserFavorite
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public User? User { get; set; }
    
    public int ProductId { get; set; }
    
    public Product? Product { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? Note { get; set; }
}

public class SearchHistory
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public User? User { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Keyword { get; set; } = string.Empty;
    
    public int ResultsCount { get; set; }
    
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
