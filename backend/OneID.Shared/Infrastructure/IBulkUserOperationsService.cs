namespace OneID.Shared.Infrastructure;

/// <summary>
/// 批量用户操作服务接口
/// </summary>
public interface IBulkUserOperationsService
{
    /// <summary>
    /// 批量分配角色
    /// </summary>
    Task<BulkOperationResult> AssignRolesToUsersAsync(
        List<Guid> userIds, 
        List<string> roleNames, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量撤销角色
    /// </summary>
    Task<BulkOperationResult> RemoveRolesFromUsersAsync(
        List<Guid> userIds, 
        List<string> roleNames, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量启用用户
    /// </summary>
    Task<BulkOperationResult> EnableUsersAsync(
        List<Guid> userIds, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量禁用用户
    /// </summary>
    Task<BulkOperationResult> DisableUsersAsync(
        List<Guid> userIds, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量锁定用户账户
    /// </summary>
    Task<BulkOperationResult> LockUsersAsync(
        List<Guid> userIds, 
        DateTimeOffset? lockoutEnd = null, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量解锁用户账户
    /// </summary>
    Task<BulkOperationResult> UnlockUsersAsync(
        List<Guid> userIds, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量撤销用户会话（强制下线）
    /// </summary>
    Task<BulkOperationResult> RevokeUserSessionsAsync(
        List<Guid> userIds, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量重置密码并发送邮件
    /// </summary>
    Task<BulkOperationResult> ResetPasswordsAsync(
        List<Guid> userIds, 
        bool sendEmail = true,
        string? operatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task<BulkOperationResult> DeleteUsersAsync(
        List<Guid> userIds, 
        string? operatedBy = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 批量操作结果
/// </summary>
public class BulkOperationResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 总数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 成功的用户ID列表
    /// </summary>
    public List<Guid> SucceededUserIds { get; set; } = new();

    /// <summary>
    /// 失败的详情列表
    /// </summary>
    public List<BulkOperationError> Errors { get; set; } = new();

    /// <summary>
    /// 操作消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 批量操作错误详情
/// </summary>
public class BulkOperationError
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

