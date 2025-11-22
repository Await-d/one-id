namespace OneID.Shared.Domain;

/// <summary>
/// 配置版本号跟踪表
/// 用于高效检测配置变更，避免轮询服务查询多个配置表
/// </summary>
public class ConfigurationVersion
{
    /// <summary>
    /// 主键ID（全局只有一条记录，ID固定为1）
    /// </summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// 当前版本号（任何配置变更时递增）
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// 最后更新时间（UTC）
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 触发更新的配置类型（用于调试和审计）
    /// 例如: "RateLimitSettings", "CorsSettings", "ExternalAuthProviders" 等
    /// </summary>
    public string? LastChangedBy { get; set; }
}
