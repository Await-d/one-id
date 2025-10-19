using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Identity.Extensions;

/// <summary>
/// Identity 服务扩展方法
/// </summary>
public static class IdentityServiceExtensions
{
    /// <summary>
    /// 从数据库配置 Identity 选项
    /// </summary>
    public static async Task<IdentityOptions> ConfigureFromDatabaseAsync(
        this IdentityOptions options,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 检查数据库连接
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("Database not available, using default Identity configuration");
                ApplyDefaults(options);
                return options;
            }

            // 读取系统设置
            var settings = await dbContext.SystemSettings
                .Where(s => s.Group == SystemSettingGroups.Password || 
                           s.Group == SystemSettingGroups.Security)
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            if (settings.Count == 0)
            {
                logger.LogInformation("No Identity settings found in database, using defaults");
                ApplyDefaults(options);
                return options;
            }

            // 配置密码策略
            ConfigurePasswordPolicy(options, settings, logger);

            // 配置登录策略
            ConfigureSignInPolicy(options, settings, logger);

            return options;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Identity configuration from database, using defaults");
            ApplyDefaults(options);
            return options;
        }
    }

    private static void ConfigurePasswordPolicy(
        IdentityOptions options,
        Dictionary<string, string> settings,
        ILogger logger)
    {
        options.Password.RequiredLength = GetIntValue(
            settings, 
            SystemSettingKeys.PasswordRequiredLength, 
            8);

        options.Password.RequireDigit = GetBoolValue(
            settings, 
            SystemSettingKeys.PasswordRequireDigit, 
            true);

        options.Password.RequireUppercase = GetBoolValue(
            settings, 
            SystemSettingKeys.PasswordRequireUppercase, 
            false);

        options.Password.RequireLowercase = GetBoolValue(
            settings, 
            SystemSettingKeys.PasswordRequireLowercase, 
            true);

        options.Password.RequireNonAlphanumeric = GetBoolValue(
            settings, 
            SystemSettingKeys.PasswordRequireNonAlphanumeric, 
            false);

        logger.LogInformation(
            "Password policy configured: MinLength={MinLength}, Digit={Digit}, Upper={Upper}, Lower={Lower}, Special={Special}",
            options.Password.RequiredLength,
            options.Password.RequireDigit,
            options.Password.RequireUppercase,
            options.Password.RequireLowercase,
            options.Password.RequireNonAlphanumeric);
    }

    private static void ConfigureSignInPolicy(
        IdentityOptions options,
        Dictionary<string, string> settings,
        ILogger logger)
    {
        var requireEmailConfirmed = GetBoolValue(
            settings, 
            SystemSettingKeys.LoginRequireEmailConfirmed, 
            true);

        options.SignIn.RequireConfirmedEmail = requireEmailConfirmed;
        options.SignIn.RequireConfirmedAccount = false;

        var maxFailedAttempts = GetIntValue(
            settings, 
            SystemSettingKeys.LoginMaxFailedAttempts, 
            5);

        var lockoutDuration = GetIntValue(
            settings, 
            SystemSettingKeys.LoginLockoutDuration, 
            15);

        options.Lockout.MaxFailedAccessAttempts = maxFailedAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutDuration);
        options.Lockout.AllowedForNewUsers = true;

        logger.LogInformation(
            "Sign-in policy configured: RequireEmail={RequireEmail}, MaxAttempts={MaxAttempts}, Lockout={Lockout}min",
            requireEmailConfirmed,
            maxFailedAttempts,
            lockoutDuration);
    }

    private static void ApplyDefaults(IdentityOptions options)
    {
        // 用户配置
        options.User.RequireUniqueEmail = true;

        // 密码策略（默认值）
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // 登录策略（默认值）
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedAccount = false;

        // 锁定策略（默认值）
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    }

    private static int GetIntValue(Dictionary<string, string> settings, string key, int defaultValue)
    {
        if (settings.TryGetValue(key, out var value) && int.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    private static bool GetBoolValue(Dictionary<string, string> settings, string key, bool defaultValue)
    {
        if (settings.TryGetValue(key, out var value) && bool.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }
}

