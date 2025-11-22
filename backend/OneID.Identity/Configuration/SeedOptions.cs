namespace OneID.Identity.Configuration;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// 强制更新所有配置（忽略 IsModified 标记）
    /// 环境变量: Seed__ForceUpdate=true
    /// </summary>
    public bool ForceUpdate { get; init; } = false;

    public AdminSeedOptions Admin { get; init; } = new();
    public OidcSeedOptions Oidc { get; init; } = new();
    public ExternalAuthSeedOptions ExternalAuth { get; init; } = new();
}

public sealed class AdminSeedOptions
{
    public string Email { get; init; } = "admin@oneid.local";
    public string UserName { get; init; } = "admin";
    public string Password { get; init; } = "ChangeMe123!";
    public string DisplayName { get; init; } = "Platform Admin";
    public string Role { get; init; } = "Admin";
}

public sealed class OidcSeedOptions
{
    public string ClientId { get; init; } = "spa.portal";
    public string ClientSecret { get; init; } = "spa-secret";
    public string DisplayName { get; init; } = "OneID Portal";
    public string RedirectUri { get; init; } = "https://localhost:5173/callback";
    public string PostLogoutRedirectUri { get; init; } = "https://localhost:5173";
    public string[] Scopes { get; init; } = new[] { "openid", "profile", "email", "offline_access" };
}

public sealed class ExternalAuthSeedOptions
{
    public ExternalAuthProviderSeedOptions[] Providers { get; init; } = Array.Empty<ExternalAuthProviderSeedOptions>();
}

public sealed class ExternalAuthProviderSeedOptions
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CallbackPath { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public int DisplayOrder { get; init; }
    public Dictionary<string, string>? AdditionalConfig { get; init; }
}
