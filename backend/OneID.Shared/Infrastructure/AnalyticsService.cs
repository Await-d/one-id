using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 分析统计服务接口
/// </summary>
public interface IAnalyticsService
{
    Task<DashboardStatistics> GetDashboardStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    Task<List<LoginTrend>> GetLoginTrendsAsync(
        int days = 7,
        CancellationToken cancellationToken = default);
    
    Task<List<ApiCallStatistic>> GetApiCallStatisticsAsync(
        int topCount = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 仪表板统计数据
/// </summary>
public class DashboardStatistics
{
    public long TotalUsers { get; set; }
    public long ActiveUsers24h { get; set; }
    public long TotalLogins { get; set; }
    public long SuccessfulLogins { get; set; }
    public long FailedLogins { get; set; }
    public double LoginSuccessRate { get; set; }
    public long TotalApiCalls { get; set; }
    public long TotalErrors { get; set; }
    public double ErrorRate { get; set; }
    public long ActiveSessions { get; set; }
}

/// <summary>
/// 登录趋势数据
/// </summary>
public class LoginTrend
{
    public DateTime Date { get; set; }
    public long SuccessfulLogins { get; set; }
    public long FailedLogins { get; set; }
    public long TotalLogins { get; set; }
}

/// <summary>
/// API 调用统计
/// </summary>
public class ApiCallStatistic
{
    public string Action { get; set; } = string.Empty;
    public long CallCount { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// 分析统计服务实现
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        AppDbContext dbContext,
        ILogger<AnalyticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DashboardStatistics> GetDashboardStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        try
        {
            // 总用户数
            var totalUsers = await _dbContext.Users
                .CountAsync(cancellationToken);

            // 24小时活跃用户（根据审计日志）
            var activeUsers24h = await _dbContext.AuditLogs
                .Where(a => a.CreatedAt >= DateTime.UtcNow.AddHours(-24) && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            // 登录统计
            var loginLogs = await _dbContext.AuditLogs
                .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate &&
                           (a.Action.Contains("Login") || a.Action.Contains("登录")))
                .GroupBy(a => a.Success)
                .Select(g => new { Success = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var successfulLogins = loginLogs.FirstOrDefault(x => x.Success)?.Count ?? 0;
            var failedLogins = loginLogs.FirstOrDefault(x => !x.Success)?.Count ?? 0;
            var totalLogins = successfulLogins + failedLogins;

            // API 调用统计
            var totalApiCalls = await _dbContext.AuditLogs
                .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
                .CountAsync(cancellationToken);

            var totalErrors = await _dbContext.AuditLogs
                .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate && !a.Success)
                .CountAsync(cancellationToken);

            // 活跃会话数
            var activeSessions = await _dbContext.UserSessions
                .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                .CountAsync(cancellationToken);

            return new DashboardStatistics
            {
                TotalUsers = totalUsers,
                ActiveUsers24h = activeUsers24h,
                TotalLogins = totalLogins,
                SuccessfulLogins = successfulLogins,
                FailedLogins = failedLogins,
                LoginSuccessRate = totalLogins > 0 ? (double)successfulLogins / totalLogins * 100 : 0,
                TotalApiCalls = totalApiCalls,
                TotalErrors = totalErrors,
                ErrorRate = totalApiCalls > 0 ? (double)totalErrors / totalApiCalls * 100 : 0,
                ActiveSessions = activeSessions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            throw;
        }
    }

    public async Task<List<LoginTrend>> GetLoginTrendsAsync(
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            var endDate = DateTime.UtcNow.Date.AddDays(1);

            var loginLogs = await _dbContext.AuditLogs
                .Where(a => a.CreatedAt >= startDate && a.CreatedAt < endDate &&
                           (a.Action.Contains("Login") || a.Action.Contains("登录")))
                .Select(a => new { a.CreatedAt, a.Success })
                .ToListAsync(cancellationToken);

            // 按日期分组统计
            var trends = loginLogs
                .GroupBy(a => a.CreatedAt.Date)
                .Select(g => new LoginTrend
                {
                    Date = g.Key,
                    SuccessfulLogins = g.Count(x => x.Success),
                    FailedLogins = g.Count(x => !x.Success),
                    TotalLogins = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            // 填充缺失的日期（显示为0）
            var allDates = Enumerable.Range(0, days)
                .Select(i => DateTime.UtcNow.Date.AddDays(-days + i + 1))
                .ToList();

            var result = allDates.Select(date =>
            {
                var existing = trends.FirstOrDefault(t => t.Date == date);
                return existing ?? new LoginTrend
                {
                    Date = date,
                    SuccessfulLogins = 0,
                    FailedLogins = 0,
                    TotalLogins = 0
                };
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving login trends");
            throw;
        }
    }

    public async Task<List<ApiCallStatistic>> GetApiCallStatisticsAsync(
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var last7Days = DateTime.UtcNow.AddDays(-7);

            var statistics = await _dbContext.AuditLogs
                .Where(a => a.CreatedAt >= last7Days)
                .GroupBy(a => a.Action)
                .Select(g => new
                {
                    Action = g.Key,
                    CallCount = g.Count(),
                    SuccessCount = g.Count(x => x.Success),
                    FailureCount = g.Count(x => !x.Success)
                })
                .OrderByDescending(x => x.CallCount)
                .Take(topCount)
                .ToListAsync(cancellationToken);

            return statistics.Select(s => new ApiCallStatistic
            {
                Action = s.Action,
                CallCount = s.CallCount,
                SuccessCount = s.SuccessCount,
                FailureCount = s.FailureCount,
                SuccessRate = s.CallCount > 0 ? (double)s.SuccessCount / s.CallCount * 100 : 0
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API call statistics");
            throw;
        }
    }
}

