namespace OneID.Shared.Application.Clients;

public interface IClientCommandService
{
    Task<ClientSummary> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string clientId, CancellationToken cancellationToken = default);
    Task<ClientSummary> UpdateAsync(string clientId, UpdateClientRequest request, CancellationToken cancellationToken = default);
    Task<ClientSummary> UpdateScopesAsync(string clientId, UpdateClientScopesRequest request, CancellationToken cancellationToken = default);
}
