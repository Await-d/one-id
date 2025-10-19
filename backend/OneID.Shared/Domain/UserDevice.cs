namespace OneID.Shared.Domain;

/// <summary>
/// 用户设备实体 - 用于设备指纹识别和管理
/// </summary>
public class UserDevice
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 设备指纹（唯一标识）
    /// </summary>
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（用户自定义或自动生成）
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 浏览器信息
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 浏览器版本
    /// </summary>
    public string? BrowserVersion { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 操作系统版本
    /// </summary>
    public string? OsVersion { get; set; }

    /// <summary>
    /// 设备类型（Desktop, Mobile, Tablet）
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// 屏幕分辨率
    /// </summary>
    public string? ScreenResolution { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 平台信息
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// 首次使用时间
    /// </summary>
    public DateTime FirstUsedAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    /// 最后使用的IP地址
    /// </summary>
    public string? LastIpAddress { get; set; }

    /// <summary>
    /// 最后使用的地理位置
    /// </summary>
    public string? LastLocation { get; set; }

    /// <summary>
    /// 是否信任此设备
    /// </summary>
    public bool IsTrusted { get; set; }

    /// <summary>
    /// 是否激活（未激活的设备不能使用）
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public Guid? TenantId { get; set; }
}

