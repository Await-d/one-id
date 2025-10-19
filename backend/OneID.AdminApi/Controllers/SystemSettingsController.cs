using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;
using System.Security.Claims;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 系统设置管理
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class SystemSettingsController(
    ISystemSettingsService systemSettingsService,
    ILogger<SystemSettingsController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有系统设置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SystemSettingDto>>> GetAll(
        [FromQuery] string? group = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await systemSettingsService.GetAllSettingsAsync(group, cancellationToken);
            var dtos = settings.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get system settings");
            return StatusCode(500, new { Message = "Failed to get system settings" });
        }
    }

    /// <summary>
    /// 根据 ID 获取系统设置
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SystemSettingDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var setting = await systemSettingsService.GetSettingByIdAsync(id, cancellationToken);
            if (setting == null)
            {
                return NotFound(new { Message = $"System setting {id} not found" });
            }

            return Ok(MapToDto(setting));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get system setting {Id}", id);
            return StatusCode(500, new { Message = "Failed to get system setting" });
        }
    }

    /// <summary>
    /// 根据键获取系统设置
    /// </summary>
    [HttpGet("key/{key}")]
    public async Task<ActionResult<SystemSettingDto>> GetByKey(string key, CancellationToken cancellationToken)
    {
        try
        {
            var setting = await systemSettingsService.GetSettingByKeyAsync(key, cancellationToken);
            if (setting == null)
            {
                return NotFound(new { Message = $"System setting '{key}' not found" });
            }

            return Ok(MapToDto(setting));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get system setting {Key}", key);
            return StatusCode(500, new { Message = "Failed to get system setting" });
        }
    }

    /// <summary>
    /// 获取分组设置（键值对）
    /// </summary>
    [HttpGet("group/{group}")]
    public async Task<ActionResult<Dictionary<string, string>>> GetByGroup(
        string group,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await systemSettingsService.GetSettingsByGroupAsync(group, cancellationToken);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get system settings for group {Group}", group);
            return StatusCode(500, new { Message = "Failed to get system settings" });
        }
    }

    /// <summary>
    /// 创建系统设置
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SystemSettingDto>> Create(
        [FromBody] CreateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var setting = new SystemSetting
            {
                Key = request.Key,
                Value = request.Value,
                Group = request.Group,
                ValueType = request.ValueType,
                DisplayName = request.DisplayName,
                Description = request.Description,
                DefaultValue = request.DefaultValue,
                IsSensitive = request.IsSensitive,
                IsReadOnly = request.IsReadOnly,
                SortOrder = request.SortOrder,
                ValidationRules = request.ValidationRules,
                AllowedValues = request.AllowedValues
            };

            var created = await systemSettingsService.CreateSettingAsync(setting, userId, cancellationToken);

            logger.LogInformation("System setting created: {Key} by {UserId}", created.Key, userId);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create system setting");
            return BadRequest(new { Message = $"Failed to create system setting: {ex.Message}" });
        }
    }

    /// <summary>
    /// 更新系统设置
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SystemSettingDto>> Update(
        int id,
        [FromBody] UpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await systemSettingsService.GetSettingByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return NotFound(new { Message = $"System setting {id} not found" });
            }

            existing.Value = request.Value;
            existing.DisplayName = request.DisplayName;
            existing.Description = request.Description;

            var updated = await systemSettingsService.UpdateSettingAsync(existing, userId, cancellationToken);

            logger.LogInformation("System setting updated: {Key} by {UserId}", updated.Key, userId);

            return Ok(MapToDto(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update system setting {Id}", id);
            return StatusCode(500, new { Message = "Failed to update system setting" });
        }
    }

    /// <summary>
    /// 更新设置值（简化接口）
    /// </summary>
    [HttpPatch("{key}/value")]
    public async Task<IActionResult> UpdateValue(
        string key,
        [FromBody] UpdateValueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await systemSettingsService.SetValueAsync(key, request.Value, userId, cancellationToken);

            logger.LogInformation("System setting value updated: {Key} by {UserId}", key, userId);

            return Ok(new { Message = "Setting value updated successfully", Key = key, Value = request.Value });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update system setting value {Key}", key);
            return StatusCode(500, new { Message = "Failed to update setting value" });
        }
    }

    /// <summary>
    /// 删除系统设置
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await systemSettingsService.DeleteSettingAsync(id, cancellationToken);

            logger.LogInformation("System setting deleted: {Id}", id);

            return Ok(new { Message = "System setting deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete system setting {Id}", id);
            return StatusCode(500, new { Message = "Failed to delete system setting" });
        }
    }

    /// <summary>
    /// 重置设置为默认值
    /// </summary>
    [HttpPost("{key}/reset")]
    public async Task<IActionResult> Reset(string key, CancellationToken cancellationToken)
    {
        try
        {
            await systemSettingsService.ResetToDefaultAsync(key, cancellationToken);

            logger.LogInformation("System setting reset to default: {Key}", key);

            return Ok(new { Message = "Setting reset to default value successfully", Key = key });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset system setting {Key}", key);
            return StatusCode(500, new { Message = "Failed to reset setting" });
        }
    }

    /// <summary>
    /// 重置分组所有设置为默认值
    /// </summary>
    [HttpPost("group/{group}/reset")]
    public async Task<IActionResult> ResetGroup(string group, CancellationToken cancellationToken)
    {
        try
        {
            await systemSettingsService.ResetGroupToDefaultAsync(group, cancellationToken);

            logger.LogInformation("System settings group reset to default: {Group}", group);

            return Ok(new { Message = "Settings group reset to default values successfully", Group = group });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset system settings group {Group}", group);
            return StatusCode(500, new { Message = "Failed to reset settings group" });
        }
    }

    /// <summary>
    /// 确保默认设置已初始化
    /// </summary>
    [HttpPost("ensure-defaults")]
    public async Task<IActionResult> EnsureDefaults(CancellationToken cancellationToken)
    {
        try
        {
            await systemSettingsService.EnsureDefaultSettingsAsync(cancellationToken);

            logger.LogInformation("Default system settings ensured");

            return Ok(new { Message = "Default settings ensured successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure default system settings");
            return StatusCode(500, new { Message = "Failed to ensure default settings" });
        }
    }

    private static SystemSettingDto MapToDto(SystemSetting setting)
    {
        return new SystemSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            Group = setting.Group,
            ValueType = setting.ValueType,
            DisplayName = setting.DisplayName,
            Description = setting.Description,
            DefaultValue = setting.DefaultValue,
            IsSensitive = setting.IsSensitive,
            IsReadOnly = setting.IsReadOnly,
            SortOrder = setting.SortOrder,
            ValidationRules = setting.ValidationRules,
            AllowedValues = setting.AllowedValues,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt,
            LastModifiedBy = setting.LastModifiedBy
        };
    }
}

// DTOs

public class SystemSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsReadOnly { get; set; }
    public int SortOrder { get; set; }
    public string? ValidationRules { get; set; }
    public string? AllowedValues { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
}

public class CreateSystemSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ValueType { get; set; } = "String";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsReadOnly { get; set; }
    public int SortOrder { get; set; }
    public string? ValidationRules { get; set; }
    public string? AllowedValues { get; set; }
}

public class UpdateSystemSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}

public class UpdateValueRequest
{
    public string Value { get; set; } = string.Empty;
}

