using Microsoft.EntityFrameworkCore;
using ProductSelector.Models;

namespace ProductSelector.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductTag> ProductTags { get; set; }
    public DbSet<PriceAlert> PriceAlerts { get; set; }
    public DbSet<PriceHistory> PriceHistories { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserFavorite> UserFavorites { get; set; }
    public DbSet<SearchHistory> SearchHistories { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Product>()
            .HasMany(p => p.Tags)
            .WithOne(t => t.Product)
            .HasForeignKey(t => t.ProductId);
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Platform);
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Category);
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.SalesCount);
        
        modelBuilder.Entity<PriceAlert>()
            .HasIndex(a => a.UserId);
        
        modelBuilder.Entity<PriceAlert>()
            .HasIndex(a => a.ProductId);
        
        modelBuilder.Entity<PriceHistory>()
            .HasIndex(h => h.ProductId);
        
        modelBuilder.Entity<PriceHistory>()
            .HasIndex(h => h.RecordedAt);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        modelBuilder.Entity<UserFavorite>()
            .HasIndex(f => new { f.UserId, f.ProductId })
            .IsUnique();
        
        modelBuilder.Entity<SearchHistory>()
            .HasIndex(h => h.UserId);
        
        modelBuilder.Entity<SearchHistory>()
            .HasIndex(h => h.Keyword);
    }
}
