namespace OneID.Shared.Application.ExternalAuth;

public sealed record ExternalAuthProviderSummary(
    Guid Id,
    string ProviderType,
    string Name,
    string DisplayName,
    bool Enabled,
    string ClientId,
    string CallbackPath,
    List<string> Scopes,
    IReadOnlyDictionary<string, string> AdditionalConfig,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateExternalAuthProviderRequest(
    string ProviderType,
    string Name,
    string DisplayName,
    string ClientId,
    string ClientSecret,
    string? CallbackPath = null,
    List<string>? Scopes = null,
    Dictionary<string, string>? AdditionalConfig = null,
    int DisplayOrder = 0);

public sealed record UpdateExternalAuthProviderRequest(
    string? DisplayName = null,
    string? ClientId = null,
    string? ClientSecret = null,
    string? CallbackPath = null,
    bool? Enabled = null,
    List<string>? Scopes = null,
    Dictionary<string, string>? AdditionalConfig = null,
    int? DisplayOrder = null);
