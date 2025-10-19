using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using UAParser;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 用户行为分析服务实现
/// </summary>
public class UserBehaviorAnalyticsService : IUserBehaviorAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserBehaviorAnalyticsService> _logger;
    private readonly Parser _uaParser;

    public UserBehaviorAnalyticsService(
        AppDbContext context,
        ILogger<UserBehaviorAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
        _uaParser = Parser.GetDefault();
    }

    public async Task<Dictionary<string, int>> GetDeviceStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = BuildBaseQuery(startDate, endDate);

        var logs = await query
            .Where(log => !string.IsNullOrEmpty(log.UserAgent))
            .Select(log => log.UserAgent!)
            .ToListAsync();

        var deviceCounts = new Dictionary<string, int>();

        foreach (var userAgent in logs)
        {
            try
            {
                var clientInfo = _uaParser.Parse(userAgent);
                var deviceType = DetermineDeviceType(clientInfo);

                if (!deviceCounts.ContainsKey(deviceType))
                    deviceCounts[deviceType] = 0;

                deviceCounts[deviceType]++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse user agent: {UserAgent}", userAgent);
            }
        }

        return deviceCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
    }

    public async Task<Dictionary<string, int>> GetBrowserStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = BuildBaseQuery(startDate, endDate);

        var logs = await query
            .Where(log => !string.IsNullOrEmpty(log.UserAgent))
            .Select(log => log.UserAgent!)
            .ToListAsync();

        var browserCounts = new Dictionary<string, int>();

        foreach (var userAgent in logs)
        {
            try
            {
                var clientInfo = _uaParser.Parse(userAgent);
                var browser = clientInfo.UA.Family ?? "Unknown";

                if (!browserCounts.ContainsKey(browser))
                    browserCounts[browser] = 0;

                browserCounts[browser]++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse user agent: {UserAgent}", userAgent);
            }
        }

        return browserCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
    }

    public async Task<Dictionary<string, int>> GetOperatingSystemStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = BuildBaseQuery(startDate, endDate);

        var logs = await query
            .Where(log => !string.IsNullOrEmpty(log.UserAgent))
            .Select(log => log.UserAgent!)
            .ToListAsync();

        var osCounts = new Dictionary<string, int>();

        foreach (var userAgent in logs)
        {
            try
            {
                var clientInfo = _uaParser.Parse(userAgent);
                var osName = clientInfo.OS.Family ?? "Unknown";

                if (!osCounts.ContainsKey(osName))
                    osCounts[osName] = 0;

                osCounts[osName]++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse user agent: {UserAgent}", userAgent);
            }
        }

        return osCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
    }

    public async Task<Dictionary<string, int>> GetGeographicStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = BuildBaseQuery(startDate, endDate);

        // 简单的 IP 地址地理位置推测（基于 IP 前缀）
        var ipCounts = await query
            .Where(log => !string.IsNullOrEmpty(log.IpAddress))
            .GroupBy(log => log.IpAddress!)
            .Select(g => new { IpAddress = g.Key, Count = g.Count() })
            .ToListAsync();

        var geoCounts = new Dictionary<string, int>();

        foreach (var item in ipCounts)
        {
            var region = DetermineRegionFromIp(item.IpAddress);

            if (!geoCounts.ContainsKey(region))
                geoCounts[region] = 0;

            geoCounts[region] += item.Count;
        }

        return geoCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
    }

    public async Task<UserBehaviorReport> GetBehaviorReportAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var effectiveStartDate = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var effectiveEndDate = endDate ?? DateTime.UtcNow;

        var query = BuildBaseQuery(startDate, endDate);

        var logs = await query
            .Where(log => !string.IsNullOrEmpty(log.UserAgent))
            .ToListAsync();

        var deviceCounts = new Dictionary<string, int>();
        var browserCounts = new Dictionary<string, int>();
        var osCounts = new Dictionary<string, int>();
        var browserVersionCounts = new Dictionary<string, int>();

        foreach (var log in logs)
        {
            try
            {
                var clientInfo = _uaParser.Parse(log.UserAgent!);

                // Device Type
                var deviceType = DetermineDeviceType(clientInfo);
                if (!deviceCounts.ContainsKey(deviceType))
                    deviceCounts[deviceType] = 0;
                deviceCounts[deviceType]++;

                // Browser
                var browser = clientInfo.UA.Family ?? "Unknown";
                if (!browserCounts.ContainsKey(browser))
                    browserCounts[browser] = 0;
                browserCounts[browser]++;

                // OS
                var os = clientInfo.OS.Family ?? "Unknown";
                if (!osCounts.ContainsKey(os))
                    osCounts[os] = 0;
                osCounts[os]++;

                // Browser Version
                var browserWithVersion = $"{browser} {clientInfo.UA.Major ?? "0"}";
                if (!browserVersionCounts.ContainsKey(browserWithVersion))
                    browserVersionCounts[browserWithVersion] = 0;
                browserVersionCounts[browserWithVersion]++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse user agent");
            }
        }

        var uniqueUsers = await query
            .Where(log => log.UserId != null)
            .Select(log => log.UserId)
            .Distinct()
            .CountAsync();

        var countries = await GetGeographicStatisticsAsync(startDate, endDate);

        return new UserBehaviorReport
        {
            DeviceTypes = deviceCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value),
            Browsers = browserCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value),
            OperatingSystems = osCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value),
            TopBrowserVersions = browserVersionCounts.OrderByDescending(x => x.Value).Take(10).ToDictionary(x => x.Key, x => x.Value),
            Countries = countries,
            TotalRequests = logs.Count,
            UniqueUsers = uniqueUsers,
            StartDate = effectiveStartDate,
            EndDate = effectiveEndDate
        };
    }

    private IQueryable<Domain.AuditLog> BuildBaseQuery(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= endDate.Value);
        }

        return query;
    }

    private string DetermineDeviceType(ClientInfo clientInfo)
    {
        var deviceFamily = clientInfo.Device.Family?.ToLowerInvariant() ?? "";

        if (deviceFamily.Contains("mobile") || deviceFamily.Contains("phone"))
            return "Mobile";

        if (deviceFamily.Contains("tablet") || deviceFamily.Contains("ipad"))
            return "Tablet";

        if (deviceFamily.Contains("tv"))
            return "TV";

        if (deviceFamily.Contains("bot") || deviceFamily.Contains("spider") || deviceFamily.Contains("crawler"))
            return "Bot";

        // 通过 OS 推断
        var osFamily = clientInfo.OS.Family?.ToLowerInvariant() ?? "";
        if (osFamily.Contains("android") || osFamily.Contains("ios") || osFamily.Contains("windows phone"))
        {
            // 如果有 OS 信息但 device 显示为 "Other"，可能是移动设备
            if (deviceFamily == "other" || string.IsNullOrEmpty(deviceFamily))
                return "Mobile";
        }

        return "Desktop";
    }

    private string DetermineRegionFromIp(string ipAddress)
    {
        // 简单的 IP 地址地理位置推测
        // 在生产环境中，应该使用专业的 GeoIP 服务，如 MaxMind GeoIP2

        if (ipAddress.StartsWith("127.") || ipAddress == "::1")
            return "Localhost";

        if (ipAddress.StartsWith("10.") || ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("172."))
            return "Private Network";

        // 这里只是示例，实际应该使用 GeoIP 数据库
        // 可以基于 IP 地址段判断区域
        var parts = ipAddress.Split('.');
        if (parts.Length >= 1 && int.TryParse(parts[0], out var firstOctet))
        {
            // 简单示例：根据第一段 IP 判断（实际不准确，仅用于演示）
            if (firstOctet >= 1 && firstOctet <= 126)
                return "North America";
            else if (firstOctet >= 128 && firstOctet <= 191)
                return "Europe";
            else if (firstOctet >= 192 && firstOctet <= 223)
                return "Asia Pacific";
        }

        return "Unknown Region";
    }
}

