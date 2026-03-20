using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneID.Shared.Data;

namespace OneID.Identity.Tests;

public sealed class IdentityFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection = new SqliteConnection("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseSetting("Persistence:Provider", "Sqlite");
        builder.UseSetting("Persistence:ConnectionName", "Default");
        builder.UseSetting("ConnectionStrings:Default", "DataSource=:memory:");
        builder.UseSetting("DataProtection:KeysPath",
            Path.Combine(Path.GetTempPath(), "oneid-identity-test-" + Guid.NewGuid()));

        builder.ConfigureServices(services =>
        {
            // Remove Postgres DbContext registrations
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // Replace with in-memory SQLite
            services.AddDbContext<AppDbContext>((_, options) =>
            {
                options.UseSqlite(_connection);
                options.UseOpenIddict();
            });

            // Ensure schema created (skip migrations) — idempotent via EnsureCreated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try { db.Database.EnsureCreated(); } catch { /* table already exists — ignore */ }
        });
    }

    public new void Dispose()
    {
        _connection.Dispose();
        base.Dispose();
    }
}
