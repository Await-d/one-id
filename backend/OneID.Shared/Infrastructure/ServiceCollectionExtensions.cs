using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneID.Shared.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace OneID.Shared.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredDatabase(this IServiceCollection services, IConfiguration configuration, string? migrationsAssembly = null)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var dbOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = ResolveConnectionString(dbOptions, configuration);

            // Use provided migrations assembly name, or default to "OneID.Identity" where migrations are located
            var effectiveMigrationsAssembly = migrationsAssembly ?? "OneID.Identity";

            switch (dbOptions.Provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(effectiveMigrationsAssembly));
                    break;
                case DatabaseProvider.MySql:
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sql => sql.MigrationsAssembly(effectiveMigrationsAssembly));
                    break;
                case DatabaseProvider.Sqlite:
                    options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(effectiveMigrationsAssembly));
                    break;
                default:
                    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(effectiveMigrationsAssembly));
                    break;
            }

            if (dbOptions.EnableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseOpenIddict();
        });

        services.AddDatabaseDeveloperPageExceptionFilter();

        return services;
    }

    public static IServiceCollection AddOneIdInfrastructure(this IServiceCollection services)
    {
        // 注册基础设施服务
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IGdprService, GdprService>();
        services.AddScoped<ISessionManagementService, SessionManagementService>();
        services.AddScoped<ISigningKeyService, SigningKeyService>();
        services.AddScoped<ISecurityRuleService, SecurityRuleService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IUserImportService, UserImportService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IBulkUserOperationsService, BulkUserOperationsService>();
        services.AddScoped<ILoginPolicyService, LoginPolicyService>();
        services.AddScoped<IUserBehaviorAnalyticsService, UserBehaviorAnalyticsService>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
        services.AddScoped<IUserDeviceService, UserDeviceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IWebhookService, WebhookService>();
        
        return services;
    }

    private static string ResolveConnectionString(DatabaseOptions options, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return options.ConnectionString;
        }

        var connection = configuration.GetConnectionString(options.ConnectionName);
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        return connection;
    }
}
