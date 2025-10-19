namespace OneID.Shared.Application.Clients;

public interface IClientQueryService
{
    Task<IReadOnlyList<ClientSummary>> ListAsync(CancellationToken cancellationToken = default);
}
