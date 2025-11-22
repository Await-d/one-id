using OneID.Shared.Domain;

namespace OneID.Shared.Configuration;

/// <summary>
/// 动态外部认证配置
/// </summary>
public sealed class DynamicExternalAuthOptions
{
    /// <summary>
    /// 外部认证提供者列表
    /// </summary>
    public IReadOnlyList<ExternalAuthProvider> Providers { get; set; } = Array.Empty<ExternalAuthProvider>();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 配置版本号
    /// </summary>
    public long Version { get; set; }
}
