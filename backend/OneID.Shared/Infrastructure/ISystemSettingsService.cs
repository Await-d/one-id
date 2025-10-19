using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 系统设置服务接口
/// </summary>
public interface ISystemSettingsService
{
    // 基本 CRUD 操作
    Task<IReadOnlyList<SystemSetting>> GetAllSettingsAsync(string? group = null, CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetSettingByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetSettingByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemSetting> CreateSettingAsync(SystemSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default);
    Task<SystemSetting> UpdateSettingAsync(SystemSetting setting, string? modifiedBy = null, CancellationToken cancellationToken = default);
    Task DeleteSettingAsync(int id, CancellationToken cancellationToken = default);
    
    // 便捷方法 - 按键获取/设置值
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<string> GetValueOrDefaultAsync(string key, string defaultValue, CancellationToken cancellationToken = default);
    Task<int> GetIntValueAsync(string key, int defaultValue = 0, CancellationToken cancellationToken = default);
    Task<bool> GetBoolValueAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default);
    Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0m, CancellationToken cancellationToken = default);
    Task<T?> GetJsonValueAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    
    Task SetValueAsync(string key, string value, string? modifiedBy = null, CancellationToken cancellationToken = default);
    Task SetIntValueAsync(string key, int value, string? modifiedBy = null, CancellationToken cancellationToken = default);
    Task SetBoolValueAsync(string key, bool value, string? modifiedBy = null, CancellationToken cancellationToken = default);
    Task SetDecimalValueAsync(string key, decimal value, string? modifiedBy = null, CancellationToken cancellationToken = default);
    Task SetJsonValueAsync<T>(string key, T value, string? modifiedBy = null, CancellationToken cancellationToken = default) where T : class;
    
    // 批量操作
    Task<Dictionary<string, string>> GetSettingsByGroupAsync(string group, CancellationToken cancellationToken = default);
    Task ResetToDefaultAsync(string key, CancellationToken cancellationToken = default);
    Task ResetGroupToDefaultAsync(string group, CancellationToken cancellationToken = default);
    
    // 初始化默认设置
    Task EnsureDefaultSettingsAsync(CancellationToken cancellationToken = default);
}

