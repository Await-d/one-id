using System;

namespace OneID.Shared.Domain;

/// <summary>
/// 外部认证提供者配置
/// </summary>
public class ExternalAuthProvider
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// 提供者类型（GitHub, Google, Gitee等）
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;
    
    /// <summary>
    /// 提供者名称（唯一标识符，如 "GitHub-Enterprise", "GitHub-Personal"）
    /// 用作认证 scheme name，必须唯一
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// OAuth Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// OAuth Client Secret (加密存储)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// 回调路径
    /// </summary>
    public string CallbackPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 作用域（JSON数组）
    /// </summary>
    public string? Scopes { get; set; }
    
    /// <summary>
    /// 额外配置（JSON对象）
    /// </summary>
    public string? AdditionalConfig { get; set; }
    
    /// <summary>
    /// 租户ID（支持多租户）
    /// </summary>
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 排序顺序
    /// </summary>
    public int DisplayOrder { get; set; }
}
