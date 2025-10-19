using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using OneID.Shared.DTOs;
using OneID.Shared.Infrastructure;

namespace OneID.Identity.Services;

public interface IApiKeyService
{
    Task<CreateApiKeyResponse> CreateApiKeyAsync(Guid userId, CreateApiKeyRequest request);
    Task<ListApiKeysResponse> GetUserApiKeysAsync(Guid userId);
    Task<RevokeApiKeyResponse> RevokeApiKeyAsync(Guid userId, Guid apiKeyId, string? reason);
    Task<AppUser?> ValidateApiKeyAsync(string apiKey);
    Task UpdateLastUsedAsync(Guid apiKeyId);
}

public class ApiKeyService(
    AppDbContext dbContext,
    UserManager<AppUser> userManager,
    IAuditLogService auditLogService) : IApiKeyService
{
    private const string KeyPrefix = "ak_";
    private const int KeyLength = 32; // 32 bytes = 256 bits

    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(Guid userId, CreateApiKeyRequest request)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new CreateApiKeyResponse(Success: false, Message: "User not found");
        }

        // Generate secure random API key
        var keyBytes = RandomNumberGenerator.GetBytes(KeyLength);
        var apiKey = KeyPrefix + Convert.ToBase64String(keyBytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
        var keyHash = ComputeHash(apiKey);
        var keyPrefixDisplay = apiKey[..12] + "...";

        var entity = new ApiKey
        {
            UserId = userId,
            Name = request.Name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefixDisplay,
            ExpiresAt = request.ExpiresAt,
            Scopes = request.Scopes != null ? JsonSerializer.Serialize(request.Scopes) : null,
            TenantId = user.TenantId
        };

        dbContext.ApiKeys.Add(entity);
        await dbContext.SaveChangesAsync();

        await auditLogService.LogAsync(
            action: "API Key Created",
            category: "Security",
            success: true,
            details: $"API key '{request.Name}' created (ID: {entity.Id})",
            userId: userId,
            userName: user.UserName
        );

        return new CreateApiKeyResponse(
            Success: true,
            Message: "API key created successfully",
            Id: entity.Id,
            ApiKey: apiKey, // Only returned once
            KeyPrefix: keyPrefixDisplay
        );
    }

    public async Task<ListApiKeysResponse> GetUserApiKeysAsync(Guid userId)
    {
        var apiKeys = await dbContext.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToArrayAsync();

        var apiKeyDtos = apiKeys.Select(k => new ApiKeyDto(
            k.Id,
            k.Name,
            k.KeyPrefix ?? "",
            k.CreatedAt,
            k.LastUsedAt,
            k.ExpiresAt,
            k.IsRevoked,
            k.IsExpired,
            k.IsActive,
            k.Scopes != null ? JsonSerializer.Deserialize<string[]>(k.Scopes) : null
        )).ToArray();

        return new ListApiKeysResponse(Success: true, ApiKeys: apiKeyDtos);
    }

    public async Task<RevokeApiKeyResponse> RevokeApiKeyAsync(Guid userId, Guid apiKeyId, string? reason)
    {
        var apiKey = await dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId);

        if (apiKey == null)
        {
            return new RevokeApiKeyResponse(Success: false, Message: "API key not found");
        }

        if (apiKey.IsRevoked)
        {
            return new RevokeApiKeyResponse(Success: false, Message: "API key is already revoked");
        }

        apiKey.IsRevoked = true;
        apiKey.RevokedAt = DateTime.UtcNow;
        apiKey.RevokedReason = reason;

        await dbContext.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(userId.ToString());
        await auditLogService.LogAsync(
            action: "API Key Revoked",
            category: "Security",
            success: true,
            details: $"API key '{apiKey.Name}' (ID: {apiKey.Id}) revoked. Reason: {reason ?? "None"}",
            userId: userId,
            userName: user?.UserName
        );

        return new RevokeApiKeyResponse(Success: true, Message: "API key revoked successfully");
    }

    public async Task<AppUser?> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || !apiKey.StartsWith(KeyPrefix))
        {
            return null;
        }

        var keyHash = ComputeHash(apiKey);
        var entity = await dbContext.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

        if (entity == null || !entity.IsActive)
        {
            return null;
        }

        return entity.User;
    }

    public async Task UpdateLastUsedAsync(Guid apiKeyId)
    {
        var apiKey = await dbContext.ApiKeys.FindAsync(apiKeyId);
        if (apiKey != null)
        {
            apiKey.LastUsedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
