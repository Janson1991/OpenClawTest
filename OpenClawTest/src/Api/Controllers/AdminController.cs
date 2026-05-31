using Microsoft.AspNetCore.Mvc;
using SkuSearch.Infrastructure.Services;

namespace SkuSearch.Api.Controllers;

/// <summary>
/// 管理接口：建索引、查状态等（生产环境应加鉴权）
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IndexingService _indexing;
    private readonly ILogger<AdminController> _logger;

    // 全局索引任务控制令牌
    private static CancellationTokenSource? _cts;
    private static DateTime? _startedAt;

    public AdminController(IndexingService indexing, ILogger<AdminController> logger)
    {
        _indexing = indexing;
        _logger   = logger;
    }

    /// <summary>
    /// 触发全量建索引（后台异步执行，立即返回）
    /// </summary>
    [HttpPost("index/rebuild")]
    [ProducesResponseType(202)]
    [ProducesResponseType(409)]
    public IActionResult RebuildIndex()
    {
        if (_cts != null)
            return Conflict(new { message = "索引正在构建中，请等待完成", startedAt = _startedAt });

        _cts       = new CancellationTokenSource();
        _startedAt = DateTime.UtcNow;
        var token  = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await _indexing.BuildFullIndexAsync(token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("索引构建已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "索引构建失败");
            }
            finally
            {
                _cts       = null;
                _startedAt = null;
            }
        }, token);

        return Accepted(new { message = "索引构建已启动，请查看日志获取进度", startedAt = _startedAt });
    }

    /// <summary>取消正在进行的索引构建</summary>
    [HttpPost("index/cancel")]
    public IActionResult CancelIndex()
    {
        if (_cts == null)
            return NotFound(new { message = "没有正在运行的索引任务" });

        _cts.Cancel();
        return Ok(new { message = "已发送取消信号，索引将在当前批次完成后停止" });
    }

    /// <summary>查询 Qdrant 集合状态</summary>
    [HttpGet("index/status")]
    public async Task<IActionResult> GetIndexStatus()
    {
        try
        {
            var info = await _indexing.GetCollectionInfoAsync();
            return Ok(new
            {
                isBuilding    = _cts != null,
                startedAt     = _startedAt,
                status        = info.Status.ToString(),
                vectorCount   = info.VectorsCount,
                pointCount    = info.PointsCount,
                segmentsCount = info.SegmentsCount,
              //  diskUsageMb   = info.DiskDataSize / 1024 / 1024
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = "Qdrant 不可用", error = ex.Message });
        }
    }
}
