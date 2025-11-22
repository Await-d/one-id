using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OneID.Shared.Domain;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OneID.Identity.Controllers;

public class AuthorizationController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IOpenIddictApplicationManager applicationManager) : Controller
{
    /// <summary>
    /// 处理授权请求
    /// </summary>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // 获取当前已登录的用户（如果有）
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        // 如果用户未登录，重定向到前端登录页面
        if (result?.Succeeded != true)
        {
            // 构建返回URL（授权请求的完整URL）
            var returnUrl = Request.PathBase + Request.Path + QueryString.Create(
                Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());
            
            // 重定向到前端登录页面，并传递 returnUrl
            return Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        // 获取用户信息
        var user = await userManager.GetUserAsync(result.Principal);
        if (user == null)
        {
            // 如果通过Principal获取失败，尝试从声明中获取用户ID
            var userId = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                userId = result.Principal?.FindFirst("sub")?.Value;
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                userId = result.Principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                user = await userManager.FindByIdAsync(userId);
            }
        }
        
        if (user == null)
        {
            // Log the principal claims for debugging
            var principalClaims = string.Join(", ", result.Principal?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? new[] { "no claims" });
            throw new InvalidOperationException($"The user details cannot be retrieved. Principal claims: {principalClaims}");
        }

        // 检查是否需要显示consent页面
        // 如果请求包含prompt=consent或者用户之前没有同意过，则显示consent页面
        var consentType = request.ClientId != null
            ? await GetConsentTypeAsync(request.ClientId)
            : ConsentTypes.Explicit;

        // 如果是显式同意类型且没有提交consent
        if (consentType == ConsentTypes.Explicit && !HasFormValueAsync("consent_granted"))
        {
            // 重定向到consent页面
            var consentUrl = $"/consent?returnUrl={Uri.EscapeDataString(Request.PathBase + Request.Path + Request.QueryString)}";
            return Redirect(consentUrl);
        }

        // 创建新的ClaimsPrincipal
        var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

        // 为授权码流程设置目标
        principal.SetScopes(request.GetScopes());
        principal.SetResources(await GetResourcesAsync(request.GetScopes()));

        // 批准授权
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
    
    private bool HasFormValueAsync(string key)
    {
        if (Request.HasFormContentType && Request.Form.TryGetValue(key, out var value))
        {
            return value == "true";
        }
        return false;
    }
    
    private async Task<string> GetConsentTypeAsync(string clientId)
    {
        try
        {
            var application = await applicationManager.FindByClientIdAsync(clientId);
            if (application == null)
            {
                // 如果找不到客户端，默认使用 Explicit
                return ConsentTypes.Explicit;
            }

            var consentType = await applicationManager.GetConsentTypeAsync(application);
            
            // 如果未设置 ConsentType 或为 null，默认使用 Explicit
            if (string.IsNullOrEmpty(consentType))
            {
                return ConsentTypes.Explicit;
            }

            return consentType;
        }
        catch (Exception)
        {
            // 如果查询失败，默认使用 Explicit（最安全的选项）
            return ConsentTypes.Explicit;
        }
    }

    /// <summary>
    /// 处理令牌请求
    /// </summary>
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // 从授权码或刷新令牌中恢复ClaimsPrincipal
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // 获取用户信息
            var user = await userManager.FindByIdAsync(result.Principal?.GetClaim(Claims.Subject) ?? string.Empty);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                    }));
            }

            // 确保用户仍然有权登录
            if (!await signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                    }));
            }

            // 创建新的ClaimsPrincipal
            var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

            principal.SetScopes(result.Principal?.GetScopes() ?? []);
            principal.SetResources(await GetResourcesAsync(result.Principal?.GetScopes() ?? []));

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    /// <summary>
    /// 处理用户信息请求
    /// </summary>
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result?.Principal == null)
        {
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is invalid."
                }));
        }

        var user = await userManager.FindByIdAsync(result.Principal.GetClaim(Claims.Subject) ?? string.Empty);
        if (user == null)
        {
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is invalid."
                }));
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = user.Id.ToString(),
            [Claims.Name] = user.DisplayName ?? user.UserName ?? string.Empty
        };

        if (result.Principal.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = user.Email ?? string.Empty;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        if (result.Principal.HasScope(Scopes.Profile))
        {
            claims[Claims.PreferredUsername] = user.UserName ?? string.Empty;
        }

        return Ok(claims);
    }

    /// <summary>
    /// 处理登出请求
    /// </summary>
    [HttpGet("~/connect/endsession")]
    [HttpPost("~/connect/endsession")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        // 登出Identity
        await signInManager.SignOutAsync();

        // 如果有post_logout_redirect_uri，重定向回去
        if (!string.IsNullOrEmpty(request?.PostLogoutRedirectUri))
        {
            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = request.PostLogoutRedirectUri
                });
        }

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// 创建ClaimsPrincipal
    /// </summary>
    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(AppUser user, IEnumerable<string> scopes)
    {
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        // 添加OpenID Connect标准声明
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, user.DisplayName ?? user.UserName);
        identity.SetClaim(Claims.PreferredUsername, user.UserName);

        if (scopes.Contains(Scopes.Email))
        {
            identity.SetClaim(Claims.Email, user.Email);
            identity.SetClaim(Claims.EmailVerified, user.EmailConfirmed);
        }

        // 添加用户角色到 access token
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(Claims.Role, role));
        }

        identity.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject or Claims.Name => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Email or Claims.EmailVerified or Claims.PreferredUsername
                => [Destinations.IdentityToken],
            Claims.Role => [Destinations.AccessToken], // 角色声明只放入 access token
            _ => [Destinations.AccessToken]
        });

        return principal;
    }

    /// <summary>
    /// 获取资源列表
    /// </summary>
    private Task<IEnumerable<string>> GetResourcesAsync(IEnumerable<string> scopes)
    {
        // 暂时返回空列表，后续可以添加API资源配置
        return Task.FromResult(Enumerable.Empty<string>());
    }
}
