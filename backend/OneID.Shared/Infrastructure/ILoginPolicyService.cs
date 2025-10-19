namespace OneID.Shared.Infrastructure;

/// <summary>
/// 登录策略验证服务接口
/// </summary>
public interface ILoginPolicyService
{
    /// <summary>
    /// 验证IP地址是否允许访问
    /// </summary>
    Task<bool> ValidateIpAccessAsync(string ipAddress, Guid? userId = null, List<string>? userRoles = null);

    /// <summary>
    /// 验证当前时间是否允许登录
    /// </summary>
    Task<bool> ValidateLoginTimeAsync(Guid? userId = null, List<string>? userRoles = null, string timeZone = "UTC");

    /// <summary>
    /// 获取拒绝访问的原因
    /// </summary>
    Task<string> GetAccessDenialReasonAsync(string ipAddress, Guid? userId = null, List<string>? userRoles = null, string timeZone = "UTC");
}

