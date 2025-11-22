using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneID.Shared.Domain;

/// <summary>
/// 系统设置实体
/// </summary>
[Table("SystemSettings")]
public class SystemSetting
{
    /// <summary>
    /// 设置ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 设置键（唯一标识符）
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 设置值
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 设置分组（用于界面组织）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 值类型（String, Integer, Boolean, JSON）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ValueType { get; set; } = "String";

    /// <summary>
    /// 显示名称
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 描述说明
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 是否为敏感信息（需要加密存储）
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// 是否只读（不允许通过 UI 修改）
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 最后修改者用户ID
    /// </summary>
    [MaxLength(450)]
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// 是否已被修改（非默认值）
    /// 用于判断是否应该被 Seed 配置更新
    /// </summary>
    public bool IsModified { get; set; } = false;

    /// <summary>
    /// 验证规则（JSON格式）
    /// 例如：{ "min": 1, "max": 100 } 或 { "regex": "^[0-9]+$" }
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 可选值列表（JSON数组格式）
    /// 用于下拉框等场景，例如：["Option1", "Option2", "Option3"]
    /// </summary>
    public string? AllowedValues { get; set; }
}

/// <summary>
/// 系统设置分组常量
/// </summary>
public static class SystemSettingGroups
{
    public const string Authentication = "Authentication";
    public const string Security = "Security";
    public const string Email = "Email";
    public const string TokenLifetime = "TokenLifetime";
    public const string Session = "Session";
    public const string Password = "Password";
    public const string Registration = "Registration";
    public const string General = "General";
}

/// <summary>
/// 系统设置值类型常量
/// </summary>
public static class SystemSettingValueTypes
{
    public const string String = "String";
    public const string Integer = "Integer";
    public const string Boolean = "Boolean";
    public const string JSON = "JSON";
    public const string Decimal = "Decimal";
}

/// <summary>
/// 系统设置键常量（预定义的设置项）
/// </summary>
public static class SystemSettingKeys
{
    // 密码策略
    public const string PasswordRequiredLength = "Password.RequiredLength";
    public const string PasswordRequireDigit = "Password.RequireDigit";
    public const string PasswordRequireUppercase = "Password.RequireUppercase";
    public const string PasswordRequireLowercase = "Password.RequireLowercase";
    public const string PasswordRequireNonAlphanumeric = "Password.RequireNonAlphanumeric";
    
    // 登录策略
    public const string LoginMaxFailedAttempts = "Login.MaxFailedAttempts";
    public const string LoginLockoutDuration = "Login.LockoutDuration";
    public const string LoginRequireEmailConfirmed = "Login.RequireEmailConfirmed";
    
    // Token 生命周期
    public const string TokenAccessTokenLifetime = "Token.AccessTokenLifetime";
    public const string TokenRefreshTokenLifetime = "Token.RefreshTokenLifetime";
    public const string TokenIdTokenLifetime = "Token.IdTokenLifetime";
    public const string TokenAuthorizationCodeLifetime = "Token.AuthorizationCodeLifetime";
    
    // 会话设置
    public const string SessionIdleTimeout = "Session.IdleTimeout";
    public const string SessionAbsoluteTimeout = "Session.AbsoluteTimeout";
    public const string SessionSlidingExpiration = "Session.SlidingExpiration";
    
    // 注册设置
    public const string RegistrationEnabled = "Registration.Enabled";
    public const string RegistrationRequireEmailConfirmation = "Registration.RequireEmailConfirmation";
    public const string RegistrationDefaultRole = "Registration.DefaultRole";
    
    // MFA 设置
    public const string MfaRequired = "MFA.Required";
    public const string MfaGracePeriod = "MFA.GracePeriod";
    
    // 安全设置
    public const string SecurityEnableIpWhitelist = "Security.EnableIpWhitelist";
    public const string SecurityRateLimitEnabled = "Security.RateLimitEnabled";
    public const string SecurityRateLimitRequestsPerMinute = "Security.RateLimitRequestsPerMinute";
}

