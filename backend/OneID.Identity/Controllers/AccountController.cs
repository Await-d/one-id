using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.DTOs;
using OneID.Shared.Infrastructure;

namespace OneID.Identity.Controllers;

/// <summary>
/// 账户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IAuditLogService auditLogService,
    IEmailService emailService,
    IConfiguration configuration,
    ILocalizationService localization,
    ISystemSettingsService systemSettings,
    IAnomalyDetectionService anomalyDetectionService,
    IUserDeviceService userDeviceService,
    INotificationService notificationService,
    ILogger<AccountController> logger) : ControllerBase
{
    private readonly ILogger<AccountController> _logger = logger;
    /// <summary>
    /// 检查注册是否启用
    /// </summary>
    [HttpGet("registration-enabled")]
    public async Task<ActionResult<bool>> IsRegistrationEnabled()
    {
        var enabled = await systemSettings.GetBoolValueAsync(
            SystemSettingKeys.RegistrationEnabled, 
            defaultValue: true);
        
        return Ok(enabled);
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        // 检查注册是否启用
        var registrationEnabled = await systemSettings.GetBoolValueAsync(
            SystemSettingKeys.RegistrationEnabled, 
            defaultValue: true);
        
        if (!registrationEnabled)
        {
            var errorMsg = localization.GetString("RegistrationDisabled") 
                ?? "User registration is currently disabled. Please contact the administrator.";
            
            await auditLogService.LogAsync(
                action: "Registration Attempt Blocked",
                category: "User",
                success: false,
                errorMessage: "Registration is disabled"
            );
            
            return BadRequest(new RegisterResponse(
                Success: false,
                Message: errorMsg,
                UserId: null
            ));
        }
        
        // 验证用户名是否已存在
        var existingUser = await userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
        {
            var errorMsg = localization.GetString("UsernameExists");
            await auditLogService.LogAsync(
                action: "User Registration Failed",
                category: "User",
                success: false,
                errorMessage: errorMsg
            );
            return BadRequest(new RegisterResponse(
                Success: false,
                Message: errorMsg,
                UserId: null
            ));
        }

        // 验证邮箱是否已存在
        var existingEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
        {
            var errorMsg = localization.GetString("EmailExists");
            await auditLogService.LogAsync(
                action: "User Registration Failed",
                category: "User",
                success: false,
                errorMessage: errorMsg
            );
            return BadRequest(new RegisterResponse(
                Success: false,
                Message: errorMsg,
                UserId: null
            ));
        }

        // 创建用户
        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = false, // 需要邮箱验证
            IsExternal = false
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            await auditLogService.LogAsync(
                action: "User Registration Failed",
                category: "User",
                success: false,
                errorMessage: errors
            );
            return BadRequest(new RegisterResponse(
                Success: false,
                Message: errors,
                UserId: null
            ));
        }

        // 生成邮箱验证令牌并发送邮件
        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = $"{configuration["App:BaseUrl"]}/confirm-email";
        
        try
        {
            await emailService.SendEmailConfirmationAsync(user.Email, confirmationToken, confirmationUrl);
        }
        catch (Exception ex)
        {
            // 邮件发送失败不影响注册流程，记录日志即可
            await auditLogService.LogAsync(
                action: "Email Confirmation Sent Failed",
                category: "User",
                userId: user.Id,
                success: false,
                errorMessage: ex.Message
            );
        }

        await auditLogService.LogAsync(
            action: "User Registered",
            category: "User",
            userId: user.Id,
            success: true
        );

        return Ok(new RegisterResponse(
            Success: true,
            Message: "Registration successful. Please check your email to confirm your account.",
            UserId: user.Id.ToString()
        ));
    }

    /// <summary>
    /// 用户名密码登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var user = await userManager.FindByNameAsync(request.UserName);
        if (user == null)
        {
            // 记录失败的登录尝试（用户名不存在）
            await anomalyDetectionService.RecordAndAnalyzeLoginAsync(
                userId: Guid.Empty,
                userName: request.UserName,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: false,
                failureReason: "User not found"
            );

            await auditLogService.LogAsync(
                action: "Login Failed",
                category: "Authentication",
                success: false,
                errorMessage: "Invalid username or password"
            );
            return BadRequest(new LoginResponse(
                Success: false,
                RequiresTwoFactor: false,
                Message: "Invalid username or password",
                UserId: null
            ));
        }

        // 检查密码
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            // 记录账户锁定的登录尝试
            await anomalyDetectionService.RecordAndAnalyzeLoginAsync(
                userId: user.Id,
                userName: user.UserName,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: false,
                failureReason: "Account locked out"
            );

            await auditLogService.LogAsync(
                action: "Login Failed - Account Locked",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: "Account is locked out"
            );
            return BadRequest(new LoginResponse(
                Success: false,
                RequiresTwoFactor: false,
                Message: "Account is locked out. Please try again later.",
                UserId: null
            ));
        }

        if (!result.Succeeded)
        {
            // 记录密码错误的登录尝试
            await anomalyDetectionService.RecordAndAnalyzeLoginAsync(
                userId: user.Id,
                userName: user.UserName,
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: false,
                failureReason: "Invalid password"
            );

            await auditLogService.LogAsync(
                action: "Login Failed",
                category: "Authentication",
                userId: user.Id,
                success: false,
                errorMessage: "Invalid password"
            );
            return BadRequest(new LoginResponse(
                Success: false,
                RequiresTwoFactor: false,
                Message: "Invalid username or password",
                UserId: null
            ));
        }

        // 检查是否需要MFA
        if (user.TwoFactorEnabled)
        {
            // 不直接登录，而是设置TwoFactor cookie
            await signInManager.SignInAsync(user, isPersistent: request.RememberMe, authenticationMethod: "Password");
           
            await auditLogService.LogAsync(
                action: "Login - MFA Required",
                category: "Authentication",
                userId: user.Id,
                success: true
            );

            return Ok(new LoginResponse(
                Success: true,
                RequiresTwoFactor: true,
                Message: "Two-factor authentication required",
                UserId: user.Id.ToString()
            ));
        }

        // 直接登录成功 - 记录成功的登录
        await signInManager.SignInAsync(user, isPersistent: request.RememberMe);

        var anomalyResult = await anomalyDetectionService.RecordAndAnalyzeLoginAsync(
            userId: user.Id,
            userName: user.UserName,
            ipAddress: ipAddress,
            userAgent: userAgent,
            success: true
        );

        // 检测到异常登录时记录日志并发送通知
        if (anomalyResult.IsAnomalous)
        {
            _logger.LogWarning(
                "Anomalous login detected for user {UserName}. Risk Score: {RiskScore}, Reasons: {Reasons}",
                user.UserName,
                anomalyResult.RiskScore,
                string.Join(", ", anomalyResult.AnomalyReasons)
            );

            // 发送异常登录通知（异步后台任务）
            _ = Task.Run(async () =>
            {
                try
                {
                    var loginHistory = await anomalyDetectionService.GetAllAnomalousLoginsAsync(
                        startDate: DateTime.UtcNow.AddMinutes(-1),
                        endDate: DateTime.UtcNow,
                        pageNumber: 1,
                        pageSize: 1
                    );
                    if (loginHistory.Any())
                    {
                        await notificationService.SendAnomalousLoginNotificationAsync(
                            user, 
                            loginHistory.First(), 
                            anomalyResult.AnomalyReasons, 
                            anomalyResult.RiskScore
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send anomalous login notification");
                }
            });
        }

        // 记录设备信息并检测新设备
        try
        {
            var deviceInfo = ParseUserAgent(userAgent);
            var deviceFingerprint = GenerateSimpleFingerprint(userAgent, ipAddress);
            
            var device = await userDeviceService.RecordDeviceAsync(
                userId: user.Id,
                deviceFingerprint: deviceFingerprint,
                deviceInfo: deviceInfo,
                ipAddress: ipAddress
            );

            // 检查是否是新设备（UsageCount为1表示刚创建）
            if (device.UsageCount == 1)
            {
                _logger.LogInformation("New device detected for user {UserId}: {DeviceName}", user.Id, device.DeviceName);
                
                // 发送新设备登录通知（异步后台任务）
                var finalIpAddress = ipAddress;
                var finalUser = user;
                var finalDevice = device;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var location = "Unknown";
                        await notificationService.SendNewDeviceLoginNotificationAsync(
                            finalUser,
                            finalDevice,
                            finalIpAddress ?? "Unknown",
                            location
                        );
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogError(notifyEx, "Failed to send new device notification");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record device for user {UserId}", user.Id);
            // 不阻止登录流程
        }

        await auditLogService.LogAsync(
            action: "Login Successful",
            category: "Authentication",
            userId: user.Id,
            success: true
        );

        return Ok(new LoginResponse(
            Success: true,
            RequiresTwoFactor: false,
            Message: "Login successful",
            UserId: user.Id.ToString()
        ));
    }

    /// <summary>
    /// 两步验证登录
    /// </summary>
    [HttpPost("login-2fa")]
    public async Task<ActionResult<TwoFactorLoginResponse>> LoginTwoFactor([FromBody] TwoFactorLoginRequest request)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return BadRequest(new TwoFactorLoginResponse(
                Success: false,
                Message: "Invalid two-factor authentication state"
            ));
        }

        bool isValid = false;

        if (request.IsRecoveryCode)
        {
            // 使用恢复码登录
            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(request.Code);
            isValid = result.Succeeded;

            if (isValid)
            {
                await auditLogService.LogAsync(
                    action: "Login with Recovery Code",
                    category: "Security",
                    userId: user.Id,
                    success: true
                );
            }
        }
        else
        {
            // 使用TOTP验证码登录
            var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                request.Code,
                request.RememberMe,
                rememberClient: request.RememberMe
            );
            isValid = result.Succeeded;

            if (result.IsLockedOut)
            {
                await auditLogService.LogAsync(
                    action: "2FA Login Failed - Account Locked",
                    category: "Security",
                    userId: user.Id,
                    success: false
                );
                return BadRequest(new TwoFactorLoginResponse(
                    Success: false,
                    Message: "Account is locked out"
                ));
            }
        }

        if (!isValid)
        {
            await auditLogService.LogAsync(
                action: "2FA Login Failed",
                category: "Authentication",
                userId: user.Id,
                success: false,
                errorMessage: "Invalid verification code"
            );
            return BadRequest(new TwoFactorLoginResponse(
                Success: false,
                Message: "Invalid verification code"
            ));
        }

        // 记录成功的 2FA 登录
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var anomalyResult = await anomalyDetectionService.RecordAndAnalyzeLoginAsync(
            userId: user.Id,
            userName: user.UserName,
            ipAddress: ipAddress,
            userAgent: userAgent,
            success: true
        );

        if (anomalyResult.IsAnomalous)
        {
            _logger.LogWarning(
                "Anomalous 2FA login detected for user {UserName}. Risk Score: {RiskScore}, Reasons: {Reasons}",
                user.UserName,
                anomalyResult.RiskScore,
                string.Join(", ", anomalyResult.AnomalyReasons)
            );

            // 发送异常登录通知
            _ = Task.Run(async () =>
            {
                try
                {
                    var loginHistory = await anomalyDetectionService.GetAllAnomalousLoginsAsync(
                        startDate: DateTime.UtcNow.AddMinutes(-1),
                        endDate: DateTime.UtcNow,
                        pageNumber: 1,
                        pageSize: 1
                    );
                    if (loginHistory.Any())
                    {
                        await notificationService.SendAnomalousLoginNotificationAsync(
                            user, 
                            loginHistory.First(), 
                            anomalyResult.AnomalyReasons, 
                            anomalyResult.RiskScore
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send anomalous 2FA login notification");
                }
            });
        }

        // 记录设备信息并检测新设备
        try
        {
            var deviceInfo = ParseUserAgent(userAgent);
            var deviceFingerprint = GenerateSimpleFingerprint(userAgent, ipAddress);
            
            var device = await userDeviceService.RecordDeviceAsync(
                userId: user.Id,
                deviceFingerprint: deviceFingerprint,
                deviceInfo: deviceInfo,
                ipAddress: ipAddress
            );

            // 检查是否是新设备
            if (device.UsageCount == 1)
            {
                _logger.LogInformation("New device detected for user {UserId}: {DeviceName}", user.Id, device.DeviceName);
                
                // 发送新设备登录通知
                var finalIpAddress = ipAddress;
                var finalUser = user;
                var finalDevice = device;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var location = "Unknown";
                        await notificationService.SendNewDeviceLoginNotificationAsync(
                            finalUser,
                            finalDevice,
                            finalIpAddress ?? "Unknown",
                            location
                        );
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogError(notifyEx, "Failed to send new device notification");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record device for user {UserId}", user.Id);
            // 不阻止登录流程
        }

        await auditLogService.LogAsync(
            action: "2FA Login Successful",
            category: "Authentication",
            userId: user.Id,
            success: true
        );

        return Ok(new TwoFactorLoginResponse(
            Success: true,
            Message: "Login successful"
        ));
    }

    /// <summary>
    /// 登出
    /// </summary>

    /// <summary>
    /// 忘记密码 - 发送重置邮件
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        
        // 安全考虑：即使用户不存在也返回成功，防止用户枚举攻击
        if (user == null)
        {
            await auditLogService.LogAsync(
                action: "Forgot Password - User Not Found",
                category: "Security",
                success: true,
                errorMessage: $"Email not found: {request.Email}"
            );
            
            return Ok(new ForgotPasswordResponse(
                Success: true,
                Message: "If the email exists, a password reset link has been sent."
            ));
        }

        // 生成密码重置token
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        // 构建重置URL
        var resetUrl = configuration["App:Url"] ?? "http://localhost:5101";
        var resetLink = $"{resetUrl}/reset-password";

        // 发送邮件
        await emailService.SendPasswordResetEmailAsync(user.Email!, token, resetLink);

        await auditLogService.LogAsync(
            action: "Password Reset Requested",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(new ForgotPasswordResponse(
            Success: true,
            Message: "If the email exists, a password reset link has been sent."
        ));
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            await auditLogService.LogAsync(
                action: "Password Reset Failed",
                category: "Security",
                success: false,
                errorMessage: "Invalid email"
            );
            
            return BadRequest(new ResetPasswordResponse(
                Success: false,
                Message: "Invalid password reset request"
            ));
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            
            await auditLogService.LogAsync(
                action: "Password Reset Failed",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: errors
            );
            
            return BadRequest(new ResetPasswordResponse(
                Success: false,
                Message: errors
            ));
        }

        await auditLogService.LogAsync(
            action: "Password Reset Successful",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(new ResetPasswordResponse(
            Success: true,
            Message: "Password has been reset successfully"
        ));
    }

    /// <summary>
    /// 修改密码（需要登录）
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            
            await auditLogService.LogAsync(
                action: "Password Change Failed",
                category: "Security",
                userId: user.Id,
                success: false,
                errorMessage: errors
            );
            
            return BadRequest(new ChangePasswordResponse(
                Success: false,
                Message: errors
            ));
        }

        await auditLogService.LogAsync(
            action: "Password Changed",
            category: "Security",
            userId: user.Id,
            success: true
        );

        return Ok(new ChangePasswordResponse(
            Success: true,
            Message: "Password changed successfully"
        ));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            userId = user.Id,
            userName = user.UserName,
            email = user.Email,
            displayName = user.DisplayName,
            emailConfirmed = user.EmailConfirmed
        });
    }

    /// <summary>
    /// 确认邮箱
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ConfirmEmailResponse>> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // 为了安全，不要透露用户是否存在
            return Ok(new ConfirmEmailResponse(
                Success: false,
                Message: "Invalid confirmation link"
            ));
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        
        if (!result.Succeeded)
        {
            await auditLogService.LogAsync(
                action: "Email Confirmation Failed",
                category: "User",
                userId: user.Id,
                success: false,
                errorMessage: string.Join(", ", result.Errors.Select(e => e.Description))
            );
            
            return BadRequest(new ConfirmEmailResponse(
                Success: false,
                Message: "Email confirmation failed. The link may be expired or invalid."
            ));
        }

        // 发送欢迎邮件
        try
        {
            await emailService.SendWelcomeEmailAsync(user.Email!, user.DisplayName ?? user.UserName!);
        }
        catch
        {
            // 欢迎邮件发送失败不影响确认流程
        }

        await auditLogService.LogAsync(
            action: "Email Confirmed",
            category: "User",
            userId: user.Id,
            success: true
        );

        return Ok(new ConfirmEmailResponse(
            Success: true,
            Message: "Email confirmed successfully! You can now login."
        ));
    }

    /// <summary>
    /// 重新发送邮箱确认邮件
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<ActionResult<ResendConfirmationResponse>> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        
        // 为了安全，始终返回成功消息，不透露用户是否存在
        if (user == null)
        {
            return Ok(new ResendConfirmationResponse(
                Success: true,
                Message: "If the email exists and is not confirmed, a confirmation email will be sent."
            ));
        }

        if (user.EmailConfirmed)
        {
            return Ok(new ResendConfirmationResponse(
                Success: true,
                Message: "Email is already confirmed."
            ));
        }

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = $"{configuration["App:BaseUrl"]}/confirm-email";
        
        try
        {
            await emailService.SendEmailConfirmationAsync(user.Email!, confirmationToken, confirmationUrl);
            
            await auditLogService.LogAsync(
                action: "Confirmation Email Resent",
                category: "User",
                userId: user.Id,
                success: true
            );
        }
        catch (Exception ex)
        {
            await auditLogService.LogAsync(
                action: "Confirmation Email Resend Failed",
                category: "User",
                userId: user.Id,
                success: false,
                errorMessage: ex.Message
            );
        }

        return Ok(new ResendConfirmationResponse(
            Success: true,
            Message: "If the email exists and is not confirmed, a confirmation email will be sent."
        ));
    }

    /// <summary>
    /// 解析 UserAgent 获取设备信息
    /// </summary>
    private DeviceInfo ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return new DeviceInfo();
        }

        var parser = UAParser.Parser.GetDefault();
        var clientInfo = parser.Parse(userAgent);

        return new DeviceInfo
        {
            Browser = clientInfo.UA.Family,
            BrowserVersion = $"{clientInfo.UA.Major}.{clientInfo.UA.Minor}",
            OperatingSystem = clientInfo.OS.Family,
            OsVersion = $"{clientInfo.OS.Major}.{clientInfo.OS.Minor}",
            DeviceType = clientInfo.Device.Family == "Other" ? "Desktop" : clientInfo.Device.Family,
            Platform = clientInfo.OS.Family
        };
    }

    /// <summary>
    /// 生成简单的设备指纹
    /// </summary>
    private string GenerateSimpleFingerprint(string? userAgent, string? ipAddress)
    {
        var data = $"{userAgent}|{ipAddress}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash)[..32]; // 取前32个字符
    }
}
