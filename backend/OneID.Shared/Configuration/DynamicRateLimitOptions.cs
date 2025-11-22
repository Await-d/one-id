using OneID.Shared.Domain;

namespace OneID.Shared.Configuration;

/// <summary>
/// 动态速率限制配置
/// </summary>
public sealed class DynamicRateLimitOptions
{
    /// <summary>
    /// 速率限制设置列表
    /// </summary>
    public IReadOnlyList<RateLimitSetting> Settings { get; set; } = Array.Empty<RateLimitSetting>();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 配置版本号（用于检测变更）
    /// </summary>
    public long Version { get; set; }
}
