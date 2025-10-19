using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using System.Net;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 登录策略验证服务实现
/// </summary>
public class LoginPolicyService : ILoginPolicyService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LoginPolicyService> _logger;

    public LoginPolicyService(AppDbContext dbContext, ILogger<LoginPolicyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> ValidateIpAccessAsync(string ipAddress, Guid? userId = null, List<string>? userRoles = null)
    {
        try
        {
            var clientIp = IPAddress.Parse(ipAddress);

            // 获取所有启用的IP规则，按优先级排序
            var rules = await _dbContext.IpAccessRules
                .Where(r => r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToListAsync();

            // 过滤适用的规则
            var applicableRules = rules.Where(r =>
                r.Scope == AccessRuleScope.Global ||
                (r.Scope == AccessRuleScope.User && r.TargetUserId == userId) ||
                (r.Scope == AccessRuleScope.Role && userRoles != null && userRoles.Contains(r.TargetRoleName ?? ""))
            ).ToList();

            if (!applicableRules.Any())
            {
                // 没有规则，默认允许
                return true;
            }

            // 检查黑名单（优先级最高）
            foreach (var rule in applicableRules.Where(r => r.RuleType == IpAccessRuleType.Blacklist))
            {
                if (IsIpInRange(clientIp, rule.IpAddress))
                {
                    _logger.LogWarning("IP {IpAddress} denied by blacklist rule {RuleName}", ipAddress, rule.Name);
                    return false;
                }
            }

            // 检查白名单
            var whitelistRules = applicableRules.Where(r => r.RuleType == IpAccessRuleType.Whitelist).ToList();
            if (whitelistRules.Any())
            {
                // 如果存在白名单规则，IP必须在白名单中
                var isAllowed = whitelistRules.Any(rule => IsIpInRange(clientIp, rule.IpAddress));
                
                if (!isAllowed)
                {
                    _logger.LogWarning("IP {IpAddress} not in whitelist", ipAddress);
                }
                
                return isAllowed;
            }

            // 没有白名单规则且不在黑名单中，允许访问
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating IP access for {IpAddress}", ipAddress);
            // 验证失败时默认允许，避免锁死系统
            return true;
        }
    }

    public async Task<bool> ValidateLoginTimeAsync(Guid? userId = null, List<string>? userRoles = null, string timeZone = "UTC")
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // 转换到指定时区
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            }
            catch
            {
                // 时区无效，使用UTC
                tz = TimeZoneInfo.Utc;
            }

            var localTime = TimeZoneInfo.ConvertTimeFromUtc(now, tz);

            // 获取所有启用的时间限制规则
            var restrictions = await _dbContext.LoginTimeRestrictions
                .Where(r => r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToListAsync();

            // 过滤适用的规则
            var applicableRestrictions = restrictions.Where(r =>
                r.Scope == AccessRuleScope.Global ||
                (r.Scope == AccessRuleScope.User && r.TargetUserId == userId) ||
                (r.Scope == AccessRuleScope.Role && userRoles != null && userRoles.Contains(r.TargetRoleName ?? ""))
            ).ToList();

            if (!applicableRestrictions.Any())
            {
                // 没有时间限制，允许登录
                return true;
            }

            // 检查所有适用规则（只要有一个规则允许即可）
            foreach (var restriction in applicableRestrictions)
            {
                if (IsTimeAllowed(localTime, restriction))
                {
                    return true;
                }
            }

            _logger.LogWarning("Login time {LocalTime} not allowed for user {UserId}", localTime, userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating login time for user {UserId}", userId);
            // 验证失败时默认允许
            return true;
        }
    }

    public async Task<string> GetAccessDenialReasonAsync(string ipAddress, Guid? userId = null, List<string>? userRoles = null, string timeZone = "UTC")
    {
        var reasons = new List<string>();

        var ipAllowed = await ValidateIpAccessAsync(ipAddress, userId, userRoles);
        if (!ipAllowed)
        {
            reasons.Add("IP address not allowed");
        }

        var timeAllowed = await ValidateLoginTimeAsync(userId, userRoles, timeZone);
        if (!timeAllowed)
        {
            reasons.Add("Login not allowed at this time");
        }

        return reasons.Any() ? string.Join("; ", reasons) : "Access denied";
    }

    #region Private Helper Methods

    /// <summary>
    /// 检查IP是否在指定范围内（支持单个IP和CIDR）
    /// </summary>
    private bool IsIpInRange(IPAddress clientIp, string ipRange)
    {
        try
        {
            // 检查是否为CIDR格式 (例如: 192.168.1.0/24)
            if (ipRange.Contains('/'))
            {
                return IsIpInCidrRange(clientIp, ipRange);
            }
            else
            {
                // 单个IP地址
                var targetIp = IPAddress.Parse(ipRange);
                return clientIp.Equals(targetIp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing IP range {IpRange}", ipRange);
            return false;
        }
    }

    /// <summary>
    /// 检查IP是否在CIDR范围内
    /// </summary>
    private bool IsIpInCidrRange(IPAddress clientIp, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        var baseAddress = IPAddress.Parse(parts[0]);
        var prefixLength = int.Parse(parts[1]);

        // 确保IP地址族匹配
        if (clientIp.AddressFamily != baseAddress.AddressFamily)
        {
            return false;
        }

        var clientBytes = clientIp.GetAddressBytes();
        var baseBytes = baseAddress.GetAddressBytes();

        // 计算需要比较的完整字节数和剩余位数
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        // 比较完整字节
        for (int i = 0; i < fullBytes; i++)
        {
            if (clientBytes[i] != baseBytes[i])
            {
                return false;
            }
        }

        // 比较剩余位
        if (remainingBits > 0 && fullBytes < baseBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((clientBytes[fullBytes] & mask) != (baseBytes[fullBytes] & mask))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查时间是否在允许范围内
    /// </summary>
    private bool IsTimeAllowed(DateTime localTime, LoginTimeRestriction restriction)
    {
        // 检查星期几
        if (!string.IsNullOrEmpty(restriction.AllowedDaysOfWeek))
        {
            var allowedDays = restriction.AllowedDaysOfWeek
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.Parse(d.Trim()))
                .ToList();

            if (!allowedDays.Contains((int)localTime.DayOfWeek))
            {
                return false;
            }
        }

        // 检查时间段
        if (!string.IsNullOrEmpty(restriction.DailyStartTime) && 
            !string.IsNullOrEmpty(restriction.DailyEndTime))
        {
            var startTime = TimeSpan.Parse(restriction.DailyStartTime);
            var endTime = TimeSpan.Parse(restriction.DailyEndTime);
            var currentTime = localTime.TimeOfDay;

            // 处理跨天的情况 (例如: 22:00 - 06:00)
            if (startTime <= endTime)
            {
                // 正常范围 (例如: 09:00 - 18:00)
                if (currentTime < startTime || currentTime > endTime)
                {
                    return false;
                }
            }
            else
            {
                // 跨天范围 (例如: 22:00 - 06:00)
                if (currentTime < startTime && currentTime > endTime)
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion
}

