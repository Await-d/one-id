using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 通知服务接口 - 处理各类系统通知
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 发送异常登录通知
    /// </summary>
    Task SendAnomalousLoginNotificationAsync(
        AppUser user,
        LoginHistory loginHistory,
        List<string> anomalyReasons,
        int riskScore);

    /// <summary>
    /// 发送新设备登录通知
    /// </summary>
    Task SendNewDeviceLoginNotificationAsync(
        AppUser user,
        UserDevice device,
        string ipAddress,
        string location);

    /// <summary>
    /// 发送密码修改通知
    /// </summary>
    Task SendPasswordChangedNotificationAsync(AppUser user);

    /// <summary>
    /// 发送账户锁定通知
    /// </summary>
    Task SendAccountLockedNotificationAsync(AppUser user, string reason);

    /// <summary>
    /// 发送 MFA 启用通知
    /// </summary>
    Task SendMfaEnabledNotificationAsync(AppUser user);

    /// <summary>
    /// 批量发送通知
    /// </summary>
    Task<int> SendBulkNotificationsAsync(List<NotificationRequest> notifications);
}

/// <summary>
/// 通知请求
/// </summary>
public class NotificationRequest
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string Subject { get; set; }
    public required string TemplateKey { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

/// <summary>
/// 通知优先级
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

