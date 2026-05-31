using Microsoft.EntityFrameworkCore;
using SkuSearch.Domain.Entities;

namespace SkuSearch.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Sku> Skus => Set<Sku>();

    /// <summary>用于 FromSqlRaw 关键词搜索投影（Keyless Entity）</summary>
    public DbSet<SkuSearchProjection> SkuSearchProjections => Set<SkuSearchProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Sku 表映射
        modelBuilder.Entity<Sku>(e =>
        {
            e.ToTable("skus");
            e.HasKey(x => x.Id);
            e.Property(x => x.SkuCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(255).IsRequired();
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.Brand).HasMaxLength(128);
            e.Property(x => x.Tags).HasMaxLength(512);
            e.Property(x => x.Price).HasPrecision(10, 2);
        });

        // 无主键投影（SQL 查询结果映射）
        modelBuilder.Entity<SkuSearchProjection>(e =>
        {
            e.HasNoKey();
            e.ToView(null); // 只用于 FromSqlRaw
        });
    }
}

/// <summary>关键词搜索 SQL 结果投影</summary>
public class SkuSearchProjection
{
    public long    Id       { get; set; }
    public string  SkuCode  { get; set; } = "";
    public string  Name     { get; set; } = "";
    public string? Category { get; set; }
    public string? Brand    { get; set; }
    public decimal Price    { get; set; }
    public string? ImageUrl { get; set; }
    public double  Score    { get; set; }
}
