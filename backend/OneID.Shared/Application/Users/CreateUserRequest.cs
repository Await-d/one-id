namespace OneID.Shared.Application.Users;

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    string? DisplayName,
    bool EmailConfirmed = false);
