namespace OneID.Shared.DTOs;

public record ConfirmEmailRequest(string Email, string Token);

public record ConfirmEmailResponse(bool Success, string Message);

public record ResendConfirmationRequest(string Email);

public record ResendConfirmationResponse(bool Success, string Message);

