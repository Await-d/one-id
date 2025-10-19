using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 安全规则管理（IP 黑白名单等）
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class SecurityRulesController(
    ISecurityRuleService securityRuleService,
    ILogger<SecurityRulesController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有安全规则
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SecurityRuleDto>>> GetAll(
        [FromQuery] bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        var rules = await securityRuleService.GetAllRulesAsync(includeDisabled, cancellationToken);
        var dtos = rules.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// 根据 ID 获取安全规则
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SecurityRuleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await securityRuleService.GetRuleByIdAsync(id, cancellationToken);
        if (rule == null)
        {
            return NotFound(new { Message = $"Security rule {id} not found" });
        }

        return Ok(MapToDto(rule));
    }

    /// <summary>
    /// 创建新的安全规则
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SecurityRuleDto>> Create(
        [FromBody] CreateSecurityRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await securityRuleService.CreateRuleAsync(
                request.RuleType,
                request.RuleValue,
                request.Description,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = rule.Id }, MapToDto(rule));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create security rule");
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 更新安全规则
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SecurityRuleDto>> Update(
        Guid id,
        [FromBody] UpdateSecurityRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await securityRuleService.UpdateRuleAsync(
                id,
                request.RuleValue,
                request.Description,
                cancellationToken);

            return Ok(MapToDto(rule));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 切换安全规则启用状态
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    public async Task<ActionResult<SecurityRuleDto>> Toggle(
        Guid id,
        [FromBody] ToggleRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await securityRuleService.ToggleRuleAsync(id, request.IsEnabled, cancellationToken);
            return Ok(MapToDto(rule));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 删除安全规则
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await securityRuleService.DeleteRuleAsync(id, cancellationToken);
            return Ok(new { Message = "Security rule deleted successfully", RuleId = id });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 测试 IP 地址是否被允许
    /// </summary>
    [HttpPost("test-ip")]
    public async Task<ActionResult<IpTestResult>> TestIp(
        [FromBody] TestIpRequest request,
        CancellationToken cancellationToken)
    {
        var isAllowed = await securityRuleService.IsIpAllowedAsync(request.IpAddress, cancellationToken);

        return Ok(new IpTestResult
        {
            IpAddress = request.IpAddress,
            IsAllowed = isAllowed,
            TestedAt = DateTime.UtcNow
        });
    }

    private static SecurityRuleDto MapToDto(SecurityRule rule)
    {
        return new SecurityRuleDto
        {
            Id = rule.Id,
            RuleType = rule.RuleType,
            RuleValue = rule.RuleValue,
            Description = rule.Description,
            IsEnabled = rule.IsEnabled,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt ?? rule.CreatedAt,
            TenantId = rule.TenantId
        };
    }
}

// DTOs
public record CreateSecurityRuleRequest(
    string RuleType,
    string RuleValue,
    string? Description = null);

public record UpdateSecurityRuleRequest(
    string RuleValue,
    string? Description = null);

public record ToggleRuleRequest(bool IsEnabled);

public record TestIpRequest(string IpAddress);

public record SecurityRuleDto
{
    public Guid Id { get; init; }
    public string RuleType { get; init; } = string.Empty;
    public string RuleValue { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid? TenantId { get; init; }
}

public record IpTestResult
{
    public string IpAddress { get; init; } = string.Empty;
    public bool IsAllowed { get; init; }
    public DateTime TestedAt { get; init; }
}

