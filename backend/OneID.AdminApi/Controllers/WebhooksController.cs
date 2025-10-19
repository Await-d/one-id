using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// Webhook管理控制器
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有Webhook配置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<WebhookDto>>> GetAllWebhooks()
    {
        var webhooks = await _webhookService.GetAllWebhooksAsync();
        var dtos = webhooks.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// 获取Webhook详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WebhookDto>> GetWebhook(Guid id)
    {
        var webhook = await _webhookService.GetWebhookAsync(id);
        if (webhook == null)
        {
            return NotFound();
        }
        return Ok(MapToDto(webhook));
    }

    /// <summary>
    /// 创建Webhook
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WebhookDto>> CreateWebhook([FromBody] CreateWebhookDto dto)
    {
        var webhook = new Webhook
        {
            Name = dto.Name,
            Url = dto.Url,
            Description = dto.Description,
            Events = string.Join(",", dto.Events),
            Secret = dto.Secret,
            IsActive = dto.IsActive,
            MaxRetries = dto.MaxRetries,
            TimeoutSeconds = dto.TimeoutSeconds,
            CustomHeaders = dto.CustomHeaders,
            TenantId = dto.TenantId
        };

        var created = await _webhookService.CreateWebhookAsync(webhook);
        return CreatedAtAction(nameof(GetWebhook), new { id = created.Id }, MapToDto(created));
    }

    /// <summary>
    /// 更新Webhook
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WebhookDto>> UpdateWebhook(Guid id, [FromBody] UpdateWebhookDto dto)
    {
        var webhook = await _webhookService.GetWebhookAsync(id);
        if (webhook == null)
        {
            return NotFound();
        }

        webhook.Name = dto.Name;
        webhook.Url = dto.Url;
        webhook.Description = dto.Description;
        webhook.Events = string.Join(",", dto.Events);
        webhook.Secret = dto.Secret;
        webhook.IsActive = dto.IsActive;
        webhook.MaxRetries = dto.MaxRetries;
        webhook.TimeoutSeconds = dto.TimeoutSeconds;
        webhook.CustomHeaders = dto.CustomHeaders;

        var updated = await _webhookService.UpdateWebhookAsync(webhook);
        return Ok(MapToDto(updated));
    }

    /// <summary>
    /// 删除Webhook
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteWebhook(Guid id)
    {
        await _webhookService.DeleteWebhookAsync(id);
        return NoContent();
    }

    /// <summary>
    /// 测试Webhook
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<ActionResult<WebhookTestResult>> TestWebhook(Guid id)
    {
        try
        {
            var result = await _webhookService.TestWebhookAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook {WebhookId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取Webhook日志
    /// </summary>
    [HttpGet("{id}/logs")]
    public async Task<ActionResult<List<WebhookLogDto>>> GetWebhookLogs(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var logs = await _webhookService.GetWebhookLogsAsync(id, pageNumber, pageSize);
        var dtos = logs.Select(MapLogToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// 重新发送失败的Webhook
    /// </summary>
    [HttpPost("logs/{logId}/retry")]
    public async Task<ActionResult> RetryWebhookLog(Guid logId)
    {
        try
        {
            await _webhookService.RetryWebhookLogAsync(logId);
            return Ok(new { message = "Webhook retry initiated" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying webhook log {LogId}", logId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有可用的事件类型
    /// </summary>
    [HttpGet("event-types")]
    public ActionResult<List<EventTypeDto>> GetEventTypes()
    {
        var eventTypes = WebhookEventTypes.GetAll()
            .Select(et => new EventTypeDto
            {
                Value = et,
                Label = WebhookEventTypes.GetDisplayName(et)
            })
            .ToList();

        return Ok(eventTypes);
    }

    private WebhookDto MapToDto(Webhook webhook)
    {
        return new WebhookDto
        {
            Id = webhook.Id,
            Name = webhook.Name,
            Url = webhook.Url,
            Description = webhook.Description,
            Events = webhook.Events.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Secret = webhook.Secret,
            IsActive = webhook.IsActive,
            MaxRetries = webhook.MaxRetries,
            TimeoutSeconds = webhook.TimeoutSeconds,
            CustomHeaders = webhook.CustomHeaders,
            LastTriggeredAt = webhook.LastTriggeredAt,
            LastSuccessAt = webhook.LastSuccessAt,
            LastFailureAt = webhook.LastFailureAt,
            FailureCount = webhook.FailureCount,
            TotalTriggers = webhook.TotalTriggers,
            SuccessCount = webhook.SuccessCount,
            CreatedAt = webhook.CreatedAt,
            UpdatedAt = webhook.UpdatedAt,
            TenantId = webhook.TenantId
        };
    }

    private WebhookLogDto MapLogToDto(WebhookLog log)
    {
        return new WebhookLogDto
        {
            Id = log.Id,
            WebhookId = log.WebhookId,
            EventType = log.EventType,
            Payload = log.Payload,
            Url = log.Url,
            StatusCode = log.StatusCode,
            Response = log.Response,
            Success = log.Success,
            ErrorMessage = log.ErrorMessage,
            RetryCount = log.RetryCount,
            DurationMs = log.DurationMs,
            CreatedAt = log.CreatedAt
        };
    }
}

/// <summary>
/// Webhook DTO
/// </summary>
public class WebhookDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Events { get; set; } = new();
    public string? Secret { get; set; }
    public bool IsActive { get; set; }
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
    public string? CustomHeaders { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public int FailureCount { get; set; }
    public int TotalTriggers { get; set; }
    public int SuccessCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? TenantId { get; set; }
}

/// <summary>
/// 创建Webhook DTO
/// </summary>
public class CreateWebhookDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Events { get; set; } = new();
    public string? Secret { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public string? CustomHeaders { get; set; }
    public Guid? TenantId { get; set; }
}

/// <summary>
/// 更新Webhook DTO
/// </summary>
public class UpdateWebhookDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Events { get; set; } = new();
    public string? Secret { get; set; }
    public bool IsActive { get; set; }
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
    public string? CustomHeaders { get; set; }
}

/// <summary>
/// Webhook日志 DTO
/// </summary>
public class WebhookLogDto
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 事件类型 DTO
/// </summary>
public class EventTypeDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

