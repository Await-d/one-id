using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// Webhook服务实现
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 触发Webhook事件
    /// </summary>
    public async Task TriggerEventAsync(string eventType, object payload, Guid? tenantId = null)
    {
        try
        {
            // 查找订阅此事件的所有活跃webhook
            var webhooks = await _context.Webhooks
                .Where(w => w.IsActive && w.Events.Contains(eventType))
                .Where(w => tenantId == null || w.TenantId == tenantId)
                .ToListAsync();

            if (!webhooks.Any())
            {
                _logger.LogDebug("No active webhooks found for event type: {EventType}", eventType);
                return;
            }

            var payloadJson = JsonSerializer.Serialize(payload);

            // 异步触发所有webhook（不等待完成）
            foreach (var webhook in webhooks)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendWebhookAsync(webhook, eventType, payloadJson, tenantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send webhook {WebhookId} for event {EventType}", 
                            webhook.Id, eventType);
                    }
                });
            }

            _logger.LogInformation("Triggered {Count} webhooks for event type: {EventType}", 
                webhooks.Count, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger webhooks for event type: {EventType}", eventType);
        }
    }

    /// <summary>
    /// 发送Webhook请求（带重试）
    /// </summary>
    private async Task SendWebhookAsync(Webhook webhook, string eventType, string payloadJson, Guid? tenantId)
    {
        var log = new WebhookLog
        {
            Id = Guid.NewGuid(),
            WebhookId = webhook.Id,
            EventType = eventType,
            Payload = payloadJson,
            Url = webhook.Url,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        for (int attempt = 0; attempt <= webhook.MaxRetries; attempt++)
        {
            try
            {
                log.RetryCount = attempt;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(webhook.TimeoutSeconds);

                var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };

                // 添加签名头
                if (!string.IsNullOrEmpty(webhook.Secret))
                {
                    var signature = GenerateSignature(payloadJson, webhook.Secret);
                    request.Headers.Add("X-Webhook-Signature", signature);
                }

                // 添加事件类型头
                request.Headers.Add("X-Webhook-Event", eventType);

                // 添加自定义头
                if (!string.IsNullOrEmpty(webhook.CustomHeaders))
                {
                    try
                    {
                        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(webhook.CustomHeaders);
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse custom headers for webhook {WebhookId}", webhook.Id);
                    }
                }

                var response = await httpClient.SendAsync(request);

                stopwatch.Stop();
                log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
                log.StatusCode = (int)response.StatusCode;
                log.Response = await response.Content.ReadAsStringAsync();
                log.Response = log.Response?.Length > 4000 ? log.Response.Substring(0, 4000) : log.Response;

                if (response.IsSuccessStatusCode)
                {
                    log.Success = true;
                    webhook.LastSuccessAt = DateTime.UtcNow;
                    webhook.SuccessCount++;
                    webhook.FailureCount = 0; // 重置失败计数
                    _logger.LogInformation(
                        "Webhook {WebhookId} sent successfully for event {EventType}. Status: {StatusCode}, Duration: {Duration}ms",
                        webhook.Id, eventType, log.StatusCode, log.DurationMs);
                    break; // 成功，退出重试循环
                }
                else
                {
                    log.Success = false;
                    log.ErrorMessage = $"HTTP {log.StatusCode}: {log.Response}";
                    webhook.LastFailureAt = DateTime.UtcNow;
                    webhook.FailureCount++;

                    if (attempt < webhook.MaxRetries)
                    {
                        var delay = CalculateRetryDelay(attempt);
                        _logger.LogWarning(
                            "Webhook {WebhookId} failed with status {StatusCode}. Retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                            webhook.Id, log.StatusCode, delay, attempt + 1, webhook.MaxRetries);
                        await Task.Delay(delay);
                    }
                    else
                    {
                        _logger.LogError(
                            "Webhook {WebhookId} failed after {MaxRetries} retries. Status: {StatusCode}",
                            webhook.Id, webhook.MaxRetries, log.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
                log.Success = false;
                log.ErrorMessage = ex.Message.Length > 2000 ? ex.Message.Substring(0, 2000) : ex.Message;
                webhook.LastFailureAt = DateTime.UtcNow;
                webhook.FailureCount++;

                if (attempt < webhook.MaxRetries)
                {
                    var delay = CalculateRetryDelay(attempt);
                    _logger.LogWarning(ex,
                        "Webhook {WebhookId} exception. Retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                        webhook.Id, delay, attempt + 1, webhook.MaxRetries);
                    await Task.Delay(delay);
                }
                else
                {
                    _logger.LogError(ex,
                        "Webhook {WebhookId} failed after {MaxRetries} retries",
                        webhook.Id, webhook.MaxRetries);
                }
            }
        }

        // 更新统计
        webhook.LastTriggeredAt = DateTime.UtcNow;
        webhook.TotalTriggers++;
        _context.Webhooks.Update(webhook);

        // 保存日志
        _context.WebhookLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 计算重试延迟（指数退避）
    /// </summary>
    private int CalculateRetryDelay(int attempt)
    {
        return (int)Math.Pow(2, attempt) * 1000; // 1s, 2s, 4s, 8s...
    }

    /// <summary>
    /// 生成HMAC-SHA256签名
    /// </summary>
    private string GenerateSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 获取所有Webhook配置
    /// </summary>
    public async Task<List<Webhook>> GetAllWebhooksAsync(Guid? tenantId = null)
    {
        var query = _context.Webhooks.AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(w => w.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取Webhook详情
    /// </summary>
    public async Task<Webhook?> GetWebhookAsync(Guid id)
    {
        return await _context.Webhooks.FindAsync(id);
    }

    /// <summary>
    /// 创建Webhook
    /// </summary>
    public async Task<Webhook> CreateWebhookAsync(Webhook webhook)
    {
        webhook.Id = Guid.NewGuid();
        webhook.CreatedAt = DateTime.UtcNow;
        webhook.UpdatedAt = DateTime.UtcNow;

        _context.Webhooks.Add(webhook);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook {WebhookId} created: {Name}", webhook.Id, webhook.Name);
        return webhook;
    }

    /// <summary>
    /// 更新Webhook
    /// </summary>
    public async Task<Webhook> UpdateWebhookAsync(Webhook webhook)
    {
        webhook.UpdatedAt = DateTime.UtcNow;

        _context.Webhooks.Update(webhook);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook {WebhookId} updated: {Name}", webhook.Id, webhook.Name);
        return webhook;
    }

    /// <summary>
    /// 删除Webhook
    /// </summary>
    public async Task DeleteWebhookAsync(Guid id)
    {
        var webhook = await _context.Webhooks.FindAsync(id);
        if (webhook != null)
        {
            _context.Webhooks.Remove(webhook);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook {WebhookId} deleted: {Name}", webhook.Id, webhook.Name);
        }
    }

    /// <summary>
    /// 测试Webhook
    /// </summary>
    public async Task<WebhookTestResult> TestWebhookAsync(Guid id)
    {
        var webhook = await GetWebhookAsync(id);
        if (webhook == null)
        {
            throw new KeyNotFoundException($"Webhook {id} not found");
        }

        var testPayload = new
        {
            test = true,
            message = "This is a test webhook",
            timestamp = DateTime.UtcNow
        };

        var result = new WebhookTestResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var payloadJson = JsonSerializer.Serialize(testPayload);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(webhook.TimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(webhook.Secret))
            {
                var signature = GenerateSignature(payloadJson, webhook.Secret);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            request.Headers.Add("X-Webhook-Event", "test");

            var response = await httpClient.SendAsync(request);

            stopwatch.Stop();

            result.StatusCode = (int)response.StatusCode;
            result.Response = await response.Content.ReadAsStringAsync();
            result.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            result.Success = response.IsSuccessStatusCode;

            if (!result.Success)
            {
                result.ErrorMessage = $"HTTP {result.StatusCode}: {result.Response}";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 获取Webhook日志
    /// </summary>
    public async Task<List<WebhookLog>> GetWebhookLogsAsync(Guid webhookId, int pageNumber = 1, int pageSize = 50)
    {
        return await _context.WebhookLogs
            .Where(l => l.WebhookId == webhookId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// 重新发送失败的Webhook
    /// </summary>
    public async Task RetryWebhookLogAsync(Guid logId)
    {
        var log = await _context.WebhookLogs
            .Include(l => l.Webhook)
            .FirstOrDefaultAsync(l => l.Id == logId);

        if (log == null || log.Webhook == null)
        {
            throw new KeyNotFoundException($"Webhook log {logId} not found");
        }

        await SendWebhookAsync(log.Webhook, log.EventType, log.Payload, log.TenantId);
    }
}

