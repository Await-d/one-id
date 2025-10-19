using System;
using System.ComponentModel.DataAnnotations;

namespace OneID.Shared.Domain;

/// <summary>
/// Webhook配置
/// </summary>
public class Webhook
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 订阅的事件类型（逗号分隔）
    /// </summary>
    [Required]
    public string Events { get; set; } = string.Empty;

    /// <summary>
    /// 签名密钥（用于验证webhook请求）
    /// </summary>
    [MaxLength(256)]
    public string? Secret { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 自定义请求头（JSON格式）
    /// </summary>
    public string? CustomHeaders { get; set; }

    /// <summary>
    /// 最后触发时间
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// 最后成功时间
    /// </summary>
    public DateTime? LastSuccessAt { get; set; }

    /// <summary>
    /// 最后失败时间
    /// </summary>
    public DateTime? LastFailureAt { get; set; }

    /// <summary>
    /// 失败次数（连续）
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 总触发次数
    /// </summary>
    public int TotalTriggers { get; set; }

    /// <summary>
    /// 成功次数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 租户ID
    /// </summary>
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Webhook事件日志
/// </summary>
public class WebhookLog
{
    public Guid Id { get; set; }

    public Guid WebhookId { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Url { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    [MaxLength(4000)]
    public string? Response { get; set; }

    public bool Success { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? TenantId { get; set; }

    // 导航属性
    public Webhook? Webhook { get; set; }
}

/// <summary>
/// Webhook事件类型
/// </summary>
public static class WebhookEventTypes
{
    // 用户事件
    public const string UserCreated = "user.created";
    public const string UserUpdated = "user.updated";
    public const string UserDeleted = "user.deleted";
    public const string UserLoggedIn = "user.logged_in";
    public const string UserLoggedOut = "user.logged_out";
    public const string UserPasswordChanged = "user.password_changed";
    public const string UserEmailVerified = "user.email_verified";
    public const string UserMfaEnabled = "user.mfa_enabled";
    public const string UserMfaDisabled = "user.mfa_disabled";

    // 安全事件
    public const string AnomalousLoginDetected = "security.anomalous_login";
    public const string NewDeviceDetected = "security.new_device";
    public const string AccountLocked = "security.account_locked";
    public const string AccountUnlocked = "security.account_unlocked";
    public const string PasswordResetRequested = "security.password_reset_requested";

    // 会话事件
    public const string SessionCreated = "session.created";
    public const string SessionRevoked = "session.revoked";

    // 客户端事件
    public const string ClientCreated = "client.created";
    public const string ClientUpdated = "client.updated";
    public const string ClientDeleted = "client.deleted";

    // 角色事件
    public const string RoleAssigned = "role.assigned";
    public const string RoleRemoved = "role.removed";

    /// <summary>
    /// 获取所有事件类型
    /// </summary>
    public static List<string> GetAll()
    {
        return new List<string>
        {
            UserCreated, UserUpdated, UserDeleted, UserLoggedIn, UserLoggedOut,
            UserPasswordChanged, UserEmailVerified, UserMfaEnabled, UserMfaDisabled,
            AnomalousLoginDetected, NewDeviceDetected, AccountLocked, AccountUnlocked,
            PasswordResetRequested, SessionCreated, SessionRevoked,
            ClientCreated, ClientUpdated, ClientDeleted,
            RoleAssigned, RoleRemoved
        };
    }

    /// <summary>
    /// 获取事件显示名称
    /// </summary>
    public static string GetDisplayName(string eventType)
    {
        return eventType switch
        {
            UserCreated => "用户创建",
            UserUpdated => "用户更新",
            UserDeleted => "用户删除",
            UserLoggedIn => "用户登录",
            UserLoggedOut => "用户登出",
            UserPasswordChanged => "密码修改",
            UserEmailVerified => "邮箱验证",
            UserMfaEnabled => "MFA启用",
            UserMfaDisabled => "MFA禁用",
            AnomalousLoginDetected => "异常登录检测",
            NewDeviceDetected => "新设备检测",
            AccountLocked => "账户锁定",
            AccountUnlocked => "账户解锁",
            PasswordResetRequested => "密码重置请求",
            SessionCreated => "会话创建",
            SessionRevoked => "会话撤销",
            ClientCreated => "客户端创建",
            ClientUpdated => "客户端更新",
            ClientDeleted => "客户端删除",
            RoleAssigned => "角色分配",
            RoleRemoved => "角色移除",
            _ => eventType
        };
    }
}

