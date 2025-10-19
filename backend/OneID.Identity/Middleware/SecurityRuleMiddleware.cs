using OneID.Shared.Infrastructure;

namespace OneID.Identity.Middleware;

/// <summary>
/// 安全规则中间件 - 应用 IP 黑白名单等安全规则
/// </summary>
public class SecurityRuleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityRuleMiddleware> _logger;

    public SecurityRuleMiddleware(RequestDelegate next, ILogger<SecurityRuleMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISecurityRuleService securityRuleService)
    {
        var ipAddress = GetClientIpAddress(context);

        if (!string.IsNullOrEmpty(ipAddress))
        {
            var isAllowed = await securityRuleService.IsIpAllowedAsync(ipAddress);

            if (!isAllowed)
            {
                _logger.LogWarning("Access denied for IP {IpAddress} due to security rules", ipAddress);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Forbidden",
                    Message = "Access denied due to security rules",
                    IpAddress = ipAddress
                });

                return;
            }
        }

        await _next(context);
    }

    private string? GetClientIpAddress(HttpContext context)
    {
        // 尝试从 X-Forwarded-For 头获取真实 IP（反向代理场景）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ips.Length > 0)
            {
                return ips[0]; // 第一个 IP 是客户端真实 IP
            }
        }

        // 尝试从 X-Real-IP 头获取
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 使用连接的远程 IP
        return context.Connection.RemoteIpAddress?.ToString();
    }
}

