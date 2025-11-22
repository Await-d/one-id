namespace OneID.Shared.Domain;

/// <summary>
/// 速率限制配置实体
/// </summary>
public class RateLimitSetting
{
    /// <summary>
    /// 主键
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 限流器名称（如：global, login, token, register, password-reset）
    /// </summary>
    public string LimiterName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 允许的请求数量
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// 时间窗口（秒）
    /// </summary>
    public int WindowSeconds { get; set; }

    /// <summary>
    /// 队列限制
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// 排序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后修改人
    /// </summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// 是否已被修改（非默认值）
    /// 用于判断是否应该被 Seed 配置更新
    /// </summary>
    public bool IsModified { get; set; } = false;
}
