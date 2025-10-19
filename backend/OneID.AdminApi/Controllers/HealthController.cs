using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using System.Diagnostics;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 系统健康检查控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 基础健康检查（公开访问）
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "v1.2.0",
            service = "OneID.AdminApi"
        });
    }

    /// <summary>
    /// 详细健康检查（需要管理员权限）
    /// </summary>
    [HttpGet("detailed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<HealthCheckResult>> GetDetailed()
    {
        var result = new HealthCheckResult
        {
            Service = "OneID.AdminApi",
            Version = "v1.2.0",
            Timestamp = DateTime.UtcNow,
            Status = "healthy"
        };

        // 检查数据库连接
        try
        {
            var dbCheck = Stopwatch.StartNew();
            await _dbContext.Database.CanConnectAsync();
            dbCheck.Stop();
            
            result.Checks.Add(new HealthCheck
            {
                Name = "Database",
                Status = "healthy",
                ResponseTime = dbCheck.ElapsedMilliseconds,
                Details = "Connection successful"
            });
        }
        catch (Exception ex)
        {
            result.Status = "unhealthy";
            result.Checks.Add(new HealthCheck
            {
                Name = "Database",
                Status = "unhealthy",
                Details = ex.Message
            });
            _logger.LogError(ex, "Database health check failed");
        }

        // 检查用户表
        try
        {
            var userCheck = Stopwatch.StartNew();
            var userCount = await _dbContext.Users.CountAsync();
            userCheck.Stop();
            
            result.Checks.Add(new HealthCheck
            {
                Name = "Users",
                Status = "healthy",
                ResponseTime = userCheck.ElapsedMilliseconds,
                Details = $"Total users: {userCount}"
            });
        }
        catch (Exception ex)
        {
            result.Status = "degraded";
            result.Checks.Add(new HealthCheck
            {
                Name = "Users",
                Status = "unhealthy",
                Details = ex.Message
            });
            _logger.LogError(ex, "Users health check failed");
        }

        // 检查审计日志
        try
        {
            var auditCheck = Stopwatch.StartNew();
            var recentLogs = await _dbContext.AuditLogs
                .Where(a => a.CreatedAt >= DateTime.UtcNow.AddHours(-1))
                .CountAsync();
            auditCheck.Stop();
            
            result.Checks.Add(new HealthCheck
            {
                Name = "AuditLogs",
                Status = "healthy",
                ResponseTime = auditCheck.ElapsedMilliseconds,
                Details = $"Logs in last hour: {recentLogs}"
            });
        }
        catch (Exception ex)
        {
            result.Status = "degraded";
            result.Checks.Add(new HealthCheck
            {
                Name = "AuditLogs",
                Status = "unhealthy",
                Details = ex.Message
            });
            _logger.LogError(ex, "AuditLogs health check failed");
        }

        // 检查系统设置
        try
        {
            var settingsCheck = Stopwatch.StartNew();
            var settingsCount = await _dbContext.SystemSettings.CountAsync();
            settingsCheck.Stop();
            
            result.Checks.Add(new HealthCheck
            {
                Name = "SystemSettings",
                Status = "healthy",
                ResponseTime = settingsCheck.ElapsedMilliseconds,
                Details = $"Total settings: {settingsCount}"
            });
        }
        catch (Exception ex)
        {
            result.Status = "degraded";
            result.Checks.Add(new HealthCheck
            {
                Name = "SystemSettings",
                Status = "unhealthy",
                Details = ex.Message
            });
            _logger.LogError(ex, "SystemSettings health check failed");
        }

        // 检查登录历史
        try
        {
            var loginCheck = Stopwatch.StartNew();
            var recentLogins = await _dbContext.LoginHistories
                .Where(l => l.LoginTime >= DateTime.UtcNow.AddHours(-1))
                .CountAsync();
            loginCheck.Stop();
            
            result.Checks.Add(new HealthCheck
            {
                Name = "LoginHistory",
                Status = "healthy",
                ResponseTime = loginCheck.ElapsedMilliseconds,
                Details = $"Logins in last hour: {recentLogins}"
            });
        }
        catch (Exception ex)
        {
            result.Status = "degraded";
            result.Checks.Add(new HealthCheck
            {
                Name = "LoginHistory",
                Status = "unhealthy",
                Details = ex.Message
            });
            _logger.LogError(ex, "LoginHistory health check failed");
        }

        // 检查设备管理
        try
        {
            var deviceCheck = Stopwatch.StartNew();
            var deviceCount = await _dbContext.UserDevices.CountAsync();
            deviceCheck.Stop();
            
            result.Checks.Add(new HealthCheck
            {
                Name = "UserDevices",
                Status = "healthy",
                ResponseTime = deviceCheck.ElapsedMilliseconds,
                Details = $"Total devices: {deviceCount}"
            });
        }
        catch (Exception ex)
        {
            result.Status = "degraded";
            result.Checks.Add(new HealthCheck
            {
                Name = "UserDevices",
                Status = "unhealthy",
                Details = ex.Message
            });
            _logger.LogError(ex, "UserDevices health check failed");
        }

        return Ok(result);
    }

    /// <summary>
    /// 就绪检查（用于 Kubernetes）
    /// </summary>
    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Ready()
    {
        try
        {
            // 检查数据库是否可以连接
            await _dbContext.Database.CanConnectAsync();
            return Ok(new { status = "ready" });
        }
        catch
        {
            return StatusCode(503, new { status = "not_ready" });
        }
    }

    /// <summary>
    /// 存活检查（用于 Kubernetes）
    /// </summary>
    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Live()
    {
        return Ok(new { status = "alive" });
    }
}

/// <summary>
/// 健康检查结果
/// </summary>
public class HealthCheckResult
{
    public string Service { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<HealthCheck> Checks { get; set; } = new();
}

/// <summary>
/// 单个健康检查项
/// </summary>
public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? ResponseTime { get; set; }
    public string? Details { get; set; }
}

