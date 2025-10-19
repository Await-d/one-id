using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OneID.Identity.Services;
using OneID.Shared.Domain;
using OneID.Shared.DTOs;
using OneID.Shared.Infrastructure;

namespace OneID.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MfaController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IMfaService _mfaService;
    private readonly IAuditLogService _auditLogService;

    public MfaController(
        UserManager<AppUser> userManager,
        IMfaService mfaService,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _mfaService = mfaService;
        _auditLogService = auditLogService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<MfaStatusResponse>> GetStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var response = new MfaStatusResponse(
            Enabled: user.TwoFactorEnabled,
            HasRecoveryCodes: !string.IsNullOrEmpty(user.RecoveryCodes)
        );

        return Ok(response);
    }

    [HttpPost("enable")]
    public async Task<ActionResult<EnableMfaResponse>> Enable([FromBody] EnableMfaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _auditLogService.LogAsync(
                action: "MFA Enable Failed",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: "Invalid password"
            );
            return BadRequest(new { error = "Invalid password" });
        }

        // Generate secret and recovery codes
        var secret = _mfaService.GenerateSecret();
        var recoveryCodes = _mfaService.GenerateRecoveryCodes();

        // Encrypt and save
        user.TotpSecret = _mfaService.EncryptSecret(secret);
        user.RecoveryCodes = _mfaService.EncryptRecoveryCodes(recoveryCodes);
        
        await _userManager.UpdateAsync(user);

        // Generate QR code
        var qrCodeUrl = _mfaService.GenerateQrCodeUrl(user.Email ?? user.UserName!, secret, "OneID");

        await _auditLogService.LogAsync(
            action: "MFA Setup Initiated",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(new EnableMfaResponse(
            Secret: secret,
            QrCodeUrl: qrCodeUrl,
            RecoveryCodes: recoveryCodes
        ));
    }

    [HttpPost("verify")]
    public async Task<ActionResult> Verify([FromBody] VerifyMfaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.TotpSecret))
            return NotFound();

        var secret = _mfaService.DecryptSecret(user.TotpSecret);
        var isValid = _mfaService.ValidateTotp(secret, request.Code);

        if (!isValid)
        {
            await _auditLogService.LogAsync(
                action: "MFA Verification Failed",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: "Invalid TOTP code"
            );
            return BadRequest(new { error = "Invalid verification code" });
        }

        // Enable 2FA
        user.TwoFactorEnabled = true;
        await _userManager.UpdateAsync(user);

        await _auditLogService.LogAsync(
            action: "MFA Enabled",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(new { message = "MFA enabled successfully" });
    }

    [HttpPost("disable")]
    public async Task<ActionResult> Disable([FromBody] DisableMfaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _auditLogService.LogAsync(
                action: "MFA Disable Failed",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: "Invalid password"
            );
            return BadRequest(new { error = "Invalid password" });
        }

        // Disable 2FA and clear secrets
        user.TwoFactorEnabled = false;
        user.TotpSecret = null;
        user.RecoveryCodes = null;
        await _userManager.UpdateAsync(user);

        await _auditLogService.LogAsync(
            action: "MFA Disabled",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(new { message = "MFA disabled successfully" });
    }

    [HttpPost("validate-recovery-code")]
    public async Task<ActionResult> ValidateRecoveryCode([FromBody] VerifyMfaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.RecoveryCodes))
            return NotFound();

        var recoveryCodes = _mfaService.DecryptRecoveryCodes(user.RecoveryCodes);
        var code = request.Code.ToUpperInvariant();

        if (!recoveryCodes.Contains(code))
        {
            await _auditLogService.LogAsync(
                action: "Recovery Code Validation Failed",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: "Invalid recovery code"
            );
            return BadRequest(new { error = "Invalid recovery code" });
        }

        // Remove used recovery code
        var remainingCodes = recoveryCodes.Where(c => c != code).ToArray();
        user.RecoveryCodes = remainingCodes.Length > 0 
            ? _mfaService.EncryptRecoveryCodes(remainingCodes) 
            : null;
        await _userManager.UpdateAsync(user);

        await _auditLogService.LogAsync(
            action: "Recovery Code Used",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(new { message = "Recovery code validated", remainingCodes = remainingCodes.Length });
    }

    [HttpPost("regenerate-recovery-codes")]
    public async Task<ActionResult<string[]>> RegenerateRecoveryCodes([FromBody] DisableMfaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.TwoFactorEnabled)
            return NotFound();

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _auditLogService.LogAsync(
                action: "Recovery Codes Regeneration Failed",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: "Invalid password"
            );
            return BadRequest(new { error = "Invalid password" });
        }

        // Generate new recovery codes
        var recoveryCodes = _mfaService.GenerateRecoveryCodes();
        user.RecoveryCodes = _mfaService.EncryptRecoveryCodes(recoveryCodes);
        await _userManager.UpdateAsync(user);

        await _auditLogService.LogAsync(
            action: "Recovery Codes Regenerated",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(recoveryCodes);
    }

    [HttpGet("qrcode")]
    public async Task<ActionResult> GetQrCode()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.TotpSecret))
            return NotFound();

        var secret = _mfaService.DecryptSecret(user.TotpSecret);
        var qrCodeUrl = _mfaService.GenerateQrCodeUrl(user.Email ?? user.UserName!, secret, "OneID");
        var qrCodeImage = _mfaService.GenerateQrCode(qrCodeUrl);

        return File(qrCodeImage, "image/png");
    }
}
