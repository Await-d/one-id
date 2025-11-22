using System;

namespace OneID.Shared.Domain;

public class CorsSetting
{
    public Guid Id { get; set; }
    public string AllowedOrigins { get; set; } = string.Empty;
    public bool AllowAnyOrigin { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已被修改（非默认值）
    /// 用于判断是否应该被 Seed 配置更新
    /// </summary>
    public bool IsModified { get; set; } = false;
}
