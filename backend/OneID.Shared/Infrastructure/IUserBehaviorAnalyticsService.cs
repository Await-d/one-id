namespace OneID.Shared.Infrastructure;

/// <summary>
/// 用户行为分析服务接口
/// </summary>
public interface IUserBehaviorAnalyticsService
{
    /// <summary>
    /// 获取设备类型统计
    /// </summary>
    Task<Dictionary<string, int>> GetDeviceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取浏览器统计
    /// </summary>
    Task<Dictionary<string, int>> GetBrowserStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取操作系统统计
    /// </summary>
    Task<Dictionary<string, int>> GetOperatingSystemStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取地理位置统计（基于 IP）
    /// </summary>
    Task<Dictionary<string, int>> GetGeographicStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取综合行为分析报告
    /// </summary>
    Task<UserBehaviorReport> GetBehaviorReportAsync(DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// 用户行为报告
/// </summary>
public class UserBehaviorReport
{
    public Dictionary<string, int> DeviceTypes { get; set; } = new();
    public Dictionary<string, int> Browsers { get; set; } = new();
    public Dictionary<string, int> OperatingSystems { get; set; } = new();
    public Dictionary<string, int> Countries { get; set; } = new();
    public Dictionary<string, int> TopBrowserVersions { get; set; } = new();
    public int TotalRequests { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

