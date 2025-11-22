namespace OneID.Shared.Configuration;

/// <summary>
/// 配置热更新选项
/// </summary>
public sealed class HotReloadOptions
{
    public const string SectionName = "HotReload";

    /// <summary>
    /// 是否启用定时轮询
    /// </summary>
    public bool PollingEnabled { get; set; } = true;

    /// <summary>
    /// 轮询间隔（秒）
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 是否在配置变更时自动应用（不需要调用刷新端点）
    /// </summary>
    public bool AutoApplyChanges { get; set; } = true;
}
