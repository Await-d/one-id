using OneID.Shared.Domain;

namespace OneID.Shared.Configuration;

/// <summary>
/// 动态 CORS 配置
/// </summary>
public sealed class DynamicCorsOptions
{
    /// <summary>
    /// CORS 设置
    /// </summary>
    public CorsSetting? Setting { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 配置版本号
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// 允许的源列表（解析后）
    /// </summary>
    public IReadOnlyList<string> AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 是否允许任意源
    /// </summary>
    public bool AllowAnyOrigin { get; set; }
}
