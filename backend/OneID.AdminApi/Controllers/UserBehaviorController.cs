using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 用户行为分析控制器
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UserBehaviorController : ControllerBase
{
    private readonly IUserBehaviorAnalyticsService _behaviorService;
    private readonly ILogger<UserBehaviorController> _logger;

    public UserBehaviorController(
        IUserBehaviorAnalyticsService behaviorService,
        ILogger<UserBehaviorController> logger)
    {
        _behaviorService = behaviorService;
        _logger = logger;
    }

    /// <summary>
    /// 获取设备类型统计
    /// </summary>
    [HttpGet("devices")]
    public async Task<ActionResult<Dictionary<string, int>>> GetDeviceStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var stats = await _behaviorService.GetDeviceStatisticsAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device statistics");
            return StatusCode(500, new { message = "Failed to retrieve device statistics" });
        }
    }

    /// <summary>
    /// 获取浏览器统计
    /// </summary>
    [HttpGet("browsers")]
    public async Task<ActionResult<Dictionary<string, int>>> GetBrowserStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var stats = await _behaviorService.GetBrowserStatisticsAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get browser statistics");
            return StatusCode(500, new { message = "Failed to retrieve browser statistics" });
        }
    }

    /// <summary>
    /// 获取操作系统统计
    /// </summary>
    [HttpGet("operating-systems")]
    public async Task<ActionResult<Dictionary<string, int>>> GetOperatingSystemStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var stats = await _behaviorService.GetOperatingSystemStatisticsAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operating system statistics");
            return StatusCode(500, new { message = "Failed to retrieve operating system statistics" });
        }
    }

    /// <summary>
    /// 获取地理位置统计
    /// </summary>
    [HttpGet("geographic")]
    public async Task<ActionResult<Dictionary<string, int>>> GetGeographicStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var stats = await _behaviorService.GetGeographicStatisticsAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get geographic statistics");
            return StatusCode(500, new { message = "Failed to retrieve geographic statistics" });
        }
    }

    /// <summary>
    /// 获取综合行为分析报告
    /// </summary>
    [HttpGet("report")]
    public async Task<ActionResult<UserBehaviorReport>> GetBehaviorReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var report = await _behaviorService.GetBehaviorReportAsync(startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get behavior report");
            return StatusCode(500, new { message = "Failed to retrieve behavior report" });
        }
    }
}

