namespace OneID.Shared.Application.Users;

public sealed record UserSummary(
    Guid Id,
    string UserName,
    string Email,
    bool EmailConfirmed,
    string? DisplayName,
    bool IsExternal,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<ExternalLoginInfo> ExternalLogins);

public sealed record ExternalLoginInfo(
    string LoginProvider,
    string ProviderKey,
    string? ProviderDisplayName);
