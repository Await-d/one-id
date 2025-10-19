namespace OneID.Shared.DTOs;

public record LoginRequest(
    string UserName,
    string Password,
    bool RememberMe = false
);

public record LoginResponse(
    bool Success,
    bool RequiresTwoFactor,
    string? Message,
    string? UserId
);

public record TwoFactorLoginRequest(
    string Code,
    bool RememberMe = false,
    bool IsRecoveryCode = false
);

public record TwoFactorLoginResponse(
    bool Success,
    string? Message
);
