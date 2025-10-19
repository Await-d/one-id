namespace OneID.Shared.Application.Users;

public sealed record UpdateUserRequest(
    string? DisplayName,
    bool? EmailConfirmed,
    bool? LockoutEnabled);
