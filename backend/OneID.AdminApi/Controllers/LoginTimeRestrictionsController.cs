using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class LoginTimeRestrictionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LoginTimeRestrictionsController> _logger;

    public LoginTimeRestrictionsController(AppDbContext dbContext, ILogger<LoginTimeRestrictionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有登录时间限制
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<LoginTimeRestriction>>> GetAll()
    {
        var restrictions = await _dbContext.LoginTimeRestrictions
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        return Ok(restrictions);
    }

    /// <summary>
    /// 根据ID获取登录时间限制
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LoginTimeRestriction>> GetById(int id)
    {
        var restriction = await _dbContext.LoginTimeRestrictions.FindAsync(id);

        if (restriction == null)
        {
            return NotFound(new { message = $"Login time restriction with ID {id} not found" });
        }

        return Ok(restriction);
    }

    /// <summary>
    /// 创建新的登录时间限制
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LoginTimeRestriction>> Create([FromBody] CreateLoginTimeRestrictionRequest request)
    {
        var restriction = new LoginTimeRestriction
        {
            Name = request.Name,
            IsEnabled = request.IsEnabled,
            Scope = request.Scope,
            TargetUserId = request.TargetUserId,
            TargetRoleName = request.TargetRoleName,
            AllowedDaysOfWeek = request.AllowedDaysOfWeek,
            DailyStartTime = request.DailyStartTime,
            DailyEndTime = request.DailyEndTime,
            TimeZone = request.TimeZone ?? "UTC",
            Description = request.Description,
            Priority = request.Priority,
            CreatedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.LoginTimeRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Login time restriction created: {RestrictionName} by {User}", 
            restriction.Name, User.Identity?.Name);

        return CreatedAtAction(nameof(GetById), new { id = restriction.Id }, restriction);
    }

    /// <summary>
    /// 更新登录时间限制
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<LoginTimeRestriction>> Update(int id, [FromBody] UpdateLoginTimeRestrictionRequest request)
    {
        var restriction = await _dbContext.LoginTimeRestrictions.FindAsync(id);

        if (restriction == null)
        {
            return NotFound(new { message = $"Login time restriction with ID {id} not found" });
        }

        restriction.Name = request.Name;
        restriction.IsEnabled = request.IsEnabled;
        restriction.Scope = request.Scope;
        restriction.TargetUserId = request.TargetUserId;
        restriction.TargetRoleName = request.TargetRoleName;
        restriction.AllowedDaysOfWeek = request.AllowedDaysOfWeek;
        restriction.DailyStartTime = request.DailyStartTime;
        restriction.DailyEndTime = request.DailyEndTime;
        restriction.TimeZone = request.TimeZone ?? "UTC";
        restriction.Description = request.Description;
        restriction.Priority = request.Priority;
        restriction.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Login time restriction updated: {RestrictionName} by {User}", 
            restriction.Name, User.Identity?.Name);

        return Ok(restriction);
    }

    /// <summary>
    /// 删除登录时间限制
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var restriction = await _dbContext.LoginTimeRestrictions.FindAsync(id);

        if (restriction == null)
        {
            return NotFound(new { message = $"Login time restriction with ID {id} not found" });
        }

        _dbContext.LoginTimeRestrictions.Remove(restriction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Login time restriction deleted: {RestrictionName} by {User}", 
            restriction.Name, User.Identity?.Name);

        return NoContent();
    }

    /// <summary>
    /// 切换规则启用状态
    /// </summary>
    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<LoginTimeRestriction>> ToggleEnabled(int id)
    {
        var restriction = await _dbContext.LoginTimeRestrictions.FindAsync(id);

        if (restriction == null)
        {
            return NotFound(new { message = $"Login time restriction with ID {id} not found" });
        }

        restriction.IsEnabled = !restriction.IsEnabled;
        restriction.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Login time restriction {RestrictionName} {Status} by {User}", 
            restriction.Name, restriction.IsEnabled ? "enabled" : "disabled", User.Identity?.Name);

        return Ok(restriction);
    }
}

#region Request DTOs

public class CreateLoginTimeRestrictionRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public AccessRuleScope Scope { get; set; } = AccessRuleScope.Global;
    public Guid? TargetUserId { get; set; }
    public string? TargetRoleName { get; set; }
    public string? AllowedDaysOfWeek { get; set; }
    public string? DailyStartTime { get; set; }
    public string? DailyEndTime { get; set; }
    public string? TimeZone { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; } = 100;
}

public class UpdateLoginTimeRestrictionRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public AccessRuleScope Scope { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetRoleName { get; set; }
    public string? AllowedDaysOfWeek { get; set; }
    public string? DailyStartTime { get; set; }
    public string? DailyEndTime { get; set; }
    public string? TimeZone { get; set; }
    public string? Description { get; set; }
    public int Priority { get; set; }
}

#endregion

