using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 速率限制设置服务实现
/// </summary>
public sealed class RateLimitSettingsService : IRateLimitSettingsService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RateLimitSettingsService> _logger;

    public RateLimitSettingsService(
        AppDbContext dbContext,
        ILogger<RateLimitSettingsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RateLimitSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RateLimitSettings
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.LimiterName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RateLimitSetting>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RateLimitSettings
            .Where(s => s.Enabled)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.LimiterName)
            .ToListAsync(cancellationToken);
    }

    public async Task<RateLimitSetting?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RateLimitSettings
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<RateLimitSetting?> GetByNameAsync(string limiterName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RateLimitSettings
            .FirstOrDefaultAsync(s => s.LimiterName == limiterName, cancellationToken);
    }

    public async Task<RateLimitSetting> CreateAsync(RateLimitSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        // 输入验证
        if (string.IsNullOrWhiteSpace(setting.LimiterName))
        {
            throw new ArgumentException("LimiterName is required and cannot be empty", nameof(setting.LimiterName));
        }

        if (setting.PermitLimit <= 0)
        {
            throw new ArgumentException("PermitLimit must be greater than 0", nameof(setting.PermitLimit));
        }

        if (setting.WindowSeconds <= 0)
        {
            throw new ArgumentException("WindowSeconds must be greater than 0", nameof(setting.WindowSeconds));
        }

        if (setting.QueueLimit < 0)
        {
            throw new ArgumentException("QueueLimit cannot be negative", nameof(setting.QueueLimit));
        }

        // 检查是否已存在同名的限流器
        var existing = await GetByNameAsync(setting.LimiterName, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Rate limiter with name '{setting.LimiterName}' already exists");
        }

        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.LastModifiedBy = modifiedBy;

        _dbContext.RateLimitSettings.Add(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Rate limit setting created: {LimiterName} ({PermitLimit} requests per {WindowSeconds}s) by {User}",
            setting.LimiterName, setting.PermitLimit, setting.WindowSeconds, modifiedBy ?? "System");

        return setting;
    }

    public async Task<RateLimitSetting> UpdateAsync(RateLimitSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        // 输入验证
        if (setting.PermitLimit <= 0)
        {
            throw new ArgumentException("PermitLimit must be greater than 0", nameof(setting.PermitLimit));
        }

        if (setting.WindowSeconds <= 0)
        {
            throw new ArgumentException("WindowSeconds must be greater than 0", nameof(setting.WindowSeconds));
        }

        if (setting.QueueLimit < 0)
        {
            throw new ArgumentException("QueueLimit cannot be negative", nameof(setting.QueueLimit));
        }

        var existing = await GetByIdAsync(setting.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Rate limit setting with ID {setting.Id} not found");

        existing.DisplayName = setting.DisplayName;
        existing.Description = setting.Description;
        existing.Enabled = setting.Enabled;
        existing.PermitLimit = setting.PermitLimit;
        existing.WindowSeconds = setting.WindowSeconds;
        existing.QueueLimit = setting.QueueLimit;
        existing.SortOrder = setting.SortOrder;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.LastModifiedBy = modifiedBy;
        existing.IsModified = true; // 标记为已修改，防止 Seed 覆盖

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Rate limit setting updated: {LimiterName} ({PermitLimit} requests per {WindowSeconds}s) by {User}",
            existing.LimiterName, existing.PermitLimit, existing.WindowSeconds, modifiedBy ?? "System");

        return existing;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var setting = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Rate limit setting with ID {id} not found");

        _dbContext.RateLimitSettings.Remove(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rate limit setting deleted: {LimiterName}", setting.LimiterName);
    }

    public async Task EnsureDefaultSettingsAsync(CancellationToken cancellationToken = default)
    {
        var defaultSettings = GetDefaultSettings();

        foreach (var setting in defaultSettings)
        {
            var existing = await GetByNameAsync(setting.LimiterName, cancellationToken);
            if (existing == null)
            {
                await CreateAsync(setting, "System", cancellationToken);
            }
        }

        _logger.LogInformation("Default rate limit settings ensured");
    }

    public static List<RateLimitSetting> GetDefaultSettings()
    {
        return new List<RateLimitSetting>
        {
            new RateLimitSetting
            {
                LimiterName = "global",
                DisplayName = "Global Rate Limit",
                Description = "Maximum requests per minute per IP address for all endpoints",
                Enabled = true,
                PermitLimit = 100,
                WindowSeconds = 60,
                QueueLimit = 0,
                SortOrder = 1
            },
            new RateLimitSetting
            {
                LimiterName = "login",
                DisplayName = "Login Rate Limit",
                Description = "Maximum login attempts per IP to prevent brute force attacks",
                Enabled = true,
                PermitLimit = 10,
                WindowSeconds = 300, // 5 minutes
                QueueLimit = 0,
                SortOrder = 2
            },
            new RateLimitSetting
            {
                LimiterName = "token",
                DisplayName = "Token Endpoint Rate Limit",
                Description = "Maximum token requests per minute per IP",
                Enabled = true,
                PermitLimit = 20,
                WindowSeconds = 60,
                QueueLimit = 0,
                SortOrder = 3
            },
            new RateLimitSetting
            {
                LimiterName = "register",
                DisplayName = "Registration Rate Limit",
                Description = "Maximum registration attempts per hour per IP",
                Enabled = true,
                PermitLimit = 5,
                WindowSeconds = 3600, // 1 hour
                QueueLimit = 0,
                SortOrder = 4
            },
            new RateLimitSetting
            {
                LimiterName = "password-reset",
                DisplayName = "Password Reset Rate Limit",
                Description = "Maximum password reset requests per hour per IP",
                Enabled = true,
                PermitLimit = 3,
                WindowSeconds = 3600, // 1 hour
                QueueLimit = 0,
                SortOrder = 5
            }
        };
    }
}
