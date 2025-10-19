namespace OneID.Shared.Infrastructure;

/// <summary>
/// 异常登录检测服务接口
/// </summary>
public interface IAnomalyDetectionService
{
    /// <summary>
    /// 记录登录尝试并检测异常
    /// </summary>
    Task<LoginAnomalyResult> RecordAndAnalyzeLoginAsync(
        Guid userId,
        string? userName,
        string? ipAddress,
        string? userAgent,
        bool success,
        string? failureReason = null);

    /// <summary>
    /// 获取用户的异常登录历史
    /// </summary>
    Task<List<Domain.LoginHistory>> GetAnomalousLoginsAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// 获取所有异常登录（管理员查看）
    /// </summary>
    Task<List<Domain.LoginHistory>> GetAllAnomalousLoginsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// 标记异常登录已处理
    /// </summary>
    Task MarkAsNotifiedAsync(Guid loginHistoryId);
}

/// <summary>
/// 登录异常分析结果
/// </summary>
public class LoginAnomalyResult
{
    public bool IsAnomalous { get; set; }
    public List<string> AnomalyReasons { get; set; } = new();
    public int RiskScore { get; set; }
    public Guid LoginHistoryId { get; set; }
}

