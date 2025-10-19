using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// Webhook服务接口
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// 触发Webhook事件
    /// </summary>
    Task TriggerEventAsync(string eventType, object payload, Guid? tenantId = null);

    /// <summary>
    /// 获取所有Webhook配置
    /// </summary>
    Task<List<Webhook>> GetAllWebhooksAsync(Guid? tenantId = null);

    /// <summary>
    /// 获取Webhook详情
    /// </summary>
    Task<Webhook?> GetWebhookAsync(Guid id);

    /// <summary>
    /// 创建Webhook
    /// </summary>
    Task<Webhook> CreateWebhookAsync(Webhook webhook);

    /// <summary>
    /// 更新Webhook
    /// </summary>
    Task<Webhook> UpdateWebhookAsync(Webhook webhook);

    /// <summary>
    /// 删除Webhook
    /// </summary>
    Task DeleteWebhookAsync(Guid id);

    /// <summary>
    /// 测试Webhook
    /// </summary>
    Task<WebhookTestResult> TestWebhookAsync(Guid id);

    /// <summary>
    /// 获取Webhook日志
    /// </summary>
    Task<List<WebhookLog>> GetWebhookLogsAsync(Guid webhookId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 重新发送失败的Webhook
    /// </summary>
    Task RetryWebhookLogAsync(Guid logId);
}

/// <summary>
/// Webhook测试结果
/// </summary>
public class WebhookTestResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
}

