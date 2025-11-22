using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using System.Text.Json;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 系统设置服务实现
/// </summary>
public sealed class SystemSettingsService : ISystemSettingsService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SystemSettingsService> _logger;
    
    public SystemSettingsService(
        AppDbContext dbContext,
        ILogger<SystemSettingsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    #region 基本 CRUD 操作

    public async Task<IReadOnlyList<SystemSetting>> GetAllSettingsAsync(string? group = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SystemSettings.AsQueryable();

        if (!string.IsNullOrEmpty(group))
        {
            query = query.Where(s => s.Group == group);
        }

        return await query
            .OrderBy(s => s.Group)
            .ThenBy(s => s.SortOrder)
            .ThenBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemSetting?> GetSettingByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<SystemSetting?> GetSettingByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task<SystemSetting> CreateSettingAsync(SystemSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.LastModifiedBy = modifiedBy;

        _dbContext.SystemSettings.Add(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "System setting created: {Key} = {Value} by {User}",
            setting.Key, setting.Value, modifiedBy ?? "System");

        return setting;
    }

    public async Task<SystemSetting> UpdateSettingAsync(SystemSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        var existing = await GetSettingByIdAsync(setting.Id, cancellationToken)
            ?? throw new InvalidOperationException($"System setting with ID {setting.Id} not found");

        if (existing.IsReadOnly)
        {
            throw new InvalidOperationException($"System setting '{existing.Key}' is read-only and cannot be modified");
        }

        existing.Value = setting.Value;
        existing.DisplayName = setting.DisplayName;
        existing.Description = setting.Description;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.LastModifiedBy = modifiedBy;
        existing.IsModified = true; // 标记为已修改，防止 Seed 覆盖

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "System setting updated: {Key} = {Value} by {User}",
            existing.Key, existing.Value, modifiedBy ?? "System");

        return existing;
    }

    public async Task DeleteSettingAsync(int id, CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"System setting with ID {id} not found");

        if (setting.IsReadOnly)
        {
            throw new InvalidOperationException($"System setting '{setting.Key}' is read-only and cannot be deleted");
        }

        _dbContext.SystemSettings.Remove(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("System setting deleted: {Key}", setting.Key);
    }

    #endregion

    #region 便捷方法 - 获取值

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingByKeyAsync(key, cancellationToken);
        return setting?.Value;
    }

    public async Task<string> GetValueOrDefaultAsync(string key, string defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(key, cancellationToken);
        return value ?? defaultValue;
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue = 0, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(key, cancellationToken);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolValueAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(key, cancellationToken);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0m, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(key, cancellationToken);
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<T?> GetJsonValueAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = await GetValueAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON value for setting {Key}", key);
            return null;
        }
    }

    #endregion

    #region 便捷方法 - 设置值

    public async Task SetValueAsync(string key, string value, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingByKeyAsync(key, cancellationToken);
        
        if (setting == null)
        {
            throw new InvalidOperationException($"System setting '{key}' not found. Create it first before setting value.");
        }

        if (setting.IsReadOnly)
        {
            throw new InvalidOperationException($"System setting '{key}' is read-only and cannot be modified");
        }

        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.LastModifiedBy = modifiedBy;
        setting.IsModified = true; // 标记为已修改，防止 Seed 覆盖

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "System setting value updated: {Key} = {Value} by {User}",
            key, value, modifiedBy ?? "System");
    }

    public async Task SetIntValueAsync(string key, int value, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        await SetValueAsync(key, value.ToString(), modifiedBy, cancellationToken);
    }

    public async Task SetBoolValueAsync(string key, bool value, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        await SetValueAsync(key, value.ToString(), modifiedBy, cancellationToken);
    }

    public async Task SetDecimalValueAsync(string key, decimal value, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        await SetValueAsync(key, value.ToString(), modifiedBy, cancellationToken);
    }

    public async Task SetJsonValueAsync<T>(string key, T value, string? modifiedBy = null, CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await SetValueAsync(key, json, modifiedBy, cancellationToken);
    }

    #endregion

    #region 批量操作

    public async Task<Dictionary<string, string>> GetSettingsByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.SystemSettings
            .Where(s => s.Group == group)
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return settings;
    }

    public async Task ResetToDefaultAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingByKeyAsync(key, cancellationToken)
            ?? throw new InvalidOperationException($"System setting '{key}' not found");

        if (setting.IsReadOnly)
        {
            throw new InvalidOperationException($"System setting '{key}' is read-only and cannot be reset");
        }

        if (string.IsNullOrEmpty(setting.DefaultValue))
        {
            _logger.LogWarning("System setting '{Key}' has no default value defined", key);
            return;
        }

        setting.Value = setting.DefaultValue;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.LastModifiedBy = "System";
        setting.IsModified = false; // 重置为默认值，允许 Seed 更新

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("System setting reset to default: {Key} = {Value}", key, setting.Value);
    }

    public async Task ResetGroupToDefaultAsync(string group, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.SystemSettings
            .Where(s => s.Group == group && !s.IsReadOnly && s.DefaultValue != null)
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            setting.Value = setting.DefaultValue!;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.LastModifiedBy = "System";
            setting.IsModified = false; // 重置为默认值，允许 Seed 更新
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("System settings group reset to default: {Group}, {Count} settings", group, settings.Count);
    }

    #endregion

    #region 初始化默认设置

    public async Task EnsureDefaultSettingsAsync(CancellationToken cancellationToken = default)
    {
        var defaultSettings = GetDefaultSettings();

        foreach (var setting in defaultSettings)
        {
            var existing = await GetSettingByKeyAsync(setting.Key, cancellationToken);
            if (existing == null)
            {
                await CreateSettingAsync(setting, "System", cancellationToken);
            }
        }

        _logger.LogInformation("Default system settings ensured, {Count} settings", defaultSettings.Count);
    }

    public static List<SystemSetting> GetDefaultSettings()
    {
        return new List<SystemSetting>
        {
            // 密码策略
            new SystemSetting
            {
                Key = SystemSettingKeys.PasswordRequiredLength,
                Value = "8",
                DefaultValue = "8",
                Group = SystemSettingGroups.Password,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Minimum Password Length",
                Description = "The minimum required password length",
                SortOrder = 1
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.PasswordRequireDigit,
                Value = "True",
                DefaultValue = "True",
                Group = SystemSettingGroups.Password,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Require Digit",
                Description = "Whether passwords must contain at least one digit (0-9)",
                SortOrder = 2
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.PasswordRequireUppercase,
                Value = "False",
                DefaultValue = "False",
                Group = SystemSettingGroups.Password,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Require Uppercase",
                Description = "Whether passwords must contain at least one uppercase letter (A-Z)",
                SortOrder = 3
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.PasswordRequireLowercase,
                Value = "True",
                DefaultValue = "True",
                Group = SystemSettingGroups.Password,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Require Lowercase",
                Description = "Whether passwords must contain at least one lowercase letter (a-z)",
                SortOrder = 4
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.PasswordRequireNonAlphanumeric,
                Value = "False",
                DefaultValue = "False",
                Group = SystemSettingGroups.Password,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Require Special Character",
                Description = "Whether passwords must contain at least one special character (!@#$%^&*)",
                SortOrder = 5
            },

            // 登录策略
            new SystemSetting
            {
                Key = SystemSettingKeys.LoginMaxFailedAttempts,
                Value = "5",
                DefaultValue = "5",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Max Failed Login Attempts",
                Description = "Maximum number of failed login attempts before account lockout",
                SortOrder = 1
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.LoginLockoutDuration,
                Value = "15",
                DefaultValue = "15",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Lockout Duration (minutes)",
                Description = "Duration of account lockout after max failed attempts",
                SortOrder = 2
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.LoginRequireEmailConfirmed,
                Value = "True",
                DefaultValue = "True",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Require Email Confirmation",
                Description = "Whether users must confirm their email before logging in",
                SortOrder = 3
            },

            // Token 生命周期
            new SystemSetting
            {
                Key = SystemSettingKeys.TokenAccessTokenLifetime,
                Value = "3600",
                DefaultValue = "3600",
                Group = SystemSettingGroups.TokenLifetime,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Access Token Lifetime (seconds)",
                Description = "Lifetime of access tokens in seconds (default: 1 hour)",
                SortOrder = 1
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.TokenRefreshTokenLifetime,
                Value = "2592000",
                DefaultValue = "2592000",
                Group = SystemSettingGroups.TokenLifetime,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Refresh Token Lifetime (seconds)",
                Description = "Lifetime of refresh tokens in seconds (default: 30 days)",
                SortOrder = 2
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.TokenIdTokenLifetime,
                Value = "3600",
                DefaultValue = "3600",
                Group = SystemSettingGroups.TokenLifetime,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "ID Token Lifetime (seconds)",
                Description = "Lifetime of ID tokens in seconds (default: 1 hour)",
                SortOrder = 3
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.TokenAuthorizationCodeLifetime,
                Value = "300",
                DefaultValue = "300",
                Group = SystemSettingGroups.TokenLifetime,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Authorization Code Lifetime (seconds)",
                Description = "Lifetime of authorization codes in seconds (default: 5 minutes)",
                SortOrder = 4
            },

            // 会话设置
            new SystemSetting
            {
                Key = SystemSettingKeys.SessionIdleTimeout,
                Value = "1800",
                DefaultValue = "1800",
                Group = SystemSettingGroups.Session,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Session Idle Timeout (seconds)",
                Description = "Maximum idle time before session expires (default: 30 minutes)",
                SortOrder = 1
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.SessionAbsoluteTimeout,
                Value = "43200",
                DefaultValue = "43200",
                Group = SystemSettingGroups.Session,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Session Absolute Timeout (seconds)",
                Description = "Maximum session lifetime regardless of activity (default: 12 hours)",
                SortOrder = 2
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.SessionSlidingExpiration,
                Value = "True",
                DefaultValue = "True",
                Group = SystemSettingGroups.Session,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Enable Sliding Expiration",
                Description = "Whether session expiration resets on activity",
                SortOrder = 3
            },

            // 注册设置
            new SystemSetting
            {
                Key = SystemSettingKeys.RegistrationEnabled,
                Value = "True",
                DefaultValue = "True",
                Group = SystemSettingGroups.Registration,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Enable Registration",
                Description = "Whether public user registration is enabled",
                SortOrder = 1
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.RegistrationRequireEmailConfirmation,
                Value = "True",
                DefaultValue = "True",
                Group = SystemSettingGroups.Registration,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Require Email Confirmation",
                Description = "Whether new users must confirm email during registration",
                SortOrder = 2
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.RegistrationDefaultRole,
                Value = "User",
                DefaultValue = "User",
                Group = SystemSettingGroups.Registration,
                ValueType = SystemSettingValueTypes.String,
                DisplayName = "Default Role",
                Description = "Default role assigned to newly registered users",
                SortOrder = 3
            },

            // MFA 设置
            new SystemSetting
            {
                Key = SystemSettingKeys.MfaRequired,
                Value = "False",
                DefaultValue = "False",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Require MFA",
                Description = "Whether multi-factor authentication is required for all users",
                SortOrder = 10
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.MfaGracePeriod,
                Value = "7",
                DefaultValue = "7",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "MFA Grace Period (days)",
                Description = "Number of days users have to enable MFA if required",
                SortOrder = 11
            },

            // 安全设置
            new SystemSetting
            {
                Key = SystemSettingKeys.SecurityEnableIpWhitelist,
                Value = "False",
                DefaultValue = "False",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Enable IP Whitelist",
                Description = "Whether IP whitelisting is enabled for security rules",
                SortOrder = 20
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.SecurityRateLimitEnabled,
                Value = "True",
                DefaultValue = "True",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Boolean,
                DisplayName = "Enable Rate Limiting",
                Description = "Whether API rate limiting is enabled",
                SortOrder = 21
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.SecurityRateLimitRequestsPerMinute,
                Value = "60",
                DefaultValue = "60",
                Group = SystemSettingGroups.Security,
                ValueType = SystemSettingValueTypes.Integer,
                DisplayName = "Rate Limit (requests/minute)",
                Description = "Maximum number of requests per minute per IP",
                SortOrder = 22
            }
        };
    }

    #endregion
}

