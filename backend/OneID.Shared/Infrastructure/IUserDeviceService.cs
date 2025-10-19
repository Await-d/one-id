using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 用户设备管理服务接口
/// </summary>
public interface IUserDeviceService
{
    /// <summary>
    /// 记录或更新设备信息
    /// </summary>
    Task<UserDevice> RecordDeviceAsync(
        Guid userId,
        string deviceFingerprint,
        DeviceInfo deviceInfo,
        string? ipAddress = null,
        string? location = null);

    /// <summary>
    /// 获取用户的所有设备
    /// </summary>
    Task<List<UserDevice>> GetUserDevicesAsync(Guid userId);

    /// <summary>
    /// 获取设备详情
    /// </summary>
    Task<UserDevice?> GetDeviceAsync(Guid deviceId);

    /// <summary>
    /// 检查设备是否为已知设备
    /// </summary>
    Task<bool> IsKnownDeviceAsync(Guid userId, string deviceFingerprint);

    /// <summary>
    /// 信任设备
    /// </summary>
    Task TrustDeviceAsync(Guid deviceId, bool trusted = true);

    /// <summary>
    /// 激活/停用设备
    /// </summary>
    Task SetDeviceActiveAsync(Guid deviceId, bool active);

    /// <summary>
    /// 删除设备
    /// </summary>
    Task DeleteDeviceAsync(Guid deviceId);

    /// <summary>
    /// 重命名设备
    /// </summary>
    Task RenameDeviceAsync(Guid deviceId, string newName);

    /// <summary>
    /// 获取设备统计信息
    /// </summary>
    Task<DeviceStatistics> GetDeviceStatisticsAsync(Guid userId);
}

/// <summary>
/// 设备信息DTO
/// </summary>
public class DeviceInfo
{
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public string? OsVersion { get; set; }
    public string? DeviceType { get; set; }
    public string? ScreenResolution { get; set; }
    public string? TimeZone { get; set; }
    public string? Language { get; set; }
    public string? Platform { get; set; }
}

/// <summary>
/// 设备统计信息
/// </summary>
public class DeviceStatistics
{
    public int TotalDevices { get; set; }
    public int TrustedDevices { get; set; }
    public int ActiveDevices { get; set; }
    public DateTime? LastDeviceAdded { get; set; }
    public List<DeviceTypeCount> DeviceTypeCounts { get; set; } = new();
}

public class DeviceTypeCount
{
    public string DeviceType { get; set; } = string.Empty;
    public int Count { get; set; }
}

