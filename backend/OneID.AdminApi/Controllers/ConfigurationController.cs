using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Configuration;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 配置管理控制器
/// 提供配置热更新和状态查询功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminApiScope")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationRefreshService _refreshService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IConfigurationRefreshService refreshService,
        IAuditLogService auditLogService,
        ILogger<ConfigurationController> logger)
    {
        _refreshService = refreshService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 获取配置状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var rateLimitOptions = _refreshService.GetRateLimitOptions();
        var corsOptions = _refreshService.GetCorsOptions();
        var externalAuthOptions = _refreshService.GetExternalAuthOptions();

        return Ok(new
        {
            rateLimit = new
            {
                version = rateLimitOptions.Version,
                lastUpdated = rateLimitOptions.LastUpdated,
                settingsCount = rateLimitOptions.Settings.Count,
                enabledCount = rateLimitOptions.Settings.Count(s => s.Enabled)
            },
            cors = new
            {
                version = corsOptions.Version,
                lastUpdated = corsOptions.LastUpdated,
                allowAnyOrigin = corsOptions.AllowAnyOrigin,
                originsCount = corsOptions.AllowedOrigins.Count
            },
            externalAuth = new
            {
                version = externalAuthOptions.Version,
                lastUpdated = externalAuthOptions.LastUpdated,
                providersCount = externalAuthOptions.Providers.Count
            }
        });
    }

    /// <summary>
    /// 刷新所有配置
    /// </summary>
    [HttpPost("reload")]
    public async Task<IActionResult> ReloadAll(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;

        await _refreshService.RefreshAllAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReloadAll",
            "Configuration",
            true,
            $"All configurations reloaded by {userId}",
            userName: userId);

        _logger.LogInformation("All configurations reloaded by {UserId}", userId);

        return Ok(new { Message = "All configurations reloaded successfully", ReloadedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// 刷新速率限制配置
    /// </summary>
    [HttpPost("reload/ratelimit")]
    public async Task<IActionResult> ReloadRateLimit(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;

        await _refreshService.RefreshRateLimitAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReloadRateLimit",
            "Configuration",
            true,
            $"RateLimit configuration reloaded by {userId}",
            userName: userId);

        var options = _refreshService.GetRateLimitOptions();
        return Ok(new
        {
            Message = "RateLimit configuration reloaded",
            options.Version,
            options.LastUpdated,
            SettingsCount = options.Settings.Count
        });
    }

    /// <summary>
    /// 刷新 CORS 配置
    /// </summary>
    [HttpPost("reload/cors")]
    public async Task<IActionResult> ReloadCors(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;

        await _refreshService.RefreshCorsAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReloadCors",
            "Configuration",
            true,
            $"CORS configuration reloaded by {userId}",
            userName: userId);

        var options = _refreshService.GetCorsOptions();
        return Ok(new
        {
            Message = "CORS configuration reloaded",
            options.Version,
            options.LastUpdated,
            options.AllowAnyOrigin,
            OriginsCount = options.AllowedOrigins.Count
        });
    }

    /// <summary>
    /// 刷新外部认证配置
    /// </summary>
    [HttpPost("reload/externalauth")]
    public async Task<IActionResult> ReloadExternalAuth(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;

        await _refreshService.RefreshExternalAuthAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReloadExternalAuth",
            "Configuration",
            true,
            $"ExternalAuth configuration reloaded by {userId}",
            userName: userId);

        var options = _refreshService.GetExternalAuthOptions();
        return Ok(new
        {
            Message = "ExternalAuth configuration reloaded",
            options.Version,
            options.LastUpdated,
            ProvidersCount = options.Providers.Count,
            Providers = options.Providers.Select(p => new { p.Name, p.DisplayName, p.ProviderType })
        });
    }
}
