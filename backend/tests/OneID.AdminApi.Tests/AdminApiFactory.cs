using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

        builder.ConfigureServices(services =>
        {
            // 替换数据库上下文为单例 SQLite 内存库
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) || d.ServiceType == typeof(AppDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
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
                    Type = OpenIddictConstants.ClientTypes.Public
                };

                descriptor.RedirectUris.Add(new Uri("https://spa.local/callback"));
                descriptor.PostLogoutRedirectUris.Add(new Uri("https://spa.local"));
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ScopeOpenId);

                applicationManager.CreateAsync(descriptor).GetAwaiter().GetResult();
            }
        });

        builder.ConfigureTestServices(services =>
        {
            services.Configure<JwtBearerOptions>(options => { });
        });
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
