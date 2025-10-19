using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Application.ExternalAuth;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Services;

public sealed class ExternalAuthProviderQueryService(AppDbContext dbContext) : IExternalAuthProviderQueryService
{
    public async Task<IReadOnlyList<ExternalAuthProviderSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var providers = await dbContext.Set<ExternalAuthProvider>()
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return providers.Select(MapToSummary).ToList();
    }

    public async Task<ExternalAuthProviderSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await dbContext.Set<ExternalAuthProvider>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return provider == null ? null : MapToSummary(provider);
    }

    public async Task<ExternalAuthProviderSummary?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var provider = await dbContext.Set<ExternalAuthProvider>()
            .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

        return provider == null ? null : MapToSummary(provider);
    }

    public async Task<IReadOnlyList<ExternalAuthProviderSummary>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        var providers = await dbContext.Set<ExternalAuthProvider>()
            .Where(p => p.Enabled)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return providers.Select(MapToSummary).ToList();
    }

    private static ExternalAuthProviderSummary MapToSummary(ExternalAuthProvider provider)
    {
        var scopes = string.IsNullOrEmpty(provider.Scopes)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(provider.Scopes) ?? new List<string>();

        var additionalConfig = string.IsNullOrEmpty(provider.AdditionalConfig)
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
            additionalConfig,
            provider.DisplayOrder,
            provider.CreatedAt,
            provider.UpdatedAt);
    }
}
