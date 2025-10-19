namespace OneID.Shared.Domain;

/// <summary>
/// 安全规则（IP黑白名单等）
/// </summary>
public class SecurityRule
{
    public Guid Id { get; set; }
    public string RuleType { get; set; } = string.Empty; // IpWhitelist, IpBlacklist, CountryBlock等
    public string RuleValue { get; set; } = string.Empty; // IP地址或CIDR
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? TenantId { get; set; }
}

