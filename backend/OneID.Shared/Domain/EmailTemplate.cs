using System;
using System.ComponentModel.DataAnnotations;

namespace OneID.Shared.Domain;

/// <summary>
/// 邮件模板实体
/// 用于存储和管理系统邮件模板
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    

    /// <summary>
    /// 模板唯一标识符（如 "email-confirmation", "password-reset"）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML 邮件内容
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// 纯文本邮件内容（可选）
    /// </summary>
    public string? TextBody { get; set; }

    /// <summary>
    /// 语言代码（如 "zh-CN", "en-US"）
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 是否为系统默认模板（不可删除）
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 可用的变量列表（JSON 格式）
    /// 例如: ["{{userName}}", "{{confirmationLink}}", "{{expiryTime}}"]
    /// </summary>
    [MaxLength(2000)]
    public string? AvailableVariables { get; set; }

    /// <summary>
    /// 最后修改人
    /// </summary>
    [MaxLength(450)]
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 是否已被修改（非默认值）
    /// 用于判断是否应该被 Seed 配置更新
    /// </summary>
    public bool IsModified { get; set; } = false;
}

