using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IpAccessRulesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<IpAccessRulesController> _logger;

    public IpAccessRulesController(AppDbContext dbContext, ILogger<IpAccessRulesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有IP访问规则
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<IpAccessRule>>> GetAll()
    {
        var rules = await _dbContext.IpAccessRules
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        return Ok(rules);
    }

    /// <summary>
    /// 根据ID获取IP访问规则
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<IpAccessRule>> GetById(int id)
    {
        var rule = await _dbContext.IpAccessRules.FindAsync(id);

        if (rule == null)
        {
            return NotFound(new { message = $"IP access rule with ID {id} not found" });
        }

        return Ok(rule);
    }

    /// <summary>
    /// 创建新的IP访问规则
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IpAccessRule>> Create([FromBody] CreateIpAccessRuleRequest request)
    {
        var rule = new IpAccessRule
        {
            Name = request.Name,
            IpAddress = request.IpAddress,
            RuleType = request.RuleType,
            IsEnabled = request.IsEnabled,
            Scope = request.Scope,
            TargetUserId = request.TargetUserId,
            TargetRoleName = request.TargetRoleName,
            Description = request.Description,
            Priority = request.Priority,
            CreatedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IpAccessRules.Add(rule);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("IP access rule created: {RuleName} by {User}", rule.Name, User.Identity?.Name);

        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule);
    }

    /// <summary>
    /// 更新IP访问规则
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<IpAccessRule>> Update(int id, [FromBody] UpdateIpAccessRuleRequest request)
    {
        var rule = await _dbContext.IpAccessRules.FindAsync(id);

        if (rule == null)
        {
            return NotFound(new { message = $"IP access rule with ID {id} not found" });
        }

        rule.Name = request.Name;
        rule.IpAddress = request.IpAddress;
        rule.RuleType = request.RuleType;
        rule.IsEnabled = request.IsEnabled;
        rule.Scope = request.Scope;
        rule.TargetUserId = request.TargetUserId;
        rule.TargetRoleName = request.TargetRoleName;
        rule.Description = request.Description;
        rule.Priority = request.Priority;
        rule.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("IP access rule updated: {RuleName} by {User}", rule.Name, User.Identity?.Name);

        return Ok(rule);
    }

    /// <summary>
    /// 删除IP访问规则
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _dbContext.IpAccessRules.FindAsync(id);

        if (rule == null)
        {
            return NotFound(new { message = $"IP access rule with ID {id} not found" });
        }

        _dbContext.IpAccessRules.Remove(rule);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("IP access rule deleted: {RuleName} by {User}", rule.Name, User.Identity?.Name);

        return NoContent();
    }

    /// <summary>
    /// 切换规则启用状态
    /// </summary>
    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<IpAccessRule>> ToggleEnabled(int id)
    {
        var rule = await _dbContext.IpAccessRules.FindAsync(id);

        if (rule == null)
        {
            return NotFound(new { message = $"IP access rule with ID {id} not found" });
        }

        rule.IsEnabled = !rule.IsEnabled;
        rule.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("IP access rule {RuleName} {Status} by {User}", 
            rule.Name, rule.IsEnabled ? "enabled" : "disabled", User.Identity?.Name);

        return Ok(rule);
    }
}

#region Request DTOs

public class CreateIpAccessRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public IpAccessRuleType RuleType { get; set; } = IpAccessRuleType.Whitelist;
    public bool IsEnabled { get; set; } = true;
    public AccessRuleScope Scope { get; set; } = AccessRuleScope.Global;
    public Guid? TargetUserId { get; set; }
    public string? TargetRoleName { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; } = 100;
}

public class UpdateIpAccessRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public IpAccessRuleType RuleType { get; set; }
    public bool IsEnabled { get; set; }
    public AccessRuleScope Scope { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetRoleName { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; }
}

#endregion

