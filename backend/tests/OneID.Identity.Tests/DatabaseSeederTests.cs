using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OneID.Identity.Configuration;
using OneID.Identity.Seed;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Identity.Tests;

public class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesAdminUserAndClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        services.Configure<SeedOptions>(options =>
        {
            options.Admin.Email = "owner@oneid.test";
            options.Admin.UserName = "owner";
            options.Admin.Password = "Passw0rd!";
            options.Admin.Role = "PlatformAdmin";
            options.Oidc.ClientId = "test.client";
            options.Oidc.DisplayName = "Test Client";
            options.Oidc.RedirectUri = "https://app.example.com/callback";
            options.Oidc.PostLogoutRedirectUri = "https://app.example.com";
        });

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connection);
            options.UseOpenIddict();
        });

        services.AddDataProtection();

        services.AddIdentityCore<AppUser>()
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AppDbContext>();
            });

        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        await using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        var seeder = provider.GetRequiredService<IDatabaseSeeder>();

        // Act
        await seeder.SeedAsync();

        // Assert
        using var assertionScope = provider.CreateScope();
        var userManager = assertionScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = assertionScope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var appManager = assertionScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var admin = await userManager.FindByEmailAsync("owner@oneid.test");
        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin!, "PlatformAdmin"));

        var role = await roleManager.FindByNameAsync("PlatformAdmin");
        Assert.NotNull(role);

        var application = await appManager.FindByClientIdAsync("test.client");
        Assert.NotNull(application);
    }

    [Fact]
    public async Task SeedAsync_CreatesExternalAuthProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        services.Configure<SeedOptions>(options =>
        {
            options.ExternalAuth = new ExternalAuthSeedOptions
            {
                Providers = new[]
                {
                    new ExternalAuthProviderSeedOptions
                    {
                        Name = "GitHub",
                        DisplayName = "GitHub",
                        Enabled = true,
                        ClientId = "gh-client",
                        ClientSecret = "gh-secret",
                        Scopes = new[]{"user:email"},
                        DisplayOrder = 1
                    },
                    new ExternalAuthProviderSeedOptions
                    {
                        Name = "WeChat",
                        DisplayName = "WeChat",
                        Enabled = false,
                        ClientId = "wx-client",
                        ClientSecret = "wx-secret",
                        CallbackPath = "/signin-wechat-custom",
                        AdditionalConfig = new Dictionary<string, string>
                        {
                            ["region"] = "China",
                            ["agentId"] = "123"
                        }
                    }
                }
            };
        });

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connection);
            options.UseOpenIddict();
        });

        services.AddDataProtection();

        services.AddIdentityCore<AppUser>()
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AppDbContext>();
            });

        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        await using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        var seeder = provider.GetRequiredService<IDatabaseSeeder>();

        // Act
        await seeder.SeedAsync();

        // Assert
        using var assertionScope = provider.CreateScope();
        var context = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var protector = assertionScope.ServiceProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("ExternalAuthProvider.ClientSecret");

        var providers = await context.Set<ExternalAuthProvider>().ToListAsync();
        Assert.Equal(2, providers.Count);

        var github = providers.Single(p => p.Name == "GitHub");
        Assert.True(github.Enabled);
        Assert.Equal("gh-client", github.ClientId);
        Assert.Equal("/signin-github", github.CallbackPath);
        Assert.Equal("[\"user:email\"]", github.Scopes);
        Assert.Equal("gh-secret", protector.Unprotect(github.ClientSecret));

        var wechat = providers.Single(p => p.Name == "WeChat");
        Assert.False(wechat.Enabled);
        Assert.Equal("/signin-wechat-custom", wechat.CallbackPath);
        Assert.Equal("wx-client", wechat.ClientId);
        Assert.Contains("region", wechat.AdditionalConfig);
        Assert.Equal("wx-secret", protector.Unprotect(wechat.ClientSecret));
    }
}
