using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public interface ISecurityRuleService
{
    Task<IReadOnlyList<SecurityRule>> GetAllRulesAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);
    Task<SecurityRule?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SecurityRule> CreateRuleAsync(string ruleType, string ruleValue, string? description = null, CancellationToken cancellationToken = default);
    Task<SecurityRule> UpdateRuleAsync(Guid id, string ruleValue, string? description = null, CancellationToken cancellationToken = default);
    Task<SecurityRule> ToggleRuleAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsIpAllowedAsync(string ipAddress, CancellationToken cancellationToken = default);
    bool MatchesCidr(string ipAddress, string cidr);
}

public sealed class SecurityRuleService : ISecurityRuleService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SecurityRuleService> _logger;

    public SecurityRuleService(AppDbContext dbContext, ILogger<SecurityRuleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SecurityRule>> GetAllRulesAsync(
        bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SecurityRules.AsQueryable();

        if (!includeDisabled)
        {
            query = query.Where(r => r.IsEnabled);
        }

        return await query
            .OrderBy(r => r.RuleType)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SecurityRule?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SecurityRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<SecurityRule> CreateRuleAsync(
        string ruleType,
        string ruleValue,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        // 验证规则类型
        if (!IsValidRuleType(ruleType))
        {
            throw new ArgumentException($"Invalid rule type: {ruleType}", nameof(ruleType));
        }

        // 验证规则值
        if (ruleType is "IpWhitelist" or "IpBlacklist")
        {
            if (!IsValidIpOrCidr(ruleValue))
            {
                throw new ArgumentException($"Invalid IP address or CIDR: {ruleValue}", nameof(ruleValue));
            }
        }

        var rule = new SecurityRule
        {
            Id = Guid.NewGuid(),
            RuleType = ruleType,
            RuleValue = ruleValue,
            Description = description,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.SecurityRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created security rule {RuleId} ({RuleType}: {RuleValue})", rule.Id, rule.RuleType, rule.RuleValue);

        return rule;
    }

    public async Task<SecurityRule> UpdateRuleAsync(
        Guid id,
        string ruleValue,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(id, cancellationToken);
        if (rule == null)
        {
            throw new InvalidOperationException($"Security rule {id} not found");
        }

        // 验证规则值
        if (rule.RuleType is "IpWhitelist" or "IpBlacklist")
        {
            if (!IsValidIpOrCidr(ruleValue))
            {
                throw new ArgumentException($"Invalid IP address or CIDR: {ruleValue}", nameof(ruleValue));
            }
        }

        rule.RuleValue = ruleValue;
        rule.Description = description;
        rule.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated security rule {RuleId} ({RuleType}: {RuleValue})", rule.Id, rule.RuleType, rule.RuleValue);

        return rule;
    }

    public async Task<SecurityRule> ToggleRuleAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(id, cancellationToken);
        if (rule == null)
        {
            throw new InvalidOperationException($"Security rule {id} not found");
        }

        rule.IsEnabled = isEnabled;
        rule.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{Action} security rule {RuleId} ({RuleType}: {RuleValue})",
            isEnabled ? "Enabled" : "Disabled",
            rule.Id,
            rule.RuleType,
            rule.RuleValue);

        return rule;
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await GetRuleByIdAsync(id, cancellationToken);
        if (rule == null)
        {
            throw new InvalidOperationException($"Security rule {id} not found");
        }

        _dbContext.SecurityRules.Remove(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted security rule {RuleId} ({RuleType}: {RuleValue})", rule.Id, rule.RuleType, rule.RuleValue);
    }

    public async Task<bool> IsIpAllowedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        // 获取所有启用的 IP 规则
        var rules = await _dbContext.SecurityRules
            .Where(r => r.IsEnabled && (r.RuleType == "IpWhitelist" || r.RuleType == "IpBlacklist"))
            .ToListAsync(cancellationToken);

        // 检查黑名单
        var blacklist = rules.Where(r => r.RuleType == "IpBlacklist").ToList();
        foreach (var rule in blacklist)
        {
            if (MatchesIpOrCidr(ipAddress, rule.RuleValue))
            {
                _logger.LogWarning("IP {IpAddress} is blocked by blacklist rule {RuleId} ({RuleValue})", ipAddress, rule.Id, rule.RuleValue);
                return false;
            }
        }

        // 检查白名单
        var whitelist = rules.Where(r => r.RuleType == "IpWhitelist").ToList();
        if (whitelist.Count > 0)
        {
            var allowed = whitelist.Any(rule => MatchesIpOrCidr(ipAddress, rule.RuleValue));
            if (!allowed)
            {
                _logger.LogWarning("IP {IpAddress} is not in whitelist", ipAddress);
                return false;
            }
        }

        return true;
    }

    public bool MatchesCidr(string ipAddress, string cidr)
    {
        return MatchesIpOrCidr(ipAddress, cidr);
    }

    private bool MatchesIpOrCidr(string ipAddress, string ipOrCidr)
    {
        try
        {
            // 解析 IP 地址
            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                return false;
            }

            // 检查是否是 CIDR 格式
            if (ipOrCidr.Contains('/'))
            {
                return MatchesCidrInternal(ip, ipOrCidr);
            }

            // 直接 IP 地址比较
            return IPAddress.TryParse(ipOrCidr, out var ruleIp) && ip.Equals(ruleIp);
        }
        catch
        {
            return false;
        }
    }

    private bool MatchesCidrInternal(IPAddress ipAddress, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
            {
                return false;
            }

            if (!IPAddress.TryParse(parts[0], out var networkIp) || !int.TryParse(parts[1], out var prefixLength))
            {
                return false;
            }

            // 仅支持 IPv4
            if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                networkIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return false;
            }

            var ipBytes = ipAddress.GetAddressBytes();
            var networkBytes = networkIp.GetAddressBytes();

            // 计算子网掩码
            var maskBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (prefixLength >= 8)
                {
                    maskBytes[i] = 255;
                    prefixLength -= 8;
                }
                else if (prefixLength > 0)
                {
                    maskBytes[i] = (byte)(255 << (8 - prefixLength));
                    prefixLength = 0;
                }
                else
                {
                    maskBytes[i] = 0;
                }
            }

            // 比较网络地址
            for (int i = 0; i < 4; i++)
            {
                if ((ipBytes[i] & maskBytes[i]) != (networkBytes[i] & maskBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidRuleType(string ruleType)
    {
        return ruleType is "IpWhitelist" or "IpBlacklist" or "CountryBlock" or "RegionBlock";
    }

    private bool IsValidIpOrCidr(string value)
    {
        try
        {
            if (value.Contains('/'))
            {
                // CIDR 格式
                var parts = value.Split('/');
                return parts.Length == 2 &&
                       IPAddress.TryParse(parts[0], out _) &&
                       int.TryParse(parts[1], out var prefix) &&
                       prefix >= 0 && prefix <= 32;
            }

            // 单个 IP 地址
            return IPAddress.TryParse(value, out _);
        }
        catch
        {
            return false;
        }
    }
}

