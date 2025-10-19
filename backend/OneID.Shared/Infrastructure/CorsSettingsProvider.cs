using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public sealed record CorsSettingsOptions(string[] AllowedOrigins, bool AllowAnyOrigin);

public sealed record CorsSettingsResult(CorsSettingsOptions Options, DateTime UpdatedAt);

public interface ICorsSettingsProvider
{
    Task<CorsSettingsResult> GetAsync(CancellationToken cancellationToken = default);
    Task<CorsSettingsResult> SetAsync(CorsSettingsOptions options, CancellationToken cancellationToken = default);
}

public sealed class CorsSettingsProvider : ICorsSettingsProvider
{
    private const string AllowedOriginsEnv = "IDENTITY_CORS_ALLOWED_ORIGINS";
    private const string AllowAnyOriginEnv = "IDENTITY_CORS_ALLOW_ANY_ORIGIN";

    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CorsSettingsProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CorsSettingsResult? _cached;

    public CorsSettingsProvider(AppDbContext dbContext, IConfiguration configuration, ILogger<CorsSettingsProvider> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CorsSettingsResult> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cached is not null)
            {
                return _cached;
            }

            var entity = await _dbContext.CorsSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            if (entity is null)
            {
                entity = CreateFromEnvironment();
                entity.Id = Guid.NewGuid();
                entity.UpdatedAt = DateTime.UtcNow;

                _dbContext.CorsSettings.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("CORS settings initialized from environment variables.");
            }

            _cached = Map(entity);
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CorsSettingsResult> SetAsync(CorsSettingsOptions options, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entity = await _dbContext.CorsSettings.FirstOrDefaultAsync(cancellationToken);
            if (entity is null)
            {
                entity = new CorsSetting { Id = Guid.NewGuid() };
                _dbContext.CorsSettings.Add(entity);
            }

            entity.AllowedOrigins = string.Join(',', options.AllowedOrigins ?? Array.Empty<string>());
            entity.AllowAnyOrigin = options.AllowAnyOrigin;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _cached = Map(entity);
            _logger.LogInformation("CORS settings updated.");
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    private CorsSetting CreateFromEnvironment()
    {
        var defaultOrigins = new[] { "http://localhost:5173", "http://localhost:5102" };
        var allowedOrigins = GetCsvValues(AllowedOriginsEnv, defaultOrigins);
        var allowAny = GetBoolean(AllowAnyOriginEnv, defaultValue: false);

        return new CorsSetting
        {
            AllowedOrigins = string.Join(',', allowedOrigins),
            AllowAnyOrigin = allowAny
        };
    }

    private CorsSettingsResult Map(CorsSetting setting)
    {
        var origins = Split(setting.AllowedOrigins);
        var options = new CorsSettingsOptions(origins, setting.AllowAnyOrigin);
        return new CorsSettingsResult(options, setting.UpdatedAt);
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

    private static string[] Split(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Array.Empty<string>();
        }

        return source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
