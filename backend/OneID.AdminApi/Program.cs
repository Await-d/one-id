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

builder.Services.AddAuthorization(options =>
{
    // 添加 admin_api scope 验证策略
    options.AddPolicy("AdminApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        // 检查 scope claim 中是否包含 admin_api（scope 是空格分隔的字符串）
        policy.RequireAssertion(context =>
        {
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            return scopeClaim != null && scopeClaim.Split(' ').Contains("admin_api");
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

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// API 路由（所有控制器都需要 admin_api scope）
app.MapControllers()
    .RequireAuthorization("AdminApiScope");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

// SPA fallback - 任何未匹配路由的请求都返回index.html  
// 必须放在所有Map之后
app.MapFallback(async context =>
{
    // 只处理非API路径
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        var filePath = Path.Combine(app.Environment.WebRootPath, "index.html");
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

await app.RunAsync();

public partial class Program;
