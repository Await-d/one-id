using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 速率限制设置服务接口
/// </summary>
public interface IRateLimitSettingsService
{
    /// <summary>
    /// 获取所有速率限制设置
    /// </summary>
    Task<IReadOnlyList<RateLimitSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的速率限制设置
    /// </summary>
    Task<IReadOnlyList<RateLimitSetting>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取设置
    /// </summary>
    Task<RateLimitSetting?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据限流器名称获取设置
    /// </summary>
    Task<RateLimitSetting?> GetByNameAsync(string limiterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建速率限制设置
    /// </summary>
    Task<RateLimitSetting> CreateAsync(RateLimitSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新速率限制设置
    /// </summary>
    Task<RateLimitSetting> UpdateAsync(RateLimitSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除速率限制设置
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确保默认设置存在
    /// </summary>
    Task EnsureDefaultSettingsAsync(CancellationToken cancellationToken = default);
}
