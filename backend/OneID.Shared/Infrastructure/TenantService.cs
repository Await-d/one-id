using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public interface ITenantService
{
    Task<IReadOnlyList<Tenant>> GetAllTenantsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Tenant?> GetTenantByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetTenantByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Tenant?> GetTenantByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<Tenant> CreateTenantAsync(string name, string displayName, string? domain = null, string? logoUrl = null, string? themeConfig = null, CancellationToken cancellationToken = default);
    Task<Tenant> UpdateTenantAsync(Guid id, string displayName, string? domain = null, string? logoUrl = null, string? themeConfig = null, CancellationToken cancellationToken = default);
    Task<Tenant> ToggleTenantAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteTenantAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class TenantService : ITenantService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TenantService> _logger;

    public TenantService(AppDbContext dbContext, ILogger<TenantService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Tenant>> GetAllTenantsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tenants.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetTenantByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<Tenant?> GetTenantByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Domain == domain && t.IsActive, cancellationToken);
    }

    public async Task<Tenant> CreateTenantAsync(
        string name,
        string displayName,
        string? domain = null,
        string? logoUrl = null,
        string? themeConfig = null,
        CancellationToken cancellationToken = default)
    {
        // 验证租户名称唯一性
        var existing = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"Tenant with name '{name}' already exists");
        }

        // 如果指定了域名，验证域名唯一性
        if (!string.IsNullOrEmpty(domain))
        {
            var domainExists = await _dbContext.Tenants
                .AnyAsync(t => t.Domain == domain, cancellationToken);

            if (domainExists)
            {
                throw new InvalidOperationException($"Tenant with domain '{domain}' already exists");
            }
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Domain = domain,
            LogoUrl = logoUrl,
            ThemeConfig = themeConfig,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);

        return tenant;
    }

    public async Task<Tenant> UpdateTenantAsync(
        Guid id,
        string displayName,
        string? domain = null,
        string? logoUrl = null,
        string? themeConfig = null,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {id} not found");
        }

        // 如果域名改变，验证新域名唯一性
        if (!string.IsNullOrEmpty(domain) && domain != tenant.Domain)
        {
            var domainExists = await _dbContext.Tenants
                .AnyAsync(t => t.Domain == domain && t.Id != id, cancellationToken);

            if (domainExists)
            {
                throw new InvalidOperationException($"Tenant with domain '{domain}' already exists");
            }
        }

        tenant.DisplayName = displayName;
        tenant.Domain = domain;
        tenant.LogoUrl = logoUrl;
        tenant.ThemeConfig = themeConfig;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);

        return tenant;
    }

    public async Task<Tenant> ToggleTenantAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {id} not found");
        }

        tenant.IsActive = isActive;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{Action} tenant {TenantId} ({TenantName})",
            isActive ? "Activated" : "Deactivated",
            tenant.Id,
            tenant.Name);

        return tenant;
    }

    public async Task DeleteTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {id} not found");
        }

        // 检查是否有关联数据
        var hasUsers = await _dbContext.Users.AnyAsync(u => u.TenantId == id, cancellationToken);
        if (hasUsers)
        {
            throw new InvalidOperationException($"Cannot delete tenant {id} because it has associated users. Please deactivate it instead.");
        }

        _dbContext.Tenants.Remove(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);
    }
}

