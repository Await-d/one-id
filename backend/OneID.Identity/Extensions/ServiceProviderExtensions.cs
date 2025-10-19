using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneID.Identity.Seed;
using OneID.Shared.Data;

namespace OneID.Identity.Extensions;

public static class ServiceProviderExtensions
{
    public static async Task InitializeIdentityDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        // 运行数据库迁移
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07")
        {
            // 表已存在，跳过迁移错误
            Console.WriteLine($"⚠️ 数据库表已存在，跳过迁移: {ex.MessageText}");
        }

        // 运行数据库种子
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
