using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Serilog;
using SkuSearch.Application.Services;
using SkuSearch.Infrastructure.Data;
using SkuSearch.Infrastructure.Repositories;
using SkuSearch.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog 日志 ───────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Controllers & Swagger ──────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SKU 智能搜索 API", Version = "v1" });
});

// ─── MySQL (Pomelo EF Core) ─────────────────────────────────────────────────
var mysqlConn = builder.Configuration.GetConnectionString("MySQL")!;
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(mysqlConn, ServerVersion.AutoDetect(mysqlConn)));

// ─── Redis 缓存 ──────────────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opt =>
    opt.Configuration = builder.Configuration.GetConnectionString("Redis"));

// ─── Qdrant 向量数据库 ────────────────────────────────────────────────────────
builder.Services.AddSingleton(_ => new QdrantClient(
    host:  builder.Configuration["Qdrant:Host"]  ?? "localhost",
    port:  int.Parse(builder.Configuration["Qdrant:GrpcPort"] ?? "6334"),
    https: false));

// ─── Embedding 服务（二选一，通过配置切换）──────────────────────────────────
var embeddingProvider = builder.Configuration["AI:EmbeddingProvider"] ?? "openai";
if (embeddingProvider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
else
    builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();

// ─── AI 查询解析 ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IAiQueryParser, AiQueryParser>();

// ─── 搜索仓储 + 服务 ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IVectorSearchRepository,  QdrantVectorSearchRepository>();
builder.Services.AddScoped<IKeywordSearchRepository, MySqlKeywordSearchRepository>();
builder.Services.AddScoped<ISearchService,           SearchService>();
builder.Services.AddScoped<IndexingService>();

// ─── CORS (Vue 开发环境) ──────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins(
        builder.Configuration["Cors:Origins"]?.Split(',')
        ?? ["http://localhost:5173"])
     .AllowAnyMethod()
     .AllowAnyHeader()));

// ─── 全局异常处理 ──────────────────────────────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SKU 搜索 v1"));
}

app.MapControllers();

app.Run();

// ─── 全局异常处理器 ───────────────────────────────────────────────────────────
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "未处理异常: {Path}", ctx.Request.Path);

        var (status, msg) = ex switch
        {
            ArgumentException        => (400, ex.Message),
            KeyNotFoundException     => (404, ex.Message),
            UnauthorizedAccessException => (403, "禁止访问"),
            OperationCanceledException  => (499, "请求已取消"),
            _                        => (500, "服务器内部错误")
        };

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { error = msg }, ct);
        return true;
    }
}
