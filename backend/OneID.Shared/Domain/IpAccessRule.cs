namespace OneID.Shared.Domain;

/// <summary>
/// IP访问控制规则
/// </summary>
public class IpAccessRule
{
    public int Id { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// IP地址或CIDR范围 (例如: 192.168.1.1 或 192.168.1.0/24)
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 规则类型: Whitelist (白名单) 或 Blacklist (黑名单)
    /// </summary>
    public IpAccessRuleType RuleType { get; set; } = IpAccessRuleType.Whitelist;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 应用范围: Global (全局), User (特定用户), Role (特定角色)
    /// </summary>
    public AccessRuleScope Scope { get; set; } = AccessRuleScope.Global;

    /// <summary>
    /// 目标用户ID（当Scope=User时）
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// 目标角色名称（当Scope=Role时）
    /// </summary>
    public string? TargetRoleName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string? CreatedBy { get; set; }
}

public enum IpAccessRuleType
{
    /// <summary>
    /// 白名单 - 仅允许列表中的IP访问
    /// </summary>
    Whitelist = 0,

    /// <summary>
    /// 黑名单 - 禁止列表中的IP访问
    /// </summary>
    Blacklist = 1
}

public enum AccessRuleScope
{
    /// <summary>
    /// 全局 - 应用于所有用户
    /// </summary>
    Global = 0,

    /// <summary>
    /// 特定用户
    /// </summary>
    User = 1,

    /// <summary>
    /// 特定角色
    /// </summary>
    Role = 2
}

