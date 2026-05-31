namespace SkuSearch.Application.DTOs;

public record SearchRequest(
    string  Query,
    int     TopK        = 20,
    string? Category    = null,
    float   MinScore    = 0.55f
);

public record SearchResponse(
    List<SkuSearchItem> Items,
    ParsedQuery         ParsedQuery,
    int                 Total,
    long                ElapsedMs
);

public record SkuSearchItem(
    long    Id,
    string  SkuCode,
    string  Name,
    string? Category,
    string? Brand,
    decimal Price,
    string? ImageUrl,
    float   Score,          // 相似度得分 0~1
    string  Source          // "vector" | "keyword" | "merged"
);

public record ParsedQuery(
    List<string>              Keywords,
    List<string>              Synonyms,
    List<string>              Categories,
    Dictionary<string,string> Attributes
);
