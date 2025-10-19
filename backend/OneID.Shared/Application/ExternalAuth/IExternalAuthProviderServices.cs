namespace OneID.Shared.Application.ExternalAuth;

public interface IExternalAuthProviderQueryService
{
    Task<IReadOnlyList<ExternalAuthProviderSummary>> ListAsync(CancellationToken cancellationToken = default);
    Task<ExternalAuthProviderSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExternalAuthProviderSummary?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalAuthProviderSummary>> GetEnabledAsync(CancellationToken cancellationToken = default);
}

public interface IExternalAuthProviderCommandService
{
    Task<ExternalAuthProviderSummary> CreateAsync(CreateExternalAuthProviderRequest request, CancellationToken cancellationToken = default);
    Task<ExternalAuthProviderSummary> UpdateAsync(Guid id, UpdateExternalAuthProviderRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExternalAuthProviderSummary> ToggleEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken = default);
}
