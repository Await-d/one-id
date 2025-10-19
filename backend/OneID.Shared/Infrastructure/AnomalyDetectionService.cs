using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using UAParser;

namespace OneID.Shared.Infrastructure;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AnomalyDetectionService> _logger;
    private readonly Parser _uaParser;

    public AnomalyDetectionService(
        AppDbContext context,
        ILogger<AnomalyDetectionService> logger)
    {
        _context = context;
        _logger = logger;
        _uaParser = Parser.GetDefault();
    }

    public async Task<LoginAnomalyResult> RecordAndAnalyzeLoginAsync(
        Guid userId,
        string? userName,
        string? ipAddress,
        string? userAgent,
        bool success,
        string? failureReason = null)
    {
        var result = new LoginAnomalyResult { LoginHistoryId = Guid.NewGuid() };
        
        // 解析 UserAgent
        var (browser, os, deviceType) = ParseUserAgent(userAgent);
        
        // 简单的地理位置推测
        var (country, city) = EstimateLocation(ipAddress);

        // 创建登录历史记录
        var loginHistory = new LoginHistory
        {
            Id = result.LoginHistoryId,
            UserId = userId,
            UserName = userName,
            LoginTime = DateTime.UtcNow,
            IpAddress = ipAddress,
            Country = country,
            City = city,
            UserAgent = userAgent,
            Browser = browser,
            OperatingSystem = os,
            DeviceType = deviceType,
            Success = success,
            FailureReason = failureReason,
            RiskScore = 0
        };

        // 仅对成功登录进行异常检测
        if (success)
        {
            result = await AnalyzeAnomaliesAsync(userId, loginHistory);
            loginHistory.IsAnomalous = result.IsAnomalous;
            loginHistory.AnomalyReason = result.AnomalyReasons.Count > 0 
                ? string.Join("; ", result.AnomalyReasons) 
                : null;
            loginHistory.RiskScore = result.RiskScore;
        }

        _context.LoginHistories.Add(loginHistory);
        await _context.SaveChangesAsync();

        return result;
    }

    private async Task<LoginAnomalyResult> AnalyzeAnomaliesAsync(Guid userId, LoginHistory currentLogin)
    {
        var result = new LoginAnomalyResult();
        var anomalies = new List<string>();
        var riskScore = 0;

        // 获取用户最近30天的登录历史
        var recentLogins = await _context.LoginHistories
            .Where(h => h.UserId == userId && 
                       h.Success && 
                       h.LoginTime >= DateTime.UtcNow.AddDays(-30) &&
                       h.LoginTime < DateTime.UtcNow)
            .OrderByDescending(h => h.LoginTime)
            .Take(100)
            .ToListAsync();

        if (recentLogins.Count < 3)
        {
            // 新用户或历史记录少，给予低风险
            result.IsAnomalous = false;
            result.RiskScore = 10;
            return result;
        }

        // 1. 检测异地登录
        var lastKnownCountry = recentLogins.FirstOrDefault(l => !string.IsNullOrEmpty(l.Country))?.Country;
        if (!string.IsNullOrEmpty(currentLogin.Country) && 
            !string.IsNullOrEmpty(lastKnownCountry) &&
            currentLogin.Country != lastKnownCountry)
        {
            anomalies.Add($"异地登录：从 {lastKnownCountry} 切换到 {currentLogin.Country}");
            riskScore += 40;
        }

        // 2. 检测异常时间登录（凌晨2-6点）
        var loginHour = currentLogin.LoginTime.Hour;
        var normalHours = recentLogins
            .Select(l => l.LoginTime.Hour)
            .Where(h => h >= 8 && h <= 22)
            .Count();
        
        if (loginHour >= 2 && loginHour <= 6 && normalHours > recentLogins.Count * 0.8)
        {
            anomalies.Add($"异常时间登录：凌晨 {loginHour}:00");
            riskScore += 20;
        }

        // 3. 检测新设备/浏览器
        var knownBrowsers = recentLogins
            .Where(l => !string.IsNullOrEmpty(l.Browser))
            .Select(l => l.Browser)
            .Distinct()
            .ToList();
        
        if (!string.IsNullOrEmpty(currentLogin.Browser) && 
            knownBrowsers.Any() && 
            !knownBrowsers.Contains(currentLogin.Browser))
        {
            anomalies.Add($"新浏览器：{currentLogin.Browser}");
            riskScore += 15;
        }

        // 4. 检测登录频率异常（短时间内多次登录）
        var recentAttempts = await _context.LoginHistories
            .Where(h => h.UserId == userId && 
                       h.LoginTime >= DateTime.UtcNow.AddMinutes(-10))
            .CountAsync();
        
        if (recentAttempts > 5)
        {
            anomalies.Add($"高频登录：10分钟内 {recentAttempts} 次");
            riskScore += 30;
        }

        // 5. 检测不同IP快速切换
        var lastLogin = recentLogins.FirstOrDefault();
        if (lastLogin != null && 
            !string.IsNullOrEmpty(lastLogin.IpAddress) &&
            !string.IsNullOrEmpty(currentLogin.IpAddress) &&
            lastLogin.IpAddress != currentLogin.IpAddress)
        {
            var timeDiff = (currentLogin.LoginTime - lastLogin.LoginTime).TotalMinutes;
            if (timeDiff < 5)
            {
                anomalies.Add($"IP快速切换：{timeDiff:F1}分钟内从 {lastLogin.IpAddress} 切换到 {currentLogin.IpAddress}");
                riskScore += 35;
            }
        }

        result.IsAnomalous = riskScore >= 40; // 风险评分40以上视为异常
        result.AnomalyReasons = anomalies;
        result.RiskScore = Math.Min(riskScore, 100);

        return result;
    }

    public async Task<List<LoginHistory>> GetAnomalousLoginsAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.LoginHistories
            .Where(h => h.UserId == userId && h.IsAnomalous);

        if (startDate.HasValue)
            query = query.Where(h => h.LoginTime >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(h => h.LoginTime <= endDate.Value);

        return await query
            .OrderByDescending(h => h.LoginTime)
            .ToListAsync();
    }

    public async Task<List<LoginHistory>> GetAllAnomalousLoginsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.LoginHistories.Where(h => h.IsAnomalous);

        if (startDate.HasValue)
            query = query.Where(h => h.LoginTime >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(h => h.LoginTime <= endDate.Value);

        return await query
            .OrderByDescending(h => h.RiskScore)
            .ThenByDescending(h => h.LoginTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task MarkAsNotifiedAsync(Guid loginHistoryId)
    {
        var login = await _context.LoginHistories.FindAsync(loginHistoryId);
        if (login != null)
        {
            login.UserNotified = true;
            await _context.SaveChangesAsync();
        }
    }

    private (string browser, string os, string deviceType) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return ("Unknown", "Unknown", "Unknown");

        try
        {
            var clientInfo = _uaParser.Parse(userAgent);
            var browser = clientInfo.UA.Family ?? "Unknown";
            var os = clientInfo.OS.Family ?? "Unknown";
            var deviceType = DetermineDeviceType(clientInfo);
            return (browser, os, deviceType);
        }
        catch
        {
            return ("Unknown", "Unknown", "Unknown");
        }
    }

    private string DetermineDeviceType(ClientInfo clientInfo)
    {
        var deviceFamily = clientInfo.Device.Family?.ToLowerInvariant() ?? "";
        if (deviceFamily.Contains("mobile") || deviceFamily.Contains("phone")) return "Mobile";
        if (deviceFamily.Contains("tablet") || deviceFamily.Contains("ipad")) return "Tablet";
        return "Desktop";
    }

    private (string? country, string? city) EstimateLocation(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return (null, null);
        
        if (ipAddress.StartsWith("127.") || ipAddress == "::1")
            return ("Localhost", "Localhost");
        
        if (ipAddress.StartsWith("10.") || ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("172."))
            return ("Private Network", "Private Network");

        // 简单的地理位置推测（生产环境应使用 MaxMind GeoIP2）
        var parts = ipAddress.Split('.');
        if (parts.Length >= 1 && int.TryParse(parts[0], out var firstOctet))
        {
            if (firstOctet >= 1 && firstOctet <= 126) return ("United States", "Unknown");
            if (firstOctet >= 128 && firstOctet <= 191) return ("Europe", "Unknown");
            if (firstOctet >= 192 && firstOctet <= 223) return ("Asia", "Unknown");
        }

        return ("Unknown", "Unknown");
    }
}

