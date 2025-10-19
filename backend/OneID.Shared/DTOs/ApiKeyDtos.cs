using System;

namespace OneID.Shared.DTOs;

public record CreateApiKeyRequest(
    string Name,
    DateTime? ExpiresAt = null,
    string[]? Scopes = null
);

public record CreateApiKeyResponse(
    bool Success,
    string? Message,
    Guid? Id = null,
    string? ApiKey = null, // Only returned once during creation
    string? KeyPrefix = null
);

public record ApiKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    DateTime? ExpiresAt,
    bool IsRevoked,
    bool IsExpired,
    bool IsActive,
    string[]? Scopes
);

public record ListApiKeysResponse(
    bool Success,
    ApiKeyDto[] ApiKeys
);

public record RevokeApiKeyRequest(
    string? Reason = null
);

public record RevokeApiKeyResponse(
    bool Success,
    string? Message
);
