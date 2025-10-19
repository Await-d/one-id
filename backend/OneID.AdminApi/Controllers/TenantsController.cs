using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;
using System.Security.Claims;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ITenantService tenantService,
        IAuditLogService auditLogService,
        ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有租户列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetAllTenants()
    {
        try
        {
            var tenants = await _tenantService.GetAllTenantsAsync(includeInactive: true);
            return Ok(tenants.Select(t => new TenantDto
            {
                Id = t.Id.ToString(),
                Name = t.Name,
                DisplayName = t.DisplayName,
                Domain = t.Domain ?? string.Empty,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenants");
            return StatusCode(500, "Failed to get tenants");
        }
    }

    /// <summary>
    /// 根据ID获取租户
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDto>> GetTenantById(Guid id)
    {
        try
        {
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(new TenantDto
            {
                Id = tenant.Id.ToString(),
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Domain = tenant.Domain ?? string.Empty,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant {TenantId}", id);
            return StatusCode(500, "Failed to get tenant");
        }
    }

    /// <summary>
    /// 创建新租户
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            var tenant = await _tenantService.CreateTenantAsync(
                request.Name,
                request.DisplayName,
                request.Domain);

            await _auditLogService.LogAsync(
                action: $"Created tenant: {tenant.Name}",
                category: "Tenant",
                success: true);

            return CreatedAtAction(
                nameof(GetTenantById),
                new { id = tenant.Id },
                new TenantDto
                {
                    Id = tenant.Id.ToString(),
                    Name = tenant.Name,
                    DisplayName = tenant.DisplayName,
                    Domain = tenant.Domain ?? string.Empty,
                    IsActive = tenant.IsActive,
                    CreatedAt = tenant.CreatedAt,
                    UpdatedAt = tenant.UpdatedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant");
            await _auditLogService.LogAsync(
                action: $"Failed to create tenant: {request.Name}",
                category: "Tenant",
                success: false);
            return StatusCode(500, "Failed to create tenant");
        }
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TenantDto>> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        try
        {
            // 先检查是否需要切换状态
            var existingTenant = await _tenantService.GetTenantByIdAsync(id);
            if (existingTenant == null)
            {
                return NotFound();
            }

            // 更新基本信息
            var tenant = await _tenantService.UpdateTenantAsync(
                id,
                request.DisplayName,
                request.Domain);

            // 如果状态不同，切换状态
            if (tenant.IsActive != request.IsActive)
            {
                tenant = await _tenantService.ToggleTenantAsync(id, request.IsActive);
            }

            await _auditLogService.LogAsync(
                action: $"Updated tenant: {tenant.Name}",
                category: "Tenant",
                success: true);

            return Ok(new TenantDto
            {
                Id = tenant.Id.ToString(),
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Domain = tenant.Domain ?? string.Empty,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tenant {TenantId}", id);
            await _auditLogService.LogAsync(
                action: $"Failed to update tenant: {id}",
                category: "Tenant",
                success: false);
            return StatusCode(500, "Failed to update tenant");
        }
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTenant(Guid id)
    {
        try
        {
            await _tenantService.DeleteTenantAsync(id);

            await _auditLogService.LogAsync(
                action: $"Deleted tenant: {id}",
                category: "Tenant",
                success: true);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete tenant {TenantId}", id);
            await _auditLogService.LogAsync(
                action: $"Failed to delete tenant: {id}",
                category: "Tenant",
                success: false);
            return StatusCode(500, "Failed to delete tenant");
        }
    }

    /// <summary>
    /// 启用/禁用租户
    /// </summary>
    [HttpPost("{id}/toggle")]
    public async Task<ActionResult> ToggleTenantStatus(Guid id)
    {
        try
        {
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            tenant = await _tenantService.ToggleTenantAsync(id, !tenant.IsActive);

            await _auditLogService.LogAsync(
                action: $"Toggled tenant status: {tenant.Name} (IsActive: {tenant.IsActive})",
                category: "Tenant",
                success: true);

            return Ok(new { isActive = tenant.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle tenant status {TenantId}", id);
            return StatusCode(500, "Failed to toggle tenant status");
        }
    }
}

public record TenantDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
}

public record UpdateTenantRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
