using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Configuration;

/// <summary>
/// 配置刷新服务接口
/// </summary>
public interface IConfigurationRefreshService
{
    /// <summary>
    /// 刷新所有配置
    /// </summary>
    Task RefreshAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新速率限制配置
    /// </summary>
    Task RefreshRateLimitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新 CORS 配置
    /// </summary>
    Task RefreshCorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新外部认证配置
    /// </summary>
    Task RefreshExternalAuthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前速率限制配置
    /// </summary>
    DynamicRateLimitOptions GetRateLimitOptions();

    /// <summary>
    /// 获取当前 CORS 配置
    /// </summary>
    DynamicCorsOptions GetCorsOptions();

    /// <summary>
    /// 获取当前外部认证配置
    /// </summary>
    DynamicExternalAuthOptions GetExternalAuthOptions();

    /// <summary>
    /// 配置变更事件
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// 配置变更事件参数
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public ConfigurationType ConfigurationType { get; init; }
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 配置类型
/// </summary>
public enum ConfigurationType
{
    All,
    RateLimit,
    Cors,
    ExternalAuth
}

/// <summary>
/// 配置刷新服务实现
/// </summary>
public sealed class ConfigurationRefreshService : IConfigurationRefreshService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConfigurationRefreshService> _logger;
    private readonly object _lock = new();

    private DynamicRateLimitOptions _rateLimitOptions = new();
    private DynamicCorsOptions _corsOptions = new();
    private DynamicExternalAuthOptions _externalAuthOptions = new();

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConfigurationRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        await RefreshRateLimitAsync(cancellationToken);
        await RefreshCorsAsync(cancellationToken);
        await RefreshExternalAuthAsync(cancellationToken);

        OnConfigurationChanged(ConfigurationType.All);
        _logger.LogInformation("All configurations refreshed");
    }

    public async Task RefreshRateLimitAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await dbContext.RateLimitSettings
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        var maxUpdated = settings.Any()
            ? settings.Max(s => s.UpdatedAt)
            : DateTime.MinValue;

        lock (_lock)
        {
            _rateLimitOptions = new DynamicRateLimitOptions
            {
                Settings = settings,
                LastUpdated = maxUpdated,
                Version = _rateLimitOptions.Version + 1
            };
        }

        OnConfigurationChanged(ConfigurationType.RateLimit);
        _logger.LogInformation("RateLimit configuration refreshed, {Count} settings loaded", settings.Count);
    }

    public async Task RefreshCorsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var setting = await dbContext.CorsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var allowedOrigins = setting?.AllowedOrigins?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? new List<string>();

        lock (_lock)
        {
            _corsOptions = new DynamicCorsOptions
            {
                Setting = setting,
                AllowedOrigins = allowedOrigins,
                AllowAnyOrigin = setting?.AllowAnyOrigin ?? false,
                LastUpdated = setting?.UpdatedAt ?? DateTime.MinValue,
                Version = _corsOptions.Version + 1
            };
        }

        OnConfigurationChanged(ConfigurationType.Cors);
        _logger.LogInformation("CORS configuration refreshed, AllowAnyOrigin={AllowAny}, Origins={Count}",
            _corsOptions.AllowAnyOrigin, allowedOrigins.Count);
    }

    public async Task RefreshExternalAuthAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var providers = await dbContext.Set<ExternalAuthProvider>()
            .AsNoTracking()
            .Where(p => p.Enabled)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);

        var maxUpdated = providers.Any()
            ? providers.Max(p => p.UpdatedAt)
            : DateTime.MinValue;

        lock (_lock)
        {
            _externalAuthOptions = new DynamicExternalAuthOptions
            {
                Providers = providers,
                LastUpdated = maxUpdated,
                Version = _externalAuthOptions.Version + 1
            };
        }

        OnConfigurationChanged(ConfigurationType.ExternalAuth);
        _logger.LogInformation("ExternalAuth configuration refreshed, {Count} providers loaded", providers.Count);
    }

    public DynamicRateLimitOptions GetRateLimitOptions()
    {
        lock (_lock) return _rateLimitOptions;
    }

    public DynamicCorsOptions GetCorsOptions()
    {
        lock (_lock) return _corsOptions;
    }

    public DynamicExternalAuthOptions GetExternalAuthOptions()
    {
        lock (_lock) return _externalAuthOptions;
    }

    private void OnConfigurationChanged(ConfigurationType type)
    {
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs { ConfigurationType = type });
    }
}
