using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 管理员控制器
/// 提供应用管理功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminApiScope")]
public class AdminController : ControllerBase
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AdminController> _logger;

    // 重启冷却期保护
    private static DateTime _lastRestartAttempt = DateTime.MinValue;
    private static readonly TimeSpan RestartCooldown = TimeSpan.FromMinutes(5);
    private static readonly object _restartLock = new object();

    public AdminController(
        IHostApplicationLifetime applicationLifetime,
        IAuditLogService auditLogService,
        ILogger<AdminController> logger)
    {
        _applicationLifetime = applicationLifetime;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 触发应用优雅重启
    /// 注意：需要容器编排工具（Docker/Kubernetes）配置自动重启策略
    /// </summary>
    [HttpPost("restart")]
    public async Task<IActionResult> Restart(
        [FromQuery] bool confirm = false,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // 冷却期检查（防止 DoS 攻击）
        TimeSpan? remainingTime = null;
        lock (_restartLock)
        {
            var timeSinceLastAttempt = DateTime.UtcNow - _lastRestartAttempt;
            if (timeSinceLastAttempt < RestartCooldown)
            {
                remainingTime = RestartCooldown - timeSinceLastAttempt;
            }
        }

        if (remainingTime.HasValue)
        {
            _logger.LogWarning(
                "Restart attempt rejected due to cooldown. User: {UserId}, IP: {IpAddress}, Remaining: {RemainingSeconds}s",
                userId, ipAddress, (int)remainingTime.Value.TotalSeconds);

            await _auditLogService.LogAsync(
                "ApplicationRestartRejected",
                "Configuration",
                false,
                $"Restart attempt rejected (cooldown active). User: {userId}, IP: {ipAddress}, Remaining: {remainingTime.Value.TotalSeconds:F0}s",
                userName: userId);

            return StatusCode(429, new
            {
                Error = "Too many restart attempts",
                Message = $"Please wait {(int)remainingTime.Value.TotalMinutes + 1} minutes before attempting another restart.",
                RemainingSeconds = (int)remainingTime.Value.TotalSeconds,
                CooldownMinutes = (int)RestartCooldown.TotalMinutes
            });
        }

        if (!confirm)
        {
            return Ok(new
            {
                Warning = "This will trigger a graceful application restart.",
                Message = "Add '?confirm=true' to confirm the restart.",
                Note = "Ensure your container orchestrator (Docker/Kubernetes) is configured to auto-restart.",
                RequesterUserId = userId,
                CooldownMinutes = (int)RestartCooldown.TotalMinutes
            });
        }

        // 更新最后重启尝试时间
        lock (_restartLock)
        {
            _lastRestartAttempt = DateTime.UtcNow;
        }

        // 记录审计日志
        await _auditLogService.LogAsync(
            "ApplicationRestart",
            "Configuration",
            true,
            $"Application restart triggered by {userId} from {ipAddress}",
            userName: userId);

        _logger.LogWarning("Application restart triggered by {UserId} from {IpAddress}", userId, ipAddress);

        // 使用后台任务触发重启，以便响应能够返回给客户端
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // 等待 1 秒让响应返回
            _applicationLifetime.StopApplication();
        });

        return Ok(new
        {
            Message = "Application restart initiated",
            TriggeredBy = userId,
            TriggeredAt = DateTime.UtcNow,
            Note = "The application will stop in ~1 second. Container orchestrator should restart it automatically."
        });
    }

    /// <summary>
    /// 获取应用信息
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            DotNetVersion = Environment.Version.ToString(),
            OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription
        });
    }
}
