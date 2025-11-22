using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;

namespace OneID.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsentController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    UserManager<AppUser> userManager,
    ILocalizationService localizationService) : ControllerBase
{
    /// <summary>
    /// 获取授权同意信息
    /// </summary>
    [HttpGet("info")]
    [Authorize]
    public async Task<IActionResult> GetConsentInfo([FromQuery] string? client_id, [FromQuery] string? scope)
    {
        if (string.IsNullOrWhiteSpace(client_id))
        {
            return BadRequest(new { Message = "client_id is required" });
        }

        // 获取客户端信息
        var application = await applicationManager.FindByClientIdAsync(client_id);
        if (application == null)
        {
            return NotFound(new { Message = "Client not found" });
        }

        var clientName = await applicationManager.GetDisplayNameAsync(application) ?? client_id;
        
        // 解析作用域
        var requestedScopes = scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var scopeDescriptions = new List<object>();

        foreach (var scopeName in requestedScopes)
        {
            var scopeEntity = await scopeManager.FindByNameAsync(scopeName);
            if (scopeEntity != null)
            {
                var displayName = await scopeManager.GetDisplayNameAsync(scopeEntity) ?? scopeName;
                var description = await scopeManager.GetDescriptionAsync(scopeEntity);
                
                scopeDescriptions.Add(new
                {
                    Name = scopeName,
                    DisplayName = displayName,
                    Description = description ?? GetDefaultScopeDescription(scopeName)
                });
            }
        }

        return Ok(new
        {
            ClientId = client_id,
            ClientName = clientName,
            Scopes = scopeDescriptions
        });
    }

    private string GetDefaultScopeDescription(string scopeName)
    {
        return scopeName switch
        {
            "openid" => localizationService.GetString("Scope_OpenId"),
            "profile" => localizationService.GetString("Scope_Profile"),
            "email" => localizationService.GetString("Scope_Email"),
            "offline_access" => localizationService.GetString("Scope_OfflineAccess"),
            "admin_api" => localizationService.GetString("Scope_AdminApi"),
            _ => localizationService.GetString("Scope_Default", scopeName)
        };
    }
}

