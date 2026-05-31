namespace SkuSearch.Domain.Entities;

/// <summary>
/// 对应 MySQL 表 skudetail2
/// </summary>
public class SkuDetail
{
    /// <summary>自增主键</summary>
    public long     RecordId        { get; set; }

    /// <summary>商品唯一Id (业务主键)</summary>
    public string   GoodsId         { get; set; } = string.Empty;

    /// <summary>店铺商品sku</summary>
    public string   SkuId           { get; set; } = string.Empty;

    /// <summary>店铺Id</summary>
    public int      ShopId          { get; set; }

    /// <summary>对接店铺内部商品sku</summary>
    public string?  ShopSkuId       { get; set; }

    /// <summary>商品名称</summary>
    public string?  Name            { get; set; }

    /// <summary>商品spu</summary>
    public string?  SpuId           { get; set; }

    /// <summary>商品详情规格对应名称</summary>
    public string?  SpuItemName     { get; set; }

    /// <summary>统一分类一级Id</summary>
    public int?     UCatId1         { get; set; }

    /// <summary>统一分类二级Id</summary>
    public int?     UCatId2         { get; set; }

    /// <summary>统一分类三级Id</summary>
    public int?     UCatId3         { get; set; }

    /// <summary>商品类型（卡券直接结算/不校验成本价/需发送激活码/权益卡/精品券/预售商品/景区门票/实体卡/批量采购）</summary>
    public string?  GoodsType       { get; set; }

    /// <summary>品牌Id</summary>
    public int      BrandId         { get; set; }

    /// <summary>品牌名</summary>
    public string?  BrandName       { get; set; }

    /// <summary>协议价/成本价/结算价</summary>
    public decimal? PriceCost       { get; set; }

    /// <summary>销售价（有多个规格取最小价）</summary>
    public decimal? PriceSale       { get; set; }

    /// <summary>销售价（有多个规格取最大价）</summary>
    public decimal? PriceSale2      { get; set; }

    /// <summary>市场价/划线价/参考价</summary>
    public decimal? PriceMarket     { get; set; }

    /// <summary>销售范围（1商城，2蛋糕，3精品券，4积分，7鲜花）</summary>
    public byte?    SaleRange       { get; set; }

    /// <summary>审核状态（上架审核，同意上架，拒绝上架）</summary>
    public string?  CheckStatus     { get; set; }

    /// <summary>删除标识（0正常，1已删除）</summary>
    public bool     Deleted         { get; set; }

    /// <summary>接口上下架状态（0下架，1上架）- 与接口同步不可修改</summary>
    public byte     State           { get; set; }

    /// <summary>设置上下架状态（0下架，1上架）- 后台可修改</summary>
    public byte     AutoState       { get; set; }

    /// <summary>基础价</summary>
    public decimal? BasePrice2      { get; set; }

    /// <summary>新增时间</summary>
    public DateTime CreateDateTime  { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    public DateTime LastUpdateTime  { get; set; } = DateTime.UtcNow;
}
