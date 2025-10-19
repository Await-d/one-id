using System;
using System.ComponentModel.DataAnnotations;

namespace OneID.Shared.Domain;

/// <summary>
/// 登录历史记录
/// 用于追踪和分析用户登录行为
/// </summary>
public class LoginHistory
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [MaxLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime LoginTime { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 国家/地区
    /// </summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// 纬度
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// 经度
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    [MaxLength(100)]
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [MaxLength(100)]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    [MaxLength(50)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// 登录是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 是否为异常登录
    /// </summary>
    public bool IsAnomalous { get; set; }

    /// <summary>
    /// 异常原因
    /// </summary>
    [MaxLength(500)]
    public string? AnomalyReason { get; set; }

    /// <summary>
    /// 风险评分 (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// 是否已通知用户
    /// </summary>
    public bool UserNotified { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid? SessionId { get; set; }
}

