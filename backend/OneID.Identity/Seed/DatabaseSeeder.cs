using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OneID.Identity.Configuration;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Identity.Seed;

public sealed class DatabaseSeeder(
    AppDbContext dbContext,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IOptions<SeedOptions> options,
    ILogger<DatabaseSeeder> logger,
    IDataProtectionProvider dataProtectionProvider) : IDatabaseSeeder
{
    private readonly SeedOptions _options = options.Value;
    private readonly IDataProtector _externalAuthProtector = dataProtectionProvider.CreateProtector("ExternalAuthProvider.ClientSecret");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 检查数据库连接（带重试逻辑）
        var maxRetries = 10;
        var retryCount = 0;
        var canConnect = false;
        
        while (!canConnect && retryCount < maxRetries)
        {
            try
            {
                canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    retryCount++;
                    logger.LogWarning("Cannot connect to database, retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "Database connection failed, retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
        
        if (!canConnect)
        {
            throw new InvalidOperationException($"Cannot connect to database after {maxRetries} retries");
        }

        logger.LogInformation("Database connection established successfully");

        // Migrations are applied in ServiceProviderExtensions before calling SeedAsync
        // No need to call MigrateAsync again here

        await EnsureAdminRoleAsync(cancellationToken);
        await EnsureAdminUserAsync(cancellationToken);
        await EnsureOidcClientAsync(cancellationToken);
        await EnsureAdminPortalClientAsync(cancellationToken);
        await EnsureOidcScopeAsync(cancellationToken);
        await EnsureExternalAuthProvidersAsync(cancellationToken);
    }

    private async Task EnsureAdminRoleAsync(CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(_options.Admin.Role))
        {
            return;
        }

        var role = new AppRole
        {
            Name = _options.Admin.Role,
            NormalizedName = _options.Admin.Role.ToUpperInvariant(),
            Description = "Platform administrator"
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create role {_options.Admin.Role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Role {Role} ensured", _options.Admin.Role);
    }

    private async Task<AppUser> EnsureAdminUserAsync(CancellationToken cancellationToken)
    {
        var existing = await userManager.Users.FirstOrDefaultAsync(u => u.Email == _options.Admin.Email, cancellationToken);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, _options.Admin.Role))
            {
                await userManager.AddToRoleAsync(existing, _options.Admin.Role);
            }

            return existing;
        }

        var user = new AppUser
        {
            UserName = _options.Admin.UserName,
            Email = _options.Admin.Email,
            EmailConfirmed = true,
            DisplayName = _options.Admin.DisplayName
        };

        var createResult = await userManager.CreateAsync(user, _options.Admin.Password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, _options.Admin.Role);

        logger.LogInformation("Admin user {Email} ensured", user.Email);
        return user;
    }

    private async Task EnsureOidcClientAsync(CancellationToken cancellationToken)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = _options.Oidc.ClientId,
            DisplayName = _options.Oidc.DisplayName,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit
        };

        descriptor.RedirectUris.Add(new Uri(_options.Oidc.RedirectUri));
        descriptor.PostLogoutRedirectUris.Add(new Uri(_options.Oidc.PostLogoutRedirectUri));

        foreach (var scope in _options.Oidc.Scopes)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);

        var existing = await applicationManager.FindByClientIdAsync(_options.Oidc.ClientId, cancellationToken);
        if (existing is not null)
        {
            await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);
            return;
        }

        await applicationManager.CreateAsync(descriptor, cancellationToken);
        logger.LogInformation("OIDC client {ClientId} ensured", _options.Oidc.ClientId);
    }

    private async Task EnsureAdminPortalClientAsync(CancellationToken cancellationToken)
    {
        const string clientId = "spa.admin";
        const string displayName = "OneID Admin Portal";
        
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = displayName,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit
        };

        // Redirect URIs - 从环境变量读取，支持多个地址用逗号分隔
        // 默认包含开发环境地址
        var redirectUris = new List<string>
        {
            "http://localhost:5173/callback",
            "http://localhost:5102/callback"
        };
        
        // 从环境变量读取额外的redirect_uri（支持多个，用逗号分隔）
        // 例如：ADMIN_REDIRECT_URIS=https://sso.company.com:8443/callback,https://192.168.1.100:9444/callback
        var envRedirectUris = Environment.GetEnvironmentVariable("ADMIN_REDIRECT_URIS");
        if (!string.IsNullOrWhiteSpace(envRedirectUris))
        {
            var uris = envRedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            redirectUris.AddRange(uris);
        }
        
        foreach (var uri in redirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        // Post Logout Redirect URIs - 从环境变量读取
        var postLogoutUris = new List<string>
        {
            "http://localhost:5173",
            "http://localhost:5173/logout-callback",  // 登出回调页面
            "http://localhost:5102",
            "http://localhost:5102/logout-callback"   // 登出回调页面
        };
        
        // 例如：ADMIN_LOGOUT_URIS=https://sso.company.com:8443,https://192.168.1.100:9444
        var envPostLogoutUris = Environment.GetEnvironmentVariable("ADMIN_LOGOUT_URIS");
        if (!string.IsNullOrWhiteSpace(envPostLogoutUris))
        {
            var uris = envPostLogoutUris.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var uri in uris)
            {
                postLogoutUris.Add(uri);
                // 同时添加带 /logout-callback 后缀的URI
                if (!uri.EndsWith("/logout-callback"))
                {
                    postLogoutUris.Add(uri.TrimEnd('/') + "/logout-callback");
                }
            }
        }
        
        foreach (var uri in postLogoutUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        // Scopes - 包含admin_api scope用于Admin API访问
        var scopes = new[] { "openid", "profile", "email", "offline_access", "admin_api" };
        foreach (var scope in scopes)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        // Endpoints
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);

        // Grant Types
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

        // Response Types
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is not null)
        {
            await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);
            return;
        }

        await applicationManager.CreateAsync(descriptor, cancellationToken);
        logger.LogInformation("OIDC client {ClientId} ensured for Admin Portal", clientId);
    }

    private async Task EnsureOidcScopeAsync(CancellationToken cancellationToken)
    {
        // 确保admin_api scope存在
        var allScopes = _options.Oidc.Scopes.Concat(new[] { "admin_api" }).Distinct();
        
        foreach (var scope in allScopes)
        {
            var existing = await scopeManager.FindByNameAsync(scope, cancellationToken);
            if (existing is null)
            {
                var descriptor = new OpenIddictScopeDescriptor
                {
                    Name = scope,
                    DisplayName = scope,
                    Resources = { scope == "admin_api" ? "oneid-adminapi" : scope }
                };

                await scopeManager.CreateAsync(descriptor, cancellationToken);
                logger.LogInformation("OIDC scope {Scope} ensured", scope);
            }
        }
    }

    private async Task EnsureExternalAuthProvidersAsync(CancellationToken cancellationToken)
    {
        if (_options.ExternalAuth?.Providers is not { Length: > 0 })
        {
            return;
        }

        var now = DateTime.UtcNow;
        var set = dbContext.Set<ExternalAuthProvider>();

        foreach (var provider in _options.ExternalAuth.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Name))
            {
                continue;
            }

            var name = provider.Name.Trim();
            var displayName = string.IsNullOrWhiteSpace(provider.DisplayName) ? name : provider.DisplayName.Trim();
            var callbackPath = string.IsNullOrWhiteSpace(provider.CallbackPath)
                ? $"/signin-{name.ToLowerInvariant()}"
                : provider.CallbackPath;

            var scopesJson = JsonSerializer.Serialize(provider.Scopes ?? Array.Empty<string>());
            var additionalJson = provider.AdditionalConfig is { Count: > 0 }
                ? JsonSerializer.Serialize(provider.AdditionalConfig)
                : null;

            var existing = await set.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

            if (existing is null)
            {
                if (string.IsNullOrWhiteSpace(provider.ClientId) || string.IsNullOrWhiteSpace(provider.ClientSecret))
                {
                    logger.LogDebug("Skip seeding external auth provider {Name} because credentials are missing", name);
                    continue;
                }

                var entity = new ExternalAuthProvider
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    DisplayName = displayName,
                    Enabled = provider.Enabled,
                    ClientId = provider.ClientId.Trim(),
                    ClientSecret = _externalAuthProtector.Protect(provider.ClientSecret.Trim()),
                    CallbackPath = callbackPath,
                    Scopes = scopesJson,
                    AdditionalConfig = additionalJson,
                    DisplayOrder = provider.DisplayOrder,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                set.Add(entity);
                logger.LogInformation("Seeded external auth provider {Name}", name);
            }
            else
            {
                var updated = false;

                if (!string.IsNullOrWhiteSpace(provider.DisplayName) && existing.DisplayName != displayName)
                {
                    existing.DisplayName = displayName;
                    updated = true;
                }

                if (!string.IsNullOrWhiteSpace(provider.ClientId) && existing.ClientId != provider.ClientId.Trim())
                {
                    existing.ClientId = provider.ClientId.Trim();
                    updated = true;
                }

                if (!string.IsNullOrWhiteSpace(provider.ClientSecret))
                {
                    var protectedSecret = _externalAuthProtector.Protect(provider.ClientSecret.Trim());
                    if (existing.ClientSecret != protectedSecret)
                    {
                        existing.ClientSecret = protectedSecret;
                        updated = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(provider.CallbackPath) && existing.CallbackPath != callbackPath)
                {
                    existing.CallbackPath = callbackPath;
                    updated = true;
                }

                if (existing.Enabled != provider.Enabled)
                {
                    existing.Enabled = provider.Enabled;
                    updated = true;
                }

                if (existing.DisplayOrder != provider.DisplayOrder)
                {
                    existing.DisplayOrder = provider.DisplayOrder;
                    updated = true;
                }

                if (existing.Scopes != scopesJson)
                {
                    existing.Scopes = scopesJson;
                    updated = true;
                }

                if (existing.AdditionalConfig != additionalJson)
                {
                    existing.AdditionalConfig = additionalJson;
                    updated = true;
                }

                if (updated)
                {
                    existing.UpdatedAt = now;
                    logger.LogInformation("Updated external auth provider {Name} from seed", name);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
