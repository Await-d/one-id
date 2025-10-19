namespace OneID.Shared.Domain;

/// <summary>
/// 用户会话记录
/// </summary>
public class UserSession
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// 导航属性：用户
    /// </summary>
    public AppUser? User { get; set; }
    
    /// <summary>
    /// 会话令牌（哈希后的）
    /// </summary>
    public string SessionTokenHash { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// 设备信息（解析后的）
    /// </summary>
    public string? DeviceInfo { get; set; }
    
    /// <summary>
    /// 浏览器信息
    /// </summary>
    public string? BrowserInfo { get; set; }
    
    /// <summary>
    /// 操作系统信息
    /// </summary>
    public string? OsInfo { get; set; }
    
    /// <summary>
    /// 地理位置
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 最后活跃时间
    /// </summary>
    public DateTime LastActivityAt { get; set; }
    
    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }
    
    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>
    /// 撤销原因
    /// </summary>
    public string? RevokedReason { get; set; }
    
    /// <summary>
    /// 租户ID（可选，用于多租户）
    /// </summary>
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// 是否为当前活跃会话
    /// </summary>
    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}

