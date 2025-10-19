using OneID.Shared.Infrastructure;

namespace OneID.Identity.Middleware;

/// <summary>
/// 租户识别中间件 - 从请求中识别租户
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ITenantContextAccessor tenantContextAccessor)
    {
        var tenantId = await ResolveTenantAsync(context, tenantService);

        if (tenantId.HasValue)
        {
            tenantContextAccessor.SetCurrentTenantId(tenantId.Value);
            _logger.LogInformation("Resolved tenant {TenantId} for request {Path}", tenantId.Value, context.Request.Path);
        }
        else
        {
            _logger.LogDebug("No tenant resolved for request {Path}", context.Request.Path);
        }

        await _next(context);

        // 清理租户上下文
        tenantContextAccessor.SetCurrentTenantId(null);
    }

    private async Task<Guid?> ResolveTenantAsync(HttpContext context, ITenantService tenantService)
    {
        // 策略 1: 从请求头获取租户 ID
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            if (Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                var tenantById = await tenantService.GetTenantByIdAsync(tenantId);
                if (tenantById?.IsActive == true)
                {
                    return tenantById.Id;
                }
            }
        }

        // 策略 2: 从域名/子域名获取租户
        var host = context.Request.Host.Host;
        var tenant = await tenantService.GetTenantByDomainAsync(host);
        if (tenant != null)
        {
            return tenant.Id;
        }

        // 策略 3: 从子域名提取租户名称
        // 例如：tenant1.example.com -> tenant1
        var parts = host.Split('.');
        if (parts.Length >= 2)
        {
            var subdomain = parts[0];
            var tenantByName = await tenantService.GetTenantByNameAsync(subdomain);
            if (tenantByName?.IsActive == true)
            {
                return tenantByName.Id;
            }
        }

        // 策略 4: 从查询参数获取（开发/测试环境）
        if (context.Request.Query.TryGetValue("tenantId", out var tenantIdQuery))
        {
            if (Guid.TryParse(tenantIdQuery, out var tenantId))
            {
                var tenantById = await tenantService.GetTenantByIdAsync(tenantId);
                if (tenantById?.IsActive == true)
                {
                    return tenantById.Id;
                }
            }
        }

        // 未找到租户（使用默认租户或无租户模式）
        return null;
    }
}

