using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ISystemSettingsService _systemSettings;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ISystemSettingsService systemSettings,
        IWebhookService webhookService,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _systemSettings = systemSettings;
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// 发送异常登录通知
    /// </summary>
    public async Task SendAnomalousLoginNotificationAsync(
        AppUser user,
        LoginHistory loginHistory,
        List<string> anomalyReasons,
        int riskScore)
    {
        try
        {
            // 检查是否启用异常登录通知
            var notificationEnabled = await _systemSettings.GetBoolValueAsync("notification-anomalous-login-enabled", true);
            if (!notificationEnabled)
            {
                _logger.LogInformation("Anomalous login notification is disabled");
                return;
            }

            // 检查风险评分阈值
            var notificationThreshold = await _systemSettings.GetIntValueAsync("notification-anomalous-login-threshold", 40);
            if (riskScore < notificationThreshold)
            {
                _logger.LogInformation(
                    "Risk score {RiskScore} is below threshold {Threshold}, skipping notification",
                    riskScore, notificationThreshold);
                return;
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("User {UserId} has no email address, cannot send notification", user.Id);
                return;
            }

            // 获取邮件模板
            var template = await _emailTemplateService.GetTemplateByKeyAsync("anomalous-login");
            if (template == null)
            {
                _logger.LogWarning("Email template 'anomalous-login' not found");
                // 使用默认模板
                await SendDefaultAnomalousLoginEmailAsync(user, loginHistory, anomalyReasons, riskScore);
                return;
            }

            // 准备变量
            var variables = new Dictionary<string, string>
            {
                ["UserName"] = user.UserName ?? "",
                ["DisplayName"] = user.DisplayName ?? user.UserName ?? "",
                ["Email"] = user.Email,
                ["LoginTime"] = loginHistory.LoginTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["IpAddress"] = loginHistory.IpAddress ?? "Unknown",
                ["Location"] = $"{loginHistory.Country}, {loginHistory.City}",
                ["Browser"] = loginHistory.Browser ?? "Unknown",
                ["OperatingSystem"] = loginHistory.OperatingSystem ?? "Unknown",
                ["DeviceType"] = loginHistory.DeviceType ?? "Unknown",
                ["RiskScore"] = riskScore.ToString(),
                ["RiskLevel"] = GetRiskLevel(riskScore),
                ["AnomalyReasons"] = string.Join(", ", anomalyReasons),
                ["DashboardUrl"] = await _systemSettings.GetValueOrDefaultAsync("admin-portal-url", "http://localhost:5174") ?? ""
            };

            // 替换变量
            var subject = _emailTemplateService.ReplaceVariables(template.Subject, variables);
            var htmlBody = _emailTemplateService.ReplaceVariables(template.HtmlBody ?? "", variables);
            var textBody = _emailTemplateService.ReplaceVariables(template.TextBody ?? "", variables);

            // 发送邮件
            await _emailService.SendEmailAsync(
                to: user.Email,
                subject: subject,
                htmlBody: htmlBody,
                textBody: textBody);

            _logger.LogInformation(
                "Anomalous login notification sent to {Email} for user {UserId}",
                user.Email, user.Id);

            // 触发Webhook事件
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webhookService.TriggerEventAsync(
                        WebhookEventTypes.AnomalousLoginDetected,
                        new
                        {
                            userId = user.Id,
                            userName = user.UserName,
                            email = user.Email,
                            loginTime = loginHistory.LoginTime,
                            ipAddress = loginHistory.IpAddress,
                            location = $"{loginHistory.Country}, {loginHistory.City}",
                            browser = loginHistory.Browser,
                            operatingSystem = loginHistory.OperatingSystem,
                            riskScore = riskScore,
                            riskLevel = GetRiskLevel(riskScore),
                            anomalyReasons = anomalyReasons
                        },
                        user.TenantId);
                }
                catch (Exception webhookEx)
                {
                    _logger.LogError(webhookEx, "Failed to trigger anomalous login webhook for user {UserId}", user.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send anomalous login notification to user {UserId}", user.Id);
        }
    }

    /// <summary>
    /// 发送新设备登录通知
    /// </summary>
    public async Task SendNewDeviceLoginNotificationAsync(
        AppUser user,
        UserDevice device,
        string ipAddress,
        string location)
    {
        try
        {
            // 检查是否启用新设备通知
            var notificationEnabled = await _systemSettings.GetBoolValueAsync("notification-new-device-enabled", true);
            if (!notificationEnabled)
            {
                _logger.LogInformation("New device notification is disabled");
                return;
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("User {UserId} has no email address, cannot send notification", user.Id);
                return;
            }

            // 获取邮件模板
            var template = await _emailTemplateService.GetTemplateByKeyAsync("new-device-login");
            if (template == null)
            {
                _logger.LogWarning("Email template 'new-device-login' not found");
                await SendDefaultNewDeviceEmailAsync(user, device, ipAddress, location);
                return;
            }

            // 准备变量
            var variables = new Dictionary<string, string>
            {
                ["UserName"] = user.UserName ?? "",
                ["DisplayName"] = user.DisplayName ?? user.UserName ?? "",
                ["Email"] = user.Email,
                ["DeviceName"] = device.DeviceName ?? "Unknown Device",
                ["Browser"] = device.Browser ?? "Unknown",
                ["BrowserVersion"] = device.BrowserVersion ?? "",
                ["OperatingSystem"] = device.OperatingSystem ?? "Unknown",
                ["OsVersion"] = device.OsVersion ?? "",
                ["DeviceType"] = device.DeviceType ?? "Unknown",
                ["IpAddress"] = ipAddress,
                ["Location"] = location,
                ["LoginTime"] = device.FirstUsedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["DashboardUrl"] = await _systemSettings.GetValueOrDefaultAsync("admin-portal-url", "http://localhost:5174") ?? ""
            };

            var subject = _emailTemplateService.ReplaceVariables(template.Subject, variables);
            var htmlBody = _emailTemplateService.ReplaceVariables(template.HtmlBody ?? "", variables);
            var textBody = _emailTemplateService.ReplaceVariables(template.TextBody ?? "", variables);

            await _emailService.SendEmailAsync(
                to: user.Email,
                subject: subject,
                htmlBody: htmlBody,
                textBody: textBody);

            _logger.LogInformation(
                "New device notification sent to {Email} for user {UserId}",
                user.Email, user.Id);

            // 触发Webhook事件
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webhookService.TriggerEventAsync(
                        WebhookEventTypes.NewDeviceDetected,
                        new
                        {
                            userId = user.Id,
                            userName = user.UserName,
                            email = user.Email,
                            deviceName = device.DeviceName,
                            browser = device.Browser,
                            operatingSystem = device.OperatingSystem,
                            deviceType = device.DeviceType,
                            ipAddress = ipAddress,
                            location = location,
                            loginTime = device.FirstUsedAt
                        },
                        user.TenantId);
                }
                catch (Exception webhookEx)
                {
                    _logger.LogError(webhookEx, "Failed to trigger new device webhook for user {UserId}", user.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new device notification to user {UserId}", user.Id);
        }
    }

    /// <summary>
    /// 发送密码修改通知
    /// </summary>
    public async Task SendPasswordChangedNotificationAsync(AppUser user)
    {
        try
        {
            var notificationEnabled = await _systemSettings.GetBoolValueAsync("notification-password-changed-enabled", true);
            if (!notificationEnabled || string.IsNullOrEmpty(user.Email))
            {
                return;
            }

            var template = await _emailTemplateService.GetTemplateByKeyAsync("password-changed");
            if (template == null)
            {
                return;
            }

            var variables = new Dictionary<string, string>
            {
                ["UserName"] = user.UserName ?? "",
                ["DisplayName"] = user.DisplayName ?? user.UserName ?? "",
                ["Email"] = user.Email,
                ["ChangeTime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            };

            var subject = _emailTemplateService.ReplaceVariables(template.Subject, variables);
            var htmlBody = _emailTemplateService.ReplaceVariables(template.HtmlBody ?? "", variables);
            var textBody = _emailTemplateService.ReplaceVariables(template.TextBody ?? "", variables);

            await _emailService.SendEmailAsync(user.Email, subject, htmlBody, textBody);

            _logger.LogInformation("Password changed notification sent to {Email}", user.Email);

            // 触发Webhook事件
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webhookService.TriggerEventAsync(
                        WebhookEventTypes.UserPasswordChanged,
                        new
                        {
                            userId = user.Id,
                            userName = user.UserName,
                            email = user.Email,
                            changeTime = DateTime.UtcNow
                        },
                        user.TenantId);
                }
                catch (Exception webhookEx)
                {
                    _logger.LogError(webhookEx, "Failed to trigger password changed webhook for user {UserId}", user.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to user {UserId}", user.Id);
        }
    }

    /// <summary>
    /// 发送账户锁定通知
    /// </summary>
    public async Task SendAccountLockedNotificationAsync(AppUser user, string reason)
    {
        try
        {
            var notificationEnabled = await _systemSettings.GetBoolValueAsync("notification-account-locked-enabled", true);
            if (!notificationEnabled || string.IsNullOrEmpty(user.Email))
            {
                return;
            }

            var template = await _emailTemplateService.GetTemplateByKeyAsync("account-locked");
            if (template == null)
            {
                return;
            }

            var variables = new Dictionary<string, string>
            {
                ["UserName"] = user.UserName ?? "",
                ["DisplayName"] = user.DisplayName ?? user.UserName ?? "",
                ["Email"] = user.Email,
                ["Reason"] = reason,
                ["LockTime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["LockoutEnd"] = user.LockoutEnd?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown"
            };

            var subject = _emailTemplateService.ReplaceVariables(template.Subject, variables);
            var htmlBody = _emailTemplateService.ReplaceVariables(template.HtmlBody ?? "", variables);
            var textBody = _emailTemplateService.ReplaceVariables(template.TextBody ?? "", variables);

            await _emailService.SendEmailAsync(user.Email, subject, htmlBody, textBody);

            _logger.LogInformation("Account locked notification sent to {Email}", user.Email);

            // 触发Webhook事件
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webhookService.TriggerEventAsync(
                        WebhookEventTypes.AccountLocked,
                        new
                        {
                            userId = user.Id,
                            userName = user.UserName,
                            email = user.Email,
                            lockoutTime = DateTime.UtcNow,
                            reason = reason
                        },
                        user.TenantId);
                }
                catch (Exception webhookEx)
                {
                    _logger.LogError(webhookEx, "Failed to trigger account locked webhook for user {UserId}", user.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account locked notification to user {UserId}", user.Id);
        }
    }

    /// <summary>
    /// 发送 MFA 启用通知
    /// </summary>
    public async Task SendMfaEnabledNotificationAsync(AppUser user)
    {
        try
        {
            var notificationEnabled = await _systemSettings.GetBoolValueAsync("notification-mfa-enabled-enabled", true);
            if (!notificationEnabled || string.IsNullOrEmpty(user.Email))
            {
                return;
            }

            var template = await _emailTemplateService.GetTemplateByKeyAsync("mfa-enabled");
            if (template == null)
            {
                return;
            }

            var variables = new Dictionary<string, string>
            {
                ["UserName"] = user.UserName ?? "",
                ["DisplayName"] = user.DisplayName ?? user.UserName ?? "",
                ["Email"] = user.Email,
                ["EnableTime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            };

            var subject = _emailTemplateService.ReplaceVariables(template.Subject, variables);
            var htmlBody = _emailTemplateService.ReplaceVariables(template.HtmlBody ?? "", variables);
            var textBody = _emailTemplateService.ReplaceVariables(template.TextBody ?? "", variables);

            await _emailService.SendEmailAsync(user.Email, subject, htmlBody, textBody);

            _logger.LogInformation("MFA enabled notification sent to {Email}", user.Email);

            // 触发Webhook事件
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webhookService.TriggerEventAsync(
                        WebhookEventTypes.UserMfaEnabled,
                        new
                        {
                            userId = user.Id,
                            userName = user.UserName,
                            email = user.Email,
                            enableTime = DateTime.UtcNow
                        },
                        user.TenantId);
                }
                catch (Exception webhookEx)
                {
                    _logger.LogError(webhookEx, "Failed to trigger MFA enabled webhook for user {UserId}", user.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MFA enabled notification to user {UserId}", user.Id);
        }
    }

    /// <summary>
    /// 批量发送通知
    /// </summary>
    public async Task<int> SendBulkNotificationsAsync(List<NotificationRequest> notifications)
    {
        int successCount = 0;

        foreach (var notification in notifications)
        {
            try
            {
                var template = await _emailTemplateService.GetTemplateByKeyAsync(notification.TemplateKey);
                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateKey} not found", notification.TemplateKey);
                    continue;
                }

                var subject = _emailTemplateService.ReplaceVariables(
                    notification.Subject ?? template.Subject,
                    notification.Variables);
                var htmlBody = _emailTemplateService.ReplaceVariables(
                    template.HtmlBody ?? "",
                    notification.Variables);
                var textBody = _emailTemplateService.ReplaceVariables(
                    template.TextBody ?? "",
                    notification.Variables);

                await _emailService.SendEmailAsync(notification.Email, subject, htmlBody, textBody);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to {Email}", notification.Email);
            }
        }

        _logger.LogInformation("Sent {SuccessCount}/{TotalCount} bulk notifications", successCount, notifications.Count);
        return successCount;
    }

    #region Private Helper Methods

    private string GetRiskLevel(int riskScore)
    {
        return riskScore switch
        {
            >= 70 => "High (高风险)",
            >= 40 => "Medium (中风险)",
            _ => "Low (低风险)"
        };
    }

    private async Task SendDefaultAnomalousLoginEmailAsync(
        AppUser user,
        LoginHistory loginHistory,
        List<string> anomalyReasons,
        int riskScore)
    {
        var subject = "⚠️ 异常登录检测 - Anomalous Login Detected";
        var htmlBody = $@"
<h2>异常登录检测 / Anomalous Login Detected</h2>
<p>尊敬的 {user.DisplayName ?? user.UserName}，</p>
<p>我们检测到您的账户有一次异常登录尝试：</p>
<ul>
    <li><strong>登录时间</strong>: {loginHistory.LoginTime:yyyy-MM-dd HH:mm:ss UTC}</li>
    <li><strong>IP地址</strong>: {loginHistory.IpAddress}</li>
    <li><strong>位置</strong>: {loginHistory.Country}, {loginHistory.City}</li>
    <li><strong>浏览器</strong>: {loginHistory.Browser}</li>
    <li><strong>操作系统</strong>: {loginHistory.OperatingSystem}</li>
    <li><strong>风险评分</strong>: {riskScore}/100 ({GetRiskLevel(riskScore)})</li>
</ul>
<p><strong>异常原因</strong>:</p>
<ul>
{string.Join("", anomalyReasons.Select(r => $"<li>{r}</li>"))}
</ul>
<p>如果这是您本人的操作，请忽略此邮件。如果不是，请立即修改密码并联系管理员。</p>
";

        await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
    }

    private async Task SendDefaultNewDeviceEmailAsync(
        AppUser user,
        UserDevice device,
        string ipAddress,
        string location)
    {
        var subject = "🔐 新设备登录通知 - New Device Login";
        var htmlBody = $@"
<h2>新设备登录通知 / New Device Login</h2>
<p>尊敬的 {user.DisplayName ?? user.UserName}，</p>
<p>我们检测到您的账户从一个新设备登录：</p>
<ul>
    <li><strong>设备名称</strong>: {device.DeviceName}</li>
    <li><strong>浏览器</strong>: {device.Browser} {device.BrowserVersion}</li>
    <li><strong>操作系统</strong>: {device.OperatingSystem} {device.OsVersion}</li>
    <li><strong>设备类型</strong>: {device.DeviceType}</li>
    <li><strong>IP地址</strong>: {ipAddress}</li>
    <li><strong>位置</strong>: {location}</li>
    <li><strong>登录时间</strong>: {device.FirstUsedAt:yyyy-MM-dd HH:mm:ss UTC}</li>
</ul>
<p>如果这是您本人的操作，请忽略此邮件。如果不是，请立即修改密码并联系管理员。</p>
";

        await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
    }

    #endregion
}

