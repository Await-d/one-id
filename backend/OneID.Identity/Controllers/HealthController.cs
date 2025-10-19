using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using System.Diagnostics;

namespace OneID.Identity.Controllers;

/// <summary>
/// Identity Server 健康检查控制器
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
            service = "OneID.Identity"
        });
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

    /// <summary>
    /// 详细健康检查
    /// </summary>
    [HttpGet("detailed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetailed()
    {
        var checks = new List<object>();
        var overallStatus = "healthy";

        // 检查数据库
        try
        {
            var dbCheck = Stopwatch.StartNew();
            await _dbContext.Database.CanConnectAsync();
            dbCheck.Stop();
            
            checks.Add(new
            {
                name = "Database",
                status = "healthy",
                responseTime = $"{dbCheck.ElapsedMilliseconds}ms"
            });
        }
        catch (Exception ex)
        {
            overallStatus = "unhealthy";
            checks.Add(new
            {
                name = "Database",
                status = "unhealthy",
                error = ex.Message
            });
            _logger.LogError(ex, "Database health check failed");
        }

        // 检查 OpenIddict 表
        try
        {
            var clientCheck = Stopwatch.StartNew();
            var clientCount = await _dbContext.Set<OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreApplication>().CountAsync();
            clientCheck.Stop();
            
            checks.Add(new
            {
                name = "OpenIddict",
                status = "healthy",
                responseTime = $"{clientCheck.ElapsedMilliseconds}ms",
                details = $"Clients: {clientCount}"
            });
        }
        catch (Exception ex)
        {
            overallStatus = "degraded";
            checks.Add(new
            {
                name = "OpenIddict",
                status = "unhealthy",
                error = ex.Message
            });
            _logger.LogError(ex, "OpenIddict health check failed");
        }

        // 检查登录历史
        try
        {
            var loginCheck = Stopwatch.StartNew();
            var recentLogins = await _dbContext.LoginHistories
                .Where(l => l.LoginTime >= DateTime.UtcNow.AddHours(-1))
                .CountAsync();
            loginCheck.Stop();
            
            checks.Add(new
            {
                name = "LoginHistory",
                status = "healthy",
                responseTime = $"{loginCheck.ElapsedMilliseconds}ms",
                details = $"Logins in last hour: {recentLogins}"
            });
        }
        catch (Exception ex)
        {
            overallStatus = "degraded";
            checks.Add(new
            {
                name = "LoginHistory",
                status = "unhealthy",
                error = ex.Message
            });
            _logger.LogError(ex, "LoginHistory health check failed");
        }

        return Ok(new
        {
            service = "OneID.Identity",
            version = "v1.2.0",
            status = overallStatus,
            timestamp = DateTime.UtcNow,
            checks
        });
    }
}
