using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 速率限制设置管理API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = "PlatformAdmin")]
public class RateLimitSettingsController : ControllerBase
{
    private readonly IRateLimitSettingsService _rateLimitSettingsService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<RateLimitSettingsController> _logger;

    public RateLimitSettingsController(
        IRateLimitSettingsService rateLimitSettingsService,
        IAuditLogService auditLogService,
        ILogger<RateLimitSettingsController> logger)
    {
        _rateLimitSettingsService = rateLimitSettingsService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的 UserId (GUID)
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 获取所有速率限制设置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RateLimitSetting>>> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _rateLimitSettingsService.GetAllAsync(cancellationToken);
        return Ok(settings);
    }

    /// <summary>
    /// 获取启用的速率限制设置
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<IEnumerable<RateLimitSetting>>> GetEnabled(CancellationToken cancellationToken)
    {
        var settings = await _rateLimitSettingsService.GetEnabledAsync(cancellationToken);
        return Ok(settings);
    }

    /// <summary>
    /// 根据ID获取速率限制设置
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RateLimitSetting>> GetById(int id, CancellationToken cancellationToken)
    {
        var setting = await _rateLimitSettingsService.GetByIdAsync(id, cancellationToken);
        if (setting == null)
        {
            return NotFound(new { message = $"Rate limit setting with ID {id} not found" });
        }

        return Ok(setting);
    }

    /// <summary>
    /// 创建速率限制设置
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RateLimitSetting>> Create(
        [FromBody] RateLimitSetting setting,
        CancellationToken cancellationToken)
    {
        try
        {
            var username = User.Identity?.Name ?? "Unknown";
            var created = await _rateLimitSettingsService.CreateAsync(setting, username, cancellationToken);

            await _auditLogService.LogAsync(
                action: "Rate Limit Setting Created",
                category: "Configuration",
                userId: GetCurrentUserId(),
                details: $"Created rate limit setting: {setting.LimiterName}",
                success: true
            );

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for creating rate limit setting");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rate limit setting already exists");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create rate limit setting");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 更新速率限制设置
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<RateLimitSetting>> Update(
        int id,
        [FromBody] RateLimitSetting setting,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != setting.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var username = User.Identity?.Name ?? "Unknown";
            var updated = await _rateLimitSettingsService.UpdateAsync(setting, username, cancellationToken);

            await _auditLogService.LogAsync(
                action: "Rate Limit Setting Updated",
                category: "Configuration",
                userId: GetCurrentUserId(),
                details: $"Updated rate limit setting: {setting.LimiterName}",
                success: true
            );

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for updating rate limit setting {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rate limit setting {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update rate limit setting {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 删除速率限制设置
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var setting = await _rateLimitSettingsService.GetByIdAsync(id, cancellationToken);
            if (setting == null)
            {
                return NotFound(new { message = $"Rate limit setting with ID {id} not found" });
            }

            await _rateLimitSettingsService.DeleteAsync(id, cancellationToken);

            await _auditLogService.LogAsync(
                action: "Rate Limit Setting Deleted",
                category: "Configuration",
                userId: GetCurrentUserId(),
                details: $"Deleted rate limit setting: {setting.LimiterName}",
                success: true
            );

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete rate limit setting {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 确保默认设置存在
    /// </summary>
    [HttpPost("ensure-defaults")]
    public async Task<IActionResult> EnsureDefaults(CancellationToken cancellationToken)
    {
        try
        {
            await _rateLimitSettingsService.EnsureDefaultSettingsAsync(cancellationToken);

            await _auditLogService.LogAsync(
                action: "Rate Limit Default Settings Ensured",
                category: "Configuration",
                userId: GetCurrentUserId(),
                success: true
            );

            return Ok(new { message = "Default rate limit settings ensured" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure default rate limit settings");
            return BadRequest(new { message = ex.Message });
        }
    }
}
