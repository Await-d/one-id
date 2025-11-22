using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 通知设置管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NotificationSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _systemSettings;
    private readonly ILogger<NotificationSettingsController> _logger;

    public NotificationSettingsController(
        ISystemSettingsService systemSettings,
        ILogger<NotificationSettingsController> logger)
    {
        _systemSettings = systemSettings;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有通知设置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<NotificationSettingsResponse>> GetSettings()
    {
        try
        {
            var settings = new NotificationSettingsResponse
            {
                AnomalousLogin = new AnomalousLoginSettings
                {
                    Enabled = await _systemSettings.GetBoolValueAsync("notification-anomalous-login-enabled", true),
                    RiskScoreThreshold = await _systemSettings.GetIntValueAsync("notification-anomalous-login-threshold", 40)
                },
                NewDevice = new NewDeviceSettings
                {
                    Enabled = await _systemSettings.GetBoolValueAsync("notification-new-device-enabled", true)
                },
                PasswordChanged = new PasswordChangedSettings
                {
                    Enabled = await _systemSettings.GetBoolValueAsync("notification-password-changed-enabled", true)
                },
                AccountLocked = new AccountLockedSettings
                {
                    Enabled = await _systemSettings.GetBoolValueAsync("notification-account-locked-enabled", true)
                },
                MfaEnabled = new MfaEnabledSettings
                {
                    Enabled = await _systemSettings.GetBoolValueAsync("notification-mfa-enabled-enabled", true)
                }
            };

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification settings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 更新异常登录通知设置
    /// </summary>
    [HttpPut("anomalous-login")]
    public async Task<ActionResult> UpdateAnomalousLoginSettings([FromBody] AnomalousLoginSettings settings)
    {
        try
        {
            await _systemSettings.SetValueAsync("notification-anomalous-login-enabled", settings.Enabled.ToString());
            await _systemSettings.SetValueAsync("notification-anomalous-login-threshold", settings.RiskScoreThreshold.ToString());
            
            _logger.LogInformation("Anomalous login notification settings updated");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update anomalous login notification settings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 更新新设备通知设置
    /// </summary>
    [HttpPut("new-device")]
    public async Task<ActionResult> UpdateNewDeviceSettings([FromBody] NewDeviceSettings settings)
    {
        try
        {
            await _systemSettings.SetValueAsync("notification-new-device-enabled", settings.Enabled.ToString());
            
            _logger.LogInformation("New device notification settings updated");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update new device notification settings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 更新密码修改通知设置
    /// </summary>
    [HttpPut("password-changed")]
    public async Task<ActionResult> UpdatePasswordChangedSettings([FromBody] PasswordChangedSettings settings)
    {
        try
        {
            await _systemSettings.SetValueAsync("notification-password-changed-enabled", settings.Enabled.ToString());
            
            _logger.LogInformation("Password changed notification settings updated");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update password changed notification settings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 更新账户锁定通知设置
    /// </summary>
    [HttpPut("account-locked")]
    public async Task<ActionResult> UpdateAccountLockedSettings([FromBody] AccountLockedSettings settings)
    {
        try
        {
            await _systemSettings.SetValueAsync("notification-account-locked-enabled", settings.Enabled.ToString());
            
            _logger.LogInformation("Account locked notification settings updated");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update account locked notification settings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 更新MFA启用通知设置
    /// </summary>
    [HttpPut("mfa-enabled")]
    public async Task<ActionResult> UpdateMfaEnabledSettings([FromBody] MfaEnabledSettings settings)
    {
        try
        {
            await _systemSettings.SetValueAsync("notification-mfa-enabled-enabled", settings.Enabled.ToString());
            
            _logger.LogInformation("MFA enabled notification settings updated");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update MFA enabled notification settings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 批量更新所有通知设置
    /// </summary>
    [HttpPut]
    public async Task<ActionResult> UpdateAllSettings([FromBody] NotificationSettingsResponse settings)
    {
        try
        {
            // 异常登录
            await _systemSettings.SetValueAsync("notification-anomalous-login-enabled", settings.AnomalousLogin.Enabled.ToString());
            await _systemSettings.SetValueAsync("notification-anomalous-login-threshold", settings.AnomalousLogin.RiskScoreThreshold.ToString());
            
            // 新设备
            await _systemSettings.SetValueAsync("notification-new-device-enabled", settings.NewDevice.Enabled.ToString());
            
            // 密码修改
            await _systemSettings.SetValueAsync("notification-password-changed-enabled", settings.PasswordChanged.Enabled.ToString());
            
            // 账户锁定
            await _systemSettings.SetValueAsync("notification-account-locked-enabled", settings.AccountLocked.Enabled.ToString());
            
            // MFA启用
            await _systemSettings.SetValueAsync("notification-mfa-enabled-enabled", settings.MfaEnabled.Enabled.ToString());
            
            _logger.LogInformation("All notification settings updated");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification settings");
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// 通知设置响应
/// </summary>
public class NotificationSettingsResponse
{
    public AnomalousLoginSettings AnomalousLogin { get; set; } = new();
    public NewDeviceSettings NewDevice { get; set; } = new();
    public PasswordChangedSettings PasswordChanged { get; set; } = new();
    public AccountLockedSettings AccountLocked { get; set; } = new();
    public MfaEnabledSettings MfaEnabled { get; set; } = new();
}

/// <summary>
/// 异常登录通知设置
/// </summary>
public class AnomalousLoginSettings
{
    public bool Enabled { get; set; }
    public int RiskScoreThreshold { get; set; }
}

/// <summary>
/// 新设备通知设置
/// </summary>
public class NewDeviceSettings
{
    public bool Enabled { get; set; }
}

/// <summary>
/// 密码修改通知设置
/// </summary>
public class PasswordChangedSettings
{
    public bool Enabled { get; set; }
}

/// <summary>
/// 账户锁定通知设置
/// </summary>
public class AccountLockedSettings
{
    public bool Enabled { get; set; }
}

/// <summary>
/// MFA启用通知设置
/// </summary>
public class MfaEnabledSettings
{
    public bool Enabled { get; set; }
}

