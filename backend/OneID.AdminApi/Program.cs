using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using OneID.AdminApi.Configuration;
using OneID.AdminApi.Extensions;
using OneID.AdminApi.Services;
using Microsoft.AspNetCore.Identity;
using OneID.Shared.Application.Clients;
using OneID.Shared.Application.Users;
using OneID.Shared.Application.AuditLogs;
using OneID.Shared.Application.ExternalAuth;
using OneID.Shared.Domain;
using OneID.Shared.Data;
using OneID.Shared.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddConfiguredDatabase(builder.Configuration);

// 配置 Data Protection - 使用共享密钥目录以支持多服务场景
var dataProtectionAppName = builder.Configuration["DataProtection:ApplicationName"] ?? "OneID";
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/shared-keys";
builder.Services.AddDataProtection()
    .SetApplicationName(dataProtectionAppName)
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath)); // 多个服务共享同一个密钥目录

builder.Services.AddIdentity<AppUser, AppRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AppDbContext>();
    });

builder.Services.AddScoped<IClientQueryService, ClientQueryService>();
builder.Services.AddScoped<IClientCommandService, ClientCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>(); // 添加 AuditLogService 用于记录审计日志
builder.Services.AddScoped<IExternalAuthProviderQueryService, ExternalAuthProviderQueryService>();
builder.Services.AddScoped<IExternalAuthProviderCommandService, ExternalAuthProviderCommandService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IClientValidationSettingsProvider, ClientValidationSettingsProvider>();
builder.Services.AddScoped<ICorsSettingsProvider, CorsSettingsProvider>();
builder.Services.AddScoped<ISessionManagementService, SessionManagementService>();
builder.Services.AddScoped<IGdprService, GdprService>();
builder.Services.AddScoped<ISigningKeyService, SigningKeyService>();
builder.Services.AddScoped<ISecurityRuleService, SecurityRuleService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IUserImportService, UserImportService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IUserDeviceService, UserDeviceService>();

// 配置 ForwardedHeaders 以支持反向代理
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto | 
                               ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.Authority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5101";
        
        // 配置Token验证参数
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            // 不验证 audience，因为 OpenIddict 默认不生成 audience claim
            // 通过 scope 验证已经足够（Token 必须包含 admin_api scope）
            ValidateAudience = false,
            
            // 允许多个 Issuer（内部 Docker 地址和外部 HTTPS 地址）
            // 注意：需要同时包含带尾部斜杠和不带尾部斜杠的版本
            ValidateIssuer = true,
            ValidIssuers = new[]
            {
                "http://identity",
                "http://identity/",
                "http://identity:80",
                "http://identity:80/",
                "http://localhost:5101",
                "http://localhost:5101/",
                "http://localhost:10230",  // 对外暴露的端口
                "http://localhost:10230/",
                "https://auth.awitk.cn",    // 生产域名
                "https://auth.awitk.cn/",
                builder.Configuration["Identity:Authority"]?.TrimEnd('/'),
                builder.Configuration["Identity:Authority"]?.TrimEnd('/') + "/",
                builder.Configuration["Identity:ExternalAuthority"]?.TrimEnd('/'),
                builder.Configuration["Identity:ExternalAuthority"]?.TrimEnd('/') + "/",
            }.Where(s => !string.IsNullOrEmpty(s)).ToArray(),
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

// 添加 CORS 服务
builder.Services.AddCors();

builder.Services.AddAuthorization(options =>
{
    // 添加 admin_api scope 验证策略
    options.AddPolicy("AdminApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        // 检查 scope claim 中是否包含 admin_api
        // OpenIddict 可能将 scope 存储为单个字符串或多个独立 claim
        policy.RequireAssertion(context =>
        {
            var logger = context.Resource as HttpContext;
            
            // 方式1: 检查单个 scope claim（空格分隔）
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            if (scopeClaim != null)
            {
                logger?.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogInformation("Scope claim (single): {Scope}", scopeClaim);
                if (scopeClaim.Split(' ').Contains("admin_api"))
                    return true;
            }
            
            // 方式2: 检查多个 scope claims
            var scopeClaims = context.User.FindAll("scope").Select(c => c.Value).ToList();
            if (scopeClaims.Any())
            {
                logger?.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogInformation("Scope claims (multiple): {Scopes}", string.Join(", ", scopeClaims));
                if (scopeClaims.Contains("admin_api"))
                    return true;
            }
            
            // 记录所有 claims 用于调试
            var allClaims = context.User.Claims.Select(c => $"{c.Type}={c.Value}");
            logger?.RequestServices.GetRequiredService<ILogger<Program>>()
                .LogWarning("Authorization failed. All claims: {Claims}", string.Join("; ", allClaims));
            
            return false;
        });
    });
    
    // 不设置全局 FallbackPolicy，让 SPA 路由（callback 等）可以匿名访问
    // API 控制器已通过 [Authorize] 特性单独保护
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddConfiguredSwagger();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

// 使用 ForwardedHeaders 中间件（必须在其他中间件之前）
app.UseForwardedHeaders();

// 添加安全HTTP头
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // HSTS - 仅在HTTPS时添加
    if (context.Request.IsHttps)
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    await next();
});

// 静态文件服务（用于前端Admin Portal）
var options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(options);
app.UseStaticFiles();

// CORS 配置 - 仅在开发环境启用
// 生产环境通过 Nginx 反向代理实现同域访问，不需要 CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5101",  // Identity Server (开发)
                "http://localhost:5173",  // Vite dev server
                "http://localhost:18080"  // 本地测试环境
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// API 路由（所有控制器都需要 admin_api scope）
app.MapControllers()
    .RequireAuthorization("AdminApiScope");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

// API 服务提示 - AdminApi 是纯后端 API 服务
// 前端页面(Admin Portal)由 Identity 服务提供在 /admin 路径
app.MapFallback(async context =>
{
    // 只处理非API路径
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        // AdminApi 是纯 API 服务,前端页面在 Identity 服务的 /admin 路径
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            service = "OneID Admin API",
            message = "This is a backend API service. Frontend is available at Identity service /admin path.",
            endpoints = new
            {
                swagger = "/swagger",
                health = "/health",
                users = "/api/users",
                clients = "/api/clients",
                roles = "/api/roles"
            },
            status = "running"
        });
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

await app.RunAsync();

public partial class Program;
