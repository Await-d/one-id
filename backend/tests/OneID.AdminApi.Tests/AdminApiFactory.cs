using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneID.Shared.Data;
using OpenIddict.Abstractions;

namespace OneID.AdminApi.Tests;

public sealed class AdminApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly DbConnection _connection = new SqliteConnection("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        var tempKeysPath = Path.Combine(Path.GetTempPath(), "oneid-test-keys-" + Guid.NewGuid());
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
        builder.UseSetting("DataProtection:KeysPath", tempKeysPath);

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext and DbContextOptions registrations so the Npgsql
            // provider (registered via AddConfiguredDatabase) is fully replaced.
            var dbTypes = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericArguments().Any(a => a == typeof(AppDbContext))))
                .ToList();
            foreach (var d in dbTypes) services.Remove(d);

            services.AddDbContext<AppDbContext>((_, options) =>
            {
                options.UseSqlite(_connection);
                options.UseOpenIddict();
            });

            var authOptions = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<AuthenticationOptions>))
                .ToList();

            foreach (var descriptor in authOptions)
            {
                services.Remove(descriptor);
            }

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.AddAuthorization(options =>
            {
                var policy = new AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
                options.DefaultPolicy = policy;
                options.FallbackPolicy = policy;
            });

            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var existing = applicationManager.FindByClientIdAsync("spa.portal").GetAwaiter().GetResult();

            if (existing is null)
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = "spa.portal",
                    DisplayName = "Portal Client",
                    ClientType = OpenIddictConstants.ClientTypes.Public
                };

                descriptor.RedirectUris.Add(new Uri("https://spa.local/callback"));
                descriptor.PostLogoutRedirectUris.Add(new Uri("https://spa.local"));
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "openid");

                applicationManager.CreateAsync(descriptor).GetAwaiter().GetResult();
            }
        });

        builder.ConfigureTestServices(services =>
        {
            services.Configure<JwtBearerOptions>(options => { });
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Bearer", _ => { });
        });
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
