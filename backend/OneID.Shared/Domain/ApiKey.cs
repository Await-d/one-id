using System;

namespace OneID.Shared.Domain;

public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty; // SHA256 hash of the actual key
    public string? KeyPrefix { get; set; } // First 8 characters for display (e.g., "ak_12345...")

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    public Guid? TenantId { get; set; }

    // Permissions/Scopes
    public string? Scopes { get; set; } // JSON array of scopes/permissions

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool IsActive => !IsRevoked && !IsExpired;
}
