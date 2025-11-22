using Microsoft.AspNetCore.Cors.Infrastructure;

namespace OneID.AdminApi.Extensions;

public static class CorsExtensions
{
    /// <summary>
    /// 配置 Admin API 的 CORS 策略
    /// </summary>
    /// <remarks>
    /// 开发环境和本地测试环境启用 CORS
    /// 生产环境通过 Nginx 反向代理实现同域访问，不需要 CORS
    /// </remarks>
    public static void ConfigureAdminApiCors(this WebApplication app)
    {
        var enableCors = app.Environment.IsDevelopment() ||
                         app.Environment.EnvironmentName == "Test" ||
                         app.Configuration.GetValue<bool>("EnableCors", false);

        if (enableCors)
        {
            app.UseCors(policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5101",   // Identity Server (开发)
                        "http://localhost:5173",   // Vite dev server
                        "http://localhost:18080",  // 本地测试环境 Identity
                        "http://localhost:10230"   // Docker Compose Identity
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        }
    }
}
