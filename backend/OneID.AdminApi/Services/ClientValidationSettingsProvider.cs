using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneID.AdminApi.Configuration;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Services;

public sealed class ClientValidationSettingsProvider : IClientValidationSettingsProvider
{
    private const string AllowedSchemesEnv = "CLIENT_VALIDATION_ALLOWED_SCHEMES";
    private const string AllowHttpLoopbackEnv = "CLIENT_VALIDATION_ALLOW_HTTP_LOOPBACK";
    private const string AllowedHostsEnv = "CLIENT_VALIDATION_ALLOWED_HOSTS";

    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClientValidationSettingsProvider> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private ClientValidationSettingsResult? _cached;

    public ClientValidationSettingsProvider(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger<ClientValidationSettingsProvider> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ClientValidationSettingsResult> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_cached is not null)
            {
                return _cached;
            }

            var entity = await _dbContext.ClientValidationSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            if (entity is null)
            {
                entity = CreateFromEnvironment();
                entity.Id = Guid.NewGuid();
                entity.UpdatedAt = DateTime.UtcNow;

                _dbContext.ClientValidationSettings.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Client validation settings initialized from environment variables.");
            }

            _cached = Map(entity);
            return _cached;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<ClientValidationSettingsResult> SetAsync(ClientValidationOptions options, CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            var entity = await _dbContext.ClientValidationSettings.FirstOrDefaultAsync(cancellationToken);
            if (entity is null)
            {
                entity = new ClientValidationSetting
                {
                    Id = Guid.NewGuid()
                };
                _dbContext.ClientValidationSettings.Add(entity);
            }

            entity.AllowedSchemes = string.Join(',', options.AllowedSchemes);
            entity.AllowHttpOnLoopback = options.AllowHttpOnLoopback;
            entity.AllowedHosts = string.Join(',', options.AllowedHosts);
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _cached = Map(entity);
            _logger.LogInformation("Client validation settings updated.");
            return _cached;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private ClientValidationSetting CreateFromEnvironment()
    {
        var allowedSchemes = GetCsvValues(AllowedSchemesEnv, new[] { "https", "http" });
        var allowHttp = GetBoolean(AllowHttpLoopbackEnv, defaultValue: true);
        var allowedHosts = GetCsvValues(AllowedHostsEnv, Array.Empty<string>());

        return new ClientValidationSetting
        {
            AllowedSchemes = string.Join(',', allowedSchemes),
            AllowHttpOnLoopback = allowHttp,
            AllowedHosts = string.Join(',', allowedHosts)
        };
    }

    private ClientValidationSettingsResult Map(ClientValidationSetting setting)
    {
        var schemes = Split(setting.AllowedSchemes, new[] { "https", "http" });
        var hosts = Split(setting.AllowedHosts, Array.Empty<string>());

        var options = new ClientValidationOptions
        {
            AllowedSchemes = schemes,
            AllowHttpOnLoopback = setting.AllowHttpOnLoopback,
            AllowedHosts = hosts
        };

        return new ClientValidationSettingsResult(options, setting.UpdatedAt);
    }

    private string[] Split(string source, string[] defaultValues)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return defaultValues;
        }

        var values = source
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        return values.Length > 0 ? values : defaultValues;
    }

    private string[] GetCsvValues(string key, string[] defaultValues)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValues;
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private bool GetBoolean(string key, bool defaultValue)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
