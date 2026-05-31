using Microsoft.EntityFrameworkCore;
using SkuSearch.Domain.Entities;

namespace SkuSearch.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SkuDetail> Skus => Set<SkuDetail>();

    /// <summary>用于 FromSqlRaw 关键词搜索投影（Keyless Entity）</summary>
    public DbSet<SkuSearchProjection> SkuSearchProjections => Set<SkuSearchProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SkuDetail>(e =>
        {
            e.ToTable("skudetail2");
            e.HasKey(x => x.RecordId);
            e.Property(x => x.RecordId).ValueGeneratedOnAdd();

            e.Property(x => x.GoodsId).HasMaxLength(36).IsRequired();
            e.HasIndex(x => x.GoodsId).IsUnique().HasDatabaseName("idx_0");

            e.Property(x => x.SkuId).HasMaxLength(36).IsRequired();
            e.Property(x => x.ShopSkuId).HasMaxLength(36);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.SpuId).HasMaxLength(36);
            e.Property(x => x.SpuItemName).HasMaxLength(100);
            e.Property(x => x.GoodsType).HasMaxLength(20);
            e.Property(x => x.BrandName).HasMaxLength(100);
            e.Property(x => x.CheckStatus).HasMaxLength(20);

            e.Property(x => x.PriceCost).HasPrecision(18, 2);
            e.Property(x => x.PriceSale).HasPrecision(18, 2);
            e.Property(x => x.PriceSale2).HasPrecision(18, 2);
            e.Property(x => x.PriceMarket).HasPrecision(18, 2);
            e.Property(x => x.BasePrice2).HasPrecision(18, 2);

            // 组合索引
            e.HasIndex(x => new { x.SkuId, x.ShopId }).HasDatabaseName("idx_1");
            e.HasIndex(x => new { x.State, x.AutoState, x.Deleted, x.ShopId }).HasDatabaseName("idx_2");
            e.HasIndex(x => x.CreateDateTime).HasDatabaseName("idx_3");
            e.HasIndex(x => new { x.UCatId1, x.UCatId2, x.UCatId3 }).HasDatabaseName("idx_4");
            e.HasIndex(x => x.LastUpdateTime).HasDatabaseName("idx_5");
            e.HasIndex(x => x.GoodsId).HasDatabaseName("idx_6");
            e.HasIndex(x => new { x.CheckStatus, x.ShopId }).HasDatabaseName("idx_7");
            e.HasIndex(x => new { x.ShopId, x.LastUpdateTime }).HasDatabaseName("idx_10");
            e.HasIndex(x => new { x.SkuId, x.LastUpdateTime }).HasDatabaseName("idx_11");
        });

        modelBuilder.Entity<SkuSearchProjection>(e =>
        {
            e.HasNoKey();
            e.ToView(null);
        });
    }
}

/// <summary>关键词搜索 SQL 结果投影</summary>
public class SkuSearchProjection
{
    public long    RecordId    { get; set; }
    public string  GoodsId     { get; set; } = "";
    public string  SkuId       { get; set; } = "";
    public int     ShopId      { get; set; }
    public string? Name        { get; set; }
    public string? SpuItemName { get; set; }
    public string? BrandName   { get; set; }
    public decimal? PriceSale  { get; set; }
    public string? ImageUrl    { get; set; }
    public double  Score       { get; set; }
}
