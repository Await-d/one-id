using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;

namespace OneID.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalAuthController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    ILogger<ExternalAuthController> logger) : ControllerBase
{
    /// <summary>
    /// 获取支持的外部登录提供者列表
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetExternalProviders()
    {
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        var providers = schemes.Select(s => new
        {
            Name = s.Name,
            DisplayName = s.DisplayName
        }).ToList();

        return Ok(providers);
    }

    /// <summary>
    /// 发起外部登录
    /// </summary>
    [HttpGet("challenge/{provider}")]
    [HttpPost("challenge/{provider}")]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, [FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// 外部登录回调
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            logger.LogWarning("External login info not found");
            return Redirect($"{returnUrl ?? "/"}?error=external_login_failed");
        }

        // 尝试使用外部登录信息登录
        var result = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User logged in with {Provider} provider", info.LoginProvider);
            return Redirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            return Redirect($"{returnUrl ?? "/"}?error=account_locked");
        }

        // 用户不存在，创建新用户
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            logger.LogWarning("Email claim not found in external provider");
            return Redirect($"{returnUrl ?? "/"}?error=email_not_provided");
        }

        var user = await userManager.FindByEmailAsync(email);
        
        if (user == null)
        {
            // 创建新用户
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
                IsExternal = true
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create user: {Errors}", 
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return Redirect($"{returnUrl ?? "/"}?error=user_creation_failed");
            }

            logger.LogInformation("Created new user account for {Email} from {Provider}", email, info.LoginProvider);
        }

        // 关联外部登录
        var addLoginResult = await userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            logger.LogError("Failed to add external login: {Errors}", 
                string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            return Redirect($"{returnUrl ?? "/"}?error=link_failed");
        }

        // 登录用户
        await signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        logger.LogInformation("User {Email} logged in with {Provider}", email, info.LoginProvider);

        return Redirect(returnUrl ?? "/");
    }

    /// <summary>
    /// 获取当前用户的外部登录列表
    /// </summary>
    [HttpGet("logins")]
    [Authorize]
    public async Task<IActionResult> GetExternalLogins()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var logins = await userManager.GetLoginsAsync(user);
        return Ok(logins.Select(l => new
        {
            l.LoginProvider,
            l.ProviderKey,
            l.ProviderDisplayName
        }));
    }

    /// <summary>
    /// 绑定外部登录到当前账号
    /// </summary>
    [HttpPost("link/{provider}")]
    [Authorize]
    public IActionResult LinkExternalLogin(string provider)
    {
        var redirectUrl = Url.Action(nameof(LinkExternalLoginCallback));
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// 绑定外部登录回调
    /// </summary>
    [HttpGet("link-callback")]
    [Authorize]
    public async Task<IActionResult> LinkExternalLoginCallback()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var info = await signInManager.GetExternalLoginInfoAsync(user.Id.ToString());
        if (info == null)
        {
            logger.LogWarning("External login info not found for linking");
            return Redirect("/?error=link_failed");
        }

        var result = await userManager.AddLoginAsync(user, info);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to link external login: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Redirect("/?error=link_failed");
        }

        logger.LogInformation("User {Email} linked {Provider} account", user.Email, info.LoginProvider);
        return Redirect("/?success=linked");
    }

    /// <summary>
    /// 解绑外部登录
    /// </summary>
    [HttpDelete("unlink/{provider}/{providerKey}")]
    [Authorize]
    public async Task<IActionResult> UnlinkExternalLogin(string provider, string providerKey)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await userManager.RemoveLoginAsync(user, provider, providerKey);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to unlink external login: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { error = "unlink_failed" });
        }

        logger.LogInformation("User {Email} unlinked {Provider} account", user.Email, provider);
        return Ok(new { message = "External login unlinked successfully" });
    }
}
