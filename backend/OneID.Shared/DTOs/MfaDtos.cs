namespace OneID.Shared.DTOs;

public record EnableMfaRequest(
    string Password
);

public record EnableMfaResponse(
    string Secret,
    string QrCodeUrl,
    string[] RecoveryCodes
);

public record VerifyMfaRequest(
    string Code
);

public record DisableMfaRequest(
    string Password
);

public record ValidateTotpRequest(
    string Code
);

public record MfaStatusResponse(
    bool Enabled,
    bool HasRecoveryCodes
);
