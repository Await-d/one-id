using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Identity.Services;
using OneID.Shared.DTOs;

namespace OneID.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiKeysController(IApiKeyService apiKeyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListApiKeysResponse>> GetApiKeys()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await apiKeyService.GetUserApiKeysAsync(userId.Value);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new CreateApiKeyResponse(Success: false, Message: "Name is required"));
        }

        if (request.Name.Length > 100)
        {
            return BadRequest(new CreateApiKeyResponse(Success: false, Message: "Name must be less than 100 characters"));
        }

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return BadRequest(new CreateApiKeyResponse(Success: false, Message: "Expiration date must be in the future"));
        }

        var result = await apiKeyService.CreateApiKeyAsync(userId.Value, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/revoke")]
    public async Task<ActionResult<RevokeApiKeyResponse>> RevokeApiKey(Guid id, [FromBody] RevokeApiKeyRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await apiKeyService.RevokeApiKeyAsync(userId.Value, id, request.Reason);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
