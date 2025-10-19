namespace OneID.Shared.DTOs;

public record ForgotPasswordRequest(
    string Email
);

public record ForgotPasswordResponse(
    bool Success,
    string? Message
);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword
);

public record ResetPasswordResponse(
    bool Success,
    string? Message
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ChangePasswordResponse(
    bool Success,
    string? Message
);
