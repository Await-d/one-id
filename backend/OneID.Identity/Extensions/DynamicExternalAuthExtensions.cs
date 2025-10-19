using System.Text.Json;
using AspNet.Security.OAuth.Apple;
using AspNet.Security.OAuth.LinkedIn;
using AspNet.Security.OAuth.QQ;
using AspNet.Security.OAuth.Weixin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Identity.Extensions;

public static class DynamicExternalAuthExtensions
{
    public static async Task ConfigureDynamicExternalAuthenticationAsync(
        this WebApplication app,
        ILogger logger)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = dataProtectionProvider.CreateProtector("ExternalAuthProvider.ClientSecret");

        // 从数据库加载已启用的外部认证提供者
        var providers = await dbContext.Set<ExternalAuthProvider>()
            .Where(p => p.Enabled)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        if (providers.Count == 0)
        {
            logger.LogInformation("No enabled external authentication providers found in database");
            return;
        }

        var authBuilder = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();

        foreach (var provider in providers)
        {
            try
            {
                // 检查是否已经注册
                var existingScheme = await authBuilder.GetSchemeAsync(provider.Name);
                if (existingScheme != null)
                {
                    logger.LogInformation("External auth provider {Name} already registered", provider.Name);
                    continue;
                }

                var scopes = string.IsNullOrEmpty(provider.Scopes)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(provider.Scopes) ?? new List<string>();

                // 解密ClientSecret
                string clientSecret;
                try
                {
                    clientSecret = protector.Unprotect(provider.ClientSecret);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to decrypt client secret for provider {Name}", provider.Name);
                    continue;
                }

                logger.LogInformation("Configuring external auth provider: {Name} with callback {CallbackPath}",
                    provider.Name, provider.CallbackPath);

                // 这里记录配置信息供后续使用
                // 由于动态添加认证方案比较复杂，建议在启动时从数据库读取并配置
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure external auth provider {Name}", provider.Name);
            }
        }
    }

    public static async Task<AuthenticationBuilder> AddDynamicExternalAuthenticationAsync(
        this AuthenticationBuilder authBuilder,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = dataProtectionProvider.CreateProtector("ExternalAuthProvider.ClientSecret");

        // 从数据库加载已启用的外部认证提供者
        var providers = await dbContext.Set<ExternalAuthProvider>()
            .Where(p => p.Enabled)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        foreach (var provider in providers)
        {
            try
            {
                var scopes = DeserializeScopes(provider.Scopes);
                var additional = DeserializeAdditionalConfig(provider.AdditionalConfig, provider.Name, logger);

                if (!TryUnprotectSecret(protector, provider, logger, out var clientSecret))
                {
                    continue;
                }

                // 使用 Name 作为唯一的 scheme name，ProviderType 用于确定认证类型
                var callbackPath = NormalizeCallbackPath(provider.CallbackPath, provider.Name);
                var schemeName = provider.Name; // 每个提供商实例的唯一标识

                switch (provider.ProviderType.ToLowerInvariant())
                {
                    case "github":
                        authBuilder.AddGitHub(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured GitHub authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "google":
                        authBuilder.AddGoogle(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured Google authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "gitee":
                        authBuilder.AddGitee(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured Gitee authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "wechat":
                    case "weixin":
                        authBuilder.AddWeixin(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured Weixin authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "microsoft":
                        authBuilder.AddMicrosoftAccount(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured Microsoft authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "apple":
                        authBuilder.AddApple(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured Apple authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "facebook":
                        authBuilder.AddFacebook(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured Facebook authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "linkedin":
                        authBuilder.AddLinkedIn(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured LinkedIn authentication '{SchemeName}' from database", schemeName);
                        break;

                    case "qq":
                        authBuilder.AddQQ(schemeName, options =>
                        {
                            options.SaveTokens = true;
                            options.ClientId = provider.ClientId;
                            options.ClientSecret = clientSecret;
                            options.CallbackPath = callbackPath;
                            ApplyScopes(options.Scope, scopes);
                        });
                        logger.LogInformation("Configured QQ authentication '{SchemeName}' from database", schemeName);
                        break;

                    default:
                        logger.LogWarning("Unknown external auth provider type: {ProviderType}", provider.ProviderType);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure external auth provider {Name}", provider.Name);
            }
        }

        return authBuilder;
    }

    private static IReadOnlyList<string> DeserializeScopes(string? scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(scopes) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static Dictionary<string, string> DeserializeAdditionalConfig(
        string? additionalConfig,
        string providerName,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(additionalConfig))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(additionalConfig);
            return data != null
                ? new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialize AdditionalConfig for provider {Name}", providerName);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool TryUnprotectSecret(
        IDataProtector protector,
        ExternalAuthProvider provider,
        ILogger logger,
        out string secret)
    {
        try
        {
            secret = protector.Unprotect(provider.ClientSecret);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decrypt client secret for provider {Name}", provider.Name);
            secret = string.Empty;
            return false;
        }
    }

    private static PathString NormalizeCallbackPath(string? callbackPath, string providerName)
    {
        if (string.IsNullOrWhiteSpace(callbackPath))
        {
            return new PathString($"/signin-{providerName.ToLowerInvariant()}");
        }

        return callbackPath.StartsWith('/') ? new PathString(callbackPath) : new PathString($"/{callbackPath}");
    }

    private static void ApplyScopes(ICollection<string> target, IReadOnlyList<string> scopes)
    {
        foreach (var scope in scopes)
        {
            if (!string.IsNullOrWhiteSpace(scope) && !target.Contains(scope))
            {
                target.Add(scope);
            }
        }
    }
}
