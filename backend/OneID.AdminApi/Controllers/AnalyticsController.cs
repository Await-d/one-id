using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 分析统计控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取仪表板统计数据
    /// </summary>
    /// <param name="startDate">开始日期（可选）</param>
    /// <param name="endDate">结束日期（可选）</param>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatistics>> GetDashboardStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var statistics = await _analyticsService.GetDashboardStatisticsAsync(startDate, endDate);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, new { message = "Error retrieving statistics" });
        }
    }

    /// <summary>
    /// 获取登录趋势数据
    /// </summary>
    /// <param name="days">天数（默认7天）</param>
    [HttpGet("login-trends")]
    public async Task<ActionResult<List<LoginTrend>>> GetLoginTrends(
        [FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 90)
            {
                return BadRequest(new { message = "Days must be between 1 and 90" });
            }

            var trends = await _analyticsService.GetLoginTrendsAsync(days);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving login trends");
            return StatusCode(500, new { message = "Error retrieving login trends" });
        }
    }

    /// <summary>
    /// 获取 API 调用统计
    /// </summary>
    /// <param name="topCount">返回前N个（默认10）</param>
    [HttpGet("api-calls")]
    public async Task<ActionResult<List<ApiCallStatistic>>> GetApiCallStatistics(
        [FromQuery] int topCount = 10)
    {
        try
        {
            if (topCount < 1 || topCount > 50)
            {
                return BadRequest(new { message = "TopCount must be between 1 and 50" });
            }

            var statistics = await _analyticsService.GetApiCallStatisticsAsync(topCount);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API call statistics");
            return StatusCode(500, new { message = "Error retrieving API call statistics" });
        }
    }
}

