namespace OneID.Shared.Application.Clients;

public sealed record ClientSummary(
    string ClientId,
    string DisplayName,
    string ClientType,
    IReadOnlyCollection<string> RedirectUris,
    IReadOnlyCollection<string> PostLogoutRedirectUris,
    IReadOnlyCollection<string> Scopes);
