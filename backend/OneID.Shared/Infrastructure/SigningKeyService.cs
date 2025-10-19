using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public interface ISigningKeyService
{
    Task<SigningKey> GenerateRsaKeyAsync(int keySize = 2048, int validityDays = 90, string? notes = null, CancellationToken cancellationToken = default);
    Task<SigningKey> GenerateEcdsaKeyAsync(string curve = "P-256", int validityDays = 90, string? notes = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SigningKey>> GetAllKeysAsync(bool includeRevoked = false, CancellationToken cancellationToken = default);
    Task<SigningKey?> GetKeyByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<SigningKey?> GetActiveKeyAsync(string type = "RSA", CancellationToken cancellationToken = default);
    Task<SigningKey> ActivateKeyAsync(string id, CancellationToken cancellationToken = default);
    Task<SigningKey> RevokeKeyAsync(string id, string? reason = null, CancellationToken cancellationToken = default);
    Task DeleteKeyAsync(string id, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredKeysAsync(int retentionDays = 30, CancellationToken cancellationToken = default);
    Task<bool> ShouldRotateKeyAsync(string type = "RSA", int warningDays = 30, CancellationToken cancellationToken = default);
}

public sealed class SigningKeyService : ISigningKeyService
{
    private readonly AppDbContext _dbContext;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<SigningKeyService> _logger;
    private readonly IDataProtector _protector;

    public SigningKeyService(
        AppDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<SigningKeyService> logger)
    {
        _dbContext = dbContext;
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
        _protector = dataProtectionProvider.CreateProtector("SigningKey.PrivateKey");
    }

    public async Task<SigningKey> GenerateRsaKeyAsync(
        int keySize = 2048,
        int validityDays = 90,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating RSA signing key with size {KeySize}", keySize);

        using var rsa = RSA.Create(keySize);

        // Export keys in PEM format
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();

        // Encrypt private key
        var encryptedPrivateKey = _protector.Protect(privateKeyPem);

        // Get current version
        var currentVersion = await GetNextVersionAsync("RSA", cancellationToken);

        var signingKey = new SigningKey
        {
            Id = Guid.NewGuid().ToString(),
            Type = "RSA",
            Use = "sig",
            Algorithm = $"RS{keySize}",
            EncryptedPrivateKey = encryptedPrivateKey,
            PublicKey = publicKeyPem,
            Version = currentVersion,
            IsActive = false, // Must be manually activated
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = null,
            ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
            RevokedAt = null,
            Notes = notes
        };

        _dbContext.SigningKeys.Add(signingKey);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("RSA signing key {KeyId} generated successfully (Version: {Version})", signingKey.Id, signingKey.Version);

        return signingKey;
    }

    public async Task<SigningKey> GenerateEcdsaKeyAsync(
        string curve = "P-256",
        int validityDays = 90,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating ECDSA signing key with curve {Curve}", curve);

        // Map curve name to ECCurve
        var ecCurve = curve switch
        {
            "P-256" => ECCurve.NamedCurves.nistP256,
            "P-384" => ECCurve.NamedCurves.nistP384,
            "P-521" => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentException($"Unsupported curve: {curve}", nameof(curve))
        };

        using var ecdsa = ECDsa.Create(ecCurve);

        // Export keys in PEM format
        var privateKeyPem = ecdsa.ExportECPrivateKeyPem();
        var publicKeyPem = ecdsa.ExportSubjectPublicKeyInfoPem();

        // Encrypt private key
        var encryptedPrivateKey = _protector.Protect(privateKeyPem);

        // Algorithm name
        var algorithm = curve switch
        {
            "P-256" => "ES256",
            "P-384" => "ES384",
            "P-521" => "ES512",
            _ => "ES256"
        };

        // Get current version
        var currentVersion = await GetNextVersionAsync("EC", cancellationToken);

        var signingKey = new SigningKey
        {
            Id = Guid.NewGuid().ToString(),
            Type = "EC",
            Use = "sig",
            Algorithm = algorithm,
            EncryptedPrivateKey = encryptedPrivateKey,
            PublicKey = publicKeyPem,
            Version = currentVersion,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = null,
            ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
            RevokedAt = null,
            Notes = notes
        };

        _dbContext.SigningKeys.Add(signingKey);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("ECDSA signing key {KeyId} generated successfully (Version: {Version})", signingKey.Id, signingKey.Version);

        return signingKey;
    }

    public async Task<IReadOnlyList<SigningKey>> GetAllKeysAsync(
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SigningKeys.AsQueryable();

        if (!includeRevoked)
        {
            query = query.Where(k => k.RevokedAt == null);
        }

        return await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SigningKey?> GetKeyByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SigningKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
    }

    public async Task<SigningKey?> GetActiveKeyAsync(string type = "RSA", CancellationToken cancellationToken = default)
    {
        return await _dbContext.SigningKeys
            .Where(k => k.Type == type && k.IsActive && k.RevokedAt == null && k.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(k => k.ActivatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SigningKey> ActivateKeyAsync(string id, CancellationToken cancellationToken = default)
    {
        var key = await GetKeyByIdAsync(id, cancellationToken);
        if (key == null)
        {
            throw new InvalidOperationException($"Signing key {id} not found");
        }

        if (key.RevokedAt != null)
        {
            throw new InvalidOperationException($"Cannot activate revoked key {id}");
        }

        if (key.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Cannot activate expired key {id}");
        }

        // Deactivate other keys of the same type
        var otherActiveKeys = await _dbContext.SigningKeys
            .Where(k => k.Type == key.Type && k.IsActive && k.Id != id)
            .ToListAsync(cancellationToken);

        foreach (var otherKey in otherActiveKeys)
        {
            otherKey.IsActive = false;
            _logger.LogInformation("Deactivated signing key {KeyId} (Version: {Version})", otherKey.Id, otherKey.Version);
        }

        // Activate the new key
        key.IsActive = true;
        key.ActivatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated signing key {KeyId} (Version: {Version}, Type: {Type})", key.Id, key.Version, key.Type);

        return key;
    }

    public async Task<SigningKey> RevokeKeyAsync(string id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var key = await GetKeyByIdAsync(id, cancellationToken);
        if (key == null)
        {
            throw new InvalidOperationException($"Signing key {id} not found");
        }

        if (key.RevokedAt != null)
        {
            throw new InvalidOperationException($"Key {id} is already revoked");
        }

        key.IsActive = false;
        key.RevokedAt = DateTime.UtcNow;
        key.Notes = string.IsNullOrEmpty(key.Notes)
            ? $"Revoked: {reason ?? "No reason provided"}"
            : $"{key.Notes}\nRevoked: {reason ?? "No reason provided"}";

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Revoked signing key {KeyId} (Version: {Version}). Reason: {Reason}", key.Id, key.Version, reason ?? "Not specified");

        return key;
    }

    public async Task DeleteKeyAsync(string id, CancellationToken cancellationToken = default)
    {
        var key = await GetKeyByIdAsync(id, cancellationToken);
        if (key == null)
        {
            throw new InvalidOperationException($"Signing key {id} not found");
        }

        if (key.IsActive)
        {
            throw new InvalidOperationException($"Cannot delete active key {id}. Revoke it first.");
        }

        _dbContext.SigningKeys.Remove(key);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted signing key {KeyId} (Version: {Version})", key.Id, key.Version);
    }

    public async Task<int> CleanupExpiredKeysAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var expiredKeys = await _dbContext.SigningKeys
            .Where(k => k.ExpiresAt < cutoffDate && !k.IsActive)
            .ToListAsync(cancellationToken);

        if (expiredKeys.Count == 0)
        {
            _logger.LogInformation("No expired keys to clean up");
            return 0;
        }

        _dbContext.SigningKeys.RemoveRange(expiredKeys);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleaned up {Count} expired signing keys", expiredKeys.Count);

        return expiredKeys.Count;
    }

    public async Task<bool> ShouldRotateKeyAsync(string type = "RSA", int warningDays = 30, CancellationToken cancellationToken = default)
    {
        var activeKey = await GetActiveKeyAsync(type, cancellationToken);

        if (activeKey == null)
        {
            _logger.LogWarning("No active {Type} key found. Key rotation recommended.", type);
            return true;
        }

        var warningDate = DateTime.UtcNow.AddDays(warningDays);
        if (activeKey.ExpiresAt <= warningDate)
        {
            _logger.LogWarning(
                "Active {Type} key {KeyId} expires soon ({ExpiresAt}). Key rotation recommended.",
                type,
                activeKey.Id,
                activeKey.ExpiresAt);
            return true;
        }

        return false;
    }

    private async Task<int> GetNextVersionAsync(string type, CancellationToken cancellationToken)
    {
        var maxVersion = await _dbContext.SigningKeys
            .Where(k => k.Type == type)
            .MaxAsync(k => (int?)k.Version, cancellationToken);

        return (maxVersion ?? 0) + 1;
    }
}

