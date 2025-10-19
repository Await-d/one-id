using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 用户设备管理控制器（管理员）
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UserDevicesController : ControllerBase
{
    private readonly IUserDeviceService _deviceService;
    private readonly ILogger<UserDevicesController> _logger;

    public UserDevicesController(IUserDeviceService deviceService, ILogger<UserDevicesController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// 获取指定用户的所有设备
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<UserDevice>>> GetUserDevices(Guid userId)
    {
        try
        {
            var devices = await _deviceService.GetUserDevicesAsync(userId);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 获取设备详情
    /// </summary>
    [HttpGet("{deviceId}")]
    public async Task<ActionResult<UserDevice>> GetDevice(Guid deviceId)
    {
        try
        {
            var device = await _deviceService.GetDeviceAsync(deviceId);
            if (device == null)
            {
                return NotFound();
            }
            return Ok(device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 获取用户设备统计
    /// </summary>
    [HttpGet("user/{userId}/statistics")]
    public async Task<ActionResult<DeviceStatistics>> GetDeviceStatistics(Guid userId)
    {
        try
        {
            var stats = await _deviceService.GetDeviceStatisticsAsync(userId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device statistics for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 信任/取消信任设备
    /// </summary>
    [HttpPatch("{deviceId}/trust")]
    public async Task<IActionResult> TrustDevice(Guid deviceId, [FromBody] TrustDeviceRequest request)
    {
        try
        {
            await _deviceService.TrustDeviceAsync(deviceId, request.Trusted);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trust status for device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 激活/停用设备
    /// </summary>
    [HttpPatch("{deviceId}/active")]
    public async Task<IActionResult> SetDeviceActive(Guid deviceId, [FromBody] SetDeviceActiveRequest request)
    {
        try
        {
            await _deviceService.SetDeviceActiveAsync(deviceId, request.Active);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating active status for device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 重命名设备
    /// </summary>
    [HttpPatch("{deviceId}/rename")]
    public async Task<IActionResult> RenameDevice(Guid deviceId, [FromBody] RenameDeviceRequest request)
    {
        try
        {
            await _deviceService.RenameDeviceAsync(deviceId, request.NewName);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 删除设备
    /// </summary>
    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> DeleteDevice(Guid deviceId)
    {
        try
        {
            await _deviceService.DeleteDeviceAsync(deviceId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }
}

public record TrustDeviceRequest(bool Trusted);
public record SetDeviceActiveRequest(bool Active);
public record RenameDeviceRequest(string NewName);

