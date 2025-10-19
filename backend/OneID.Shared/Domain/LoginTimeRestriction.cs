namespace OneID.Shared.Domain;

/// <summary>
/// 登录时间限制规则
/// </summary>
public class LoginTimeRestriction
{
    public int Id { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 应用范围: Global, User, Role
    /// </summary>
    public AccessRuleScope Scope { get; set; } = AccessRuleScope.Global;

    /// <summary>
    /// 目标用户ID
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// 目标角色名称
    /// </summary>
    public string? TargetRoleName { get; set; }

    /// <summary>
    /// 允许的星期几（逗号分隔，0=周日，1=周一...6=周六）
    /// 例如: "1,2,3,4,5" 表示周一到周五
    /// </summary>
    public string? AllowedDaysOfWeek { get; set; }

    /// <summary>
    /// 每日允许开始时间 (HH:mm 格式，例如 "09:00")
    /// </summary>
    public string? DailyStartTime { get; set; }

    /// <summary>
    /// 每日允许结束时间 (HH:mm 格式，例如 "18:00")
    /// </summary>
    public string? DailyEndTime { get; set; }

    /// <summary>
    /// 时区 (例如: "Asia/Shanghai", "UTC")
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级
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

