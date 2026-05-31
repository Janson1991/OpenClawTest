namespace SkuSearch.Domain.Entities;

public class Sku
{
    public long     Id          { get; set; }
    public string   SkuCode     { get; set; } = string.Empty;
    public string   Name        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public string?  Category    { get; set; }
    public string?  Tags        { get; set; }   // 逗号分隔: "帐篷,户外,露营"
    public string?  Brand       { get; set; }
    public decimal  Price       { get; set; }
    public string?  ImageUrl    { get; set; }
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;
}
