using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneID.Shared.Data;

namespace OneID.Shared.Configuration;

/// <summary>
/// 配置轮询后台服务
/// 定时检查数据库配置变更并自动刷新
/// 使用 ConfigurationVersion 表进行高效变更检测（单次查询）
/// </summary>
public sealed class ConfigurationPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfigurationRefreshService _refreshService;
    private readonly ILogger<ConfigurationPollingService> _logger;
    private readonly HotReloadOptions _options;

    private long _lastKnownVersion = 0;

    public ConfigurationPollingService(
        IServiceScopeFactory scopeFactory,
        IConfigurationRefreshService refreshService,
        IOptions<HotReloadOptions> options,
        ILogger<ConfigurationPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _refreshService = refreshService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.PollingEnabled)
        {
            _logger.LogInformation("Configuration polling is disabled");
            return;
        }

        _logger.LogInformation("Configuration polling started, interval: {Interval}s (using version table)", _options.PollingIntervalSeconds);

        // 初始加载并获取当前版本号
        _lastKnownVersion = await GetCurrentVersionAsync(stoppingToken);
        await _refreshService.RefreshAllAsync(stoppingToken);
        _logger.LogInformation("Initial configuration loaded, version: {Version}", _lastKnownVersion);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
                await CheckForChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during configuration polling");
                // 继续运行，不中断服务
            }
        }

        _logger.LogInformation("Configuration polling stopped");
    }

    private async Task CheckForChangesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 单次查询获取当前版本号
        var configVersion = await dbContext.ConfigurationVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == 1, cancellationToken);

        if (configVersion == null)
        {
            _logger.LogWarning("ConfigurationVersion record not found, skipping this poll cycle");
            return;
        }

        // 检查版本是否变化
        if (configVersion.Version > _lastKnownVersion)
        {
            _logger.LogInformation(
                "Configuration change detected: Version {OldVersion} → {NewVersion}, Changed by: {ChangedBy}",
                _lastKnownVersion,
                configVersion.Version,
                configVersion.LastChangedBy ?? "Unknown");

            // 刷新所有配置（因为我们不知道具体是哪个配置变了）
            await _refreshService.RefreshAllAsync(cancellationToken);

            // 更新已知版本号
            _lastKnownVersion = configVersion.Version;

            _logger.LogInformation("All configurations refreshed successfully");
        }
    }

    private async Task<long> GetCurrentVersionAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var configVersion = await dbContext.ConfigurationVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == 1, cancellationToken);

        return configVersion?.Version ?? 1;
    }
}
