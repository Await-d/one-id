using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 异常登录检测控制器
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AnomalyDetectionController : ControllerBase
{
    private readonly IAnomalyDetectionService _anomalyService;
    private readonly ILogger<AnomalyDetectionController> _logger;

    public AnomalyDetectionController(
        IAnomalyDetectionService anomalyService,
        ILogger<AnomalyDetectionController> logger)
    {
        _anomalyService = anomalyService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有异常登录记录
    /// </summary>
    [HttpGet("anomalous-logins")]
    public async Task<ActionResult<List<LoginHistory>>> GetAllAnomalousLogins(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logins = await _anomalyService.GetAllAnomalousLoginsAsync(
                startDate, 
                endDate, 
                pageNumber, 
                pageSize);
            
            return Ok(logins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get anomalous logins");
            return StatusCode(500, new { message = "Failed to retrieve anomalous logins" });
        }
    }

    /// <summary>
    /// 获取特定用户的异常登录记录
    /// </summary>
    [HttpGet("user/{userId}/anomalous-logins")]
    public async Task<ActionResult<List<LoginHistory>>> GetUserAnomalousLogins(
        Guid userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var logins = await _anomalyService.GetAnomalousLoginsAsync(userId, startDate, endDate);
            return Ok(logins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user anomalous logins");
            return StatusCode(500, new { message = "Failed to retrieve user anomalous logins" });
        }
    }

    /// <summary>
    /// 标记异常登录已通知用户
    /// </summary>
    [HttpPost("{loginHistoryId}/mark-notified")]
    public async Task<ActionResult> MarkAsNotified(Guid loginHistoryId)
    {
        try
        {
            await _anomalyService.MarkAsNotifiedAsync(loginHistoryId);
            return Ok(new { message = "Login marked as notified" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark login as notified");
            return StatusCode(500, new { message = "Failed to mark as notified" });
        }
    }
}

