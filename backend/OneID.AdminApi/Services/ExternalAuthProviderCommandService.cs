using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Application.ExternalAuth;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Services;

public sealed class ExternalAuthProviderCommandService(
    AppDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<ExternalAuthProviderCommandService> logger) : IExternalAuthProviderCommandService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("ExternalAuthProvider.ClientSecret");

    public async Task<ExternalAuthProviderSummary> CreateAsync(
        CreateExternalAuthProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        // 检查名称是否已存在
        var exists = await dbContext.Set<ExternalAuthProvider>()
            .AnyAsync(p => p.Name == request.Name, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Provider with name '{request.Name}' already exists");
        }

        var callbackPath = request.CallbackPath ?? $"/signin-{request.Name.ToLowerInvariant()}";
        var scopes = request.Scopes ?? new List<string>();

        var additionalConfigJson = request.AdditionalConfig is { Count: > 0 }
            ? JsonSerializer.Serialize(request.AdditionalConfig)
            : null;

        var provider = new ExternalAuthProvider
        {
            Id = Guid.NewGuid(),
            ProviderType = request.ProviderType,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Enabled = false, // 默认禁用，需手动启用
            ClientId = request.ClientId,
            ClientSecret = _protector.Protect(request.ClientSecret), // 加密存储
            CallbackPath = callbackPath,
            Scopes = JsonSerializer.Serialize(scopes),
            AdditionalConfig = additionalConfigJson,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Set<ExternalAuthProvider>().Add(provider);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created external auth provider: {Name}", provider.Name);

        return MapToSummary(provider);
    }

    public async Task<ExternalAuthProviderSummary> UpdateAsync(
        Guid id,
        UpdateExternalAuthProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = await dbContext.Set<ExternalAuthProvider>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException($"Provider with ID {id} not found");
        }

        var updated = false;

        if (request.DisplayName != null && provider.DisplayName != request.DisplayName)
        {
            provider.DisplayName = request.DisplayName;
            updated = true;
        }

        if (request.ClientId != null && provider.ClientId != request.ClientId)
        {
            provider.ClientId = request.ClientId;
            updated = true;
        }

        if (request.ClientSecret != null)
        {
            provider.ClientSecret = _protector.Protect(request.ClientSecret);
            updated = true;
        }

        if (request.CallbackPath != null && provider.CallbackPath != request.CallbackPath)
        {
            provider.CallbackPath = request.CallbackPath;
            updated = true;
        }

        if (request.Enabled.HasValue && provider.Enabled != request.Enabled.Value)
        {
            provider.Enabled = request.Enabled.Value;
            updated = true;
        }

        if (request.Scopes != null)
        {
            provider.Scopes = JsonSerializer.Serialize(request.Scopes);
            updated = true;
        }

        if (request.AdditionalConfig != null)
        {
            provider.AdditionalConfig = request.AdditionalConfig.Count > 0
                ? JsonSerializer.Serialize(request.AdditionalConfig)
                : null;
            updated = true;
        }

        if (request.DisplayOrder.HasValue && provider.DisplayOrder != request.DisplayOrder.Value)
        {
            provider.DisplayOrder = request.DisplayOrder.Value;
            updated = true;
        }

        if (updated)
        {
            provider.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated external auth provider: {Name}", provider.Name);
        }

        return MapToSummary(provider);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await dbContext.Set<ExternalAuthProvider>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException($"Provider with ID {id} not found");
        }

        dbContext.Set<ExternalAuthProvider>().Remove(provider);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted external auth provider: {Name}", provider.Name);
    }

    public async Task<ExternalAuthProviderSummary> ToggleEnabledAsync(
        Guid id,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var provider = await dbContext.Set<ExternalAuthProvider>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (provider == null)
        {
            throw new InvalidOperationException($"Provider with ID {id} not found");
        }

        provider.Enabled = enabled;
        provider.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Toggled external auth provider {Name} to {Enabled}", provider.Name, enabled);

        return MapToSummary(provider);
    }

    private ExternalAuthProviderSummary MapToSummary(ExternalAuthProvider provider)
    {
        var scopes = string.IsNullOrEmpty(provider.Scopes)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(provider.Scopes) ?? new List<string>();

        var additional = string.IsNullOrEmpty(provider.AdditionalConfig)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(provider.AdditionalConfig)
              ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new ExternalAuthProviderSummary(
            provider.Id,
            provider.ProviderType,
            provider.Name,
            provider.DisplayName,
            provider.Enabled,
            provider.ClientId,
            provider.CallbackPath,
            scopes,
            additional,
            provider.DisplayOrder,
            provider.CreatedAt,
            provider.UpdatedAt);
    }
}
