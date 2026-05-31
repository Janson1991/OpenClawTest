namespace SkuSearch.Application.DTOs;

public record SearchRequest(
    string  Query,
    int     TopK        = 20,
    int?    ShopId      = null,    // 按店铺过滤
    int?    UCatId1     = null,    // 按一级分类过滤
    float   MinScore    = 0.50f
);

public record SearchResponse(
    List<SkuSearchItem> Items,
    ParsedQuery         ParsedQuery,
    int                 Total,
    long                ElapsedMs
);

public record SkuSearchItem(
    long     RecordId,
    string   GoodsId,
    string   SkuId,
    int      ShopId,
    string?  Name,
    string?  SpuItemName,
    string?  BrandName,
    decimal? PriceSale,
    decimal? PriceMarket,
    string?  GoodsType,
    string?  CheckStatus,
    byte     State,
    byte     AutoState,
    float    Score,           // 相似度得分 0~1
    string   Source           // "vector" | "keyword" | "merged"
);

public record ParsedQuery(
    List<string>              Keywords,
    List<string>              Synonyms,
    List<string>              Categories,
    Dictionary<string,string> Attributes
);
