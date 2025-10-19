namespace OneID.Shared.DTOs;

public record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    string? DisplayName
);

public record RegisterResponse(
    bool Success,
    string? Message,
    string? UserId
);
