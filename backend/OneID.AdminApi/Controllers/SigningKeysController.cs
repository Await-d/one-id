using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// JWT 签名密钥管理
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class SigningKeysController(
    ISigningKeyService signingKeyService,
    ILogger<SigningKeysController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有签名密钥
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SigningKeyDto>>> GetAll(
        [FromQuery] bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var keys = await signingKeyService.GetAllKeysAsync(includeRevoked, cancellationToken);
        var dtos = keys.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// 根据 ID 获取签名密钥
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SigningKeyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var key = await signingKeyService.GetKeyByIdAsync(id.ToString(), cancellationToken);
        if (key == null)
        {
            return NotFound(new { Message = $"Signing key {id} not found" });
        }

        return Ok(MapToDto(key));
    }

    /// <summary>
    /// 获取当前激活的签名密钥
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<SigningKeyDto>> GetActive(
        [FromQuery] string type = "RSA",
        CancellationToken cancellationToken = default)
    {
        var key = await signingKeyService.GetActiveKeyAsync(type, cancellationToken);
        if (key == null)
        {
            return NotFound(new { Message = $"No active {type} key found" });
        }

        return Ok(MapToDto(key));
    }

    /// <summary>
    /// 生成新的 RSA 签名密钥
    /// </summary>
    [HttpPost("rsa")]
    public async Task<ActionResult<SigningKeyDto>> GenerateRsaKey(
        [FromBody] GenerateRsaKeyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = await signingKeyService.GenerateRsaKeyAsync(
                request.KeySize,
                request.ValidityDays,
                request.Notes,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = key.Id }, MapToDto(key));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate RSA key");
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 生成新的 ECDSA 签名密钥
    /// </summary>
    [HttpPost("ecdsa")]
    public async Task<ActionResult<SigningKeyDto>> GenerateEcdsaKey(
        [FromBody] GenerateEcdsaKeyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = await signingKeyService.GenerateEcdsaKeyAsync(
                request.Curve,
                request.ValidityDays,
                request.Notes,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = key.Id }, MapToDto(key));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate ECDSA key");
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 激活指定的签名密钥
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<SigningKeyDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var key = await signingKeyService.ActivateKeyAsync(id.ToString(), cancellationToken);
            return Ok(MapToDto(key));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 撤销指定的签名密钥
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    public async Task<ActionResult<SigningKeyDto>> Revoke(
        Guid id,
        [FromBody] RevokeKeyRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = await signingKeyService.RevokeKeyAsync(id.ToString(), request?.Reason, cancellationToken);
            return Ok(MapToDto(key));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 删除指定的签名密钥
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await signingKeyService.DeleteKeyAsync(id.ToString(), cancellationToken);
            return Ok(new { Message = "Signing key deleted successfully", KeyId = id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 清理过期的签名密钥
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<CleanupResult>> Cleanup(
        [FromQuery] int retentionDays = 30,
        CancellationToken cancellationToken = default)
    {
        var count = await signingKeyService.CleanupExpiredKeysAsync(retentionDays, cancellationToken);
        return Ok(new CleanupResult
        {
            DeletedCount = count,
            RetentionDays = retentionDays,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 检查是否需要轮换密钥
    /// </summary>
    [HttpGet("rotation-status")]
    public async Task<ActionResult<RotationStatus>> GetRotationStatus(
        [FromQuery] string type = "RSA",
        [FromQuery] int warningDays = 30,
        CancellationToken cancellationToken = default)
    {
        var shouldRotate = await signingKeyService.ShouldRotateKeyAsync(type, warningDays, cancellationToken);
        var activeKey = await signingKeyService.GetActiveKeyAsync(type, cancellationToken);

        return Ok(new RotationStatus
        {
            Type = type,
            ShouldRotate = shouldRotate,
            ActiveKey = activeKey != null ? MapToDto(activeKey) : null,
            WarningDays = warningDays,
            Timestamp = DateTime.UtcNow
        });
    }

    private static SigningKeyDto MapToDto(SigningKey key)
    {
        return new SigningKeyDto
        {
            Id = Guid.Parse(key.Id),
            Type = key.Type,
            Use = key.Use,
            Algorithm = key.Algorithm,
            PublicKey = key.PublicKey,
            Version = key.Version,
            IsActive = key.IsActive,
            CreatedAt = key.CreatedAt,
            ActivatedAt = key.ActivatedAt,
            ExpiresAt = key.ExpiresAt ?? DateTime.UtcNow.AddDays(90),
            RevokedAt = key.RevokedAt,
            Notes = key.Notes,
            TenantId = key.TenantId
        };
    }
}

// DTOs
public record GenerateRsaKeyRequest(
    int KeySize = 2048,
    int ValidityDays = 90,
    string? Notes = null);

public record GenerateEcdsaKeyRequest(
    string Curve = "P-256",
    int ValidityDays = 90,
    string? Notes = null);

public record RevokeKeyRequest(string? Reason = null);

public record SigningKeyDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Use { get; init; } = string.Empty;
    public string Algorithm { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
    public int Version { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public string? Notes { get; init; }
    public Guid? TenantId { get; init; }
}

public record CleanupResult
{
    public int DeletedCount { get; init; }
    public int RetentionDays { get; init; }
    public DateTime Timestamp { get; init; }
}

public record RotationStatus
{
    public string Type { get; init; } = string.Empty;
    public bool ShouldRotate { get; init; }
    public SigningKeyDto? ActiveKey { get; init; }
    public int WarningDays { get; init; }
    public DateTime Timestamp { get; init; }
}

