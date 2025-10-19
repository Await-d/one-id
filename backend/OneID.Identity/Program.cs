using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using OneID.Identity.Authentication;
using OneID.Identity.Configuration;
using OneID.Identity.Extensions;
using OneID.Identity.Middleware;
using OneID.Identity.Seed;
using OneID.Identity.Services;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

Console.WriteLine("=============== STARTING ONEID IDENTITY SERVER ===============");
Console.Out.Flush();

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=============== WebApplicationBuilder CREATED ===============");
Console.Out.Flush();

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration);
});

Console.WriteLine("=============== SERILOG CONFIGURED ===============");
Console.Out.Flush();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddConfiguredSwagger();

builder.Services.AddCors();

Console.WriteLine("=============== ADDING RATE LIMITER ===============");
Console.Out.Flush();

builder.Services.AddRateLimiter(options =>
{
    // 全局限流：每个IP每分钟100个请求
    options.AddFixedWindowLimiter("global", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // 登录API限流：每个IP每5分钟10次尝试，防止暴力破解
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.QueueLimit = 0;
    });

    // Token端点限流：每个IP每分钟20个请求
    options.AddFixedWindowLimiter("token", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // 注册API限流：每个IP每小时5次注册
    options.AddFixedWindowLimiter("register", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromHours(1);
        opt.QueueLimit = 0;
    });

    // 密码重置限流：每个IP每小时3次
    options.AddFixedWindowLimiter("password-reset", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromHours(1);
        opt.QueueLimit = 0;
    });
});

Console.WriteLine("=============== ADDING DATABASE ===============");
Console.Out.Flush();

builder.Services.AddConfiguredDatabase(builder.Configuration);

Console.WriteLine("=============== DATABASE CONFIGURED ===============");
Console.Out.Flush();

Console.WriteLine("=============== ADDING DATA PROTECTION ===============");
Console.Out.Flush();

// 配置 Data Protection - 使用共享密钥目录以支持多服务场景
var dataProtectionAppName = builder.Configuration["DataProtection:ApplicationName"] ?? "OneID";
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/shared-keys";
builder.Services.AddDataProtection()
    .SetApplicationName(dataProtectionAppName)
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath)); // 多个服务共享同一个密钥目录

Console.WriteLine("=============== DATA PROTECTION CONFIGURED ===============");
Console.Out.Flush();

Console.WriteLine("=============== ADDING SERVICES ===============");
Console.Out.Flush();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorsSettingsProvider, CorsSettingsProvider>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddEmailService();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IUserBehaviorAnalyticsService, UserBehaviorAnalyticsService>();
builder.Services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddScoped<IUserDeviceService, UserDeviceService>();
builder.Services.AddScoped<ISigningKeyService, SigningKeyService>();
builder.Services.AddScoped<ISecurityRuleService, SecurityRuleService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddLocalizationService(); // 添加国际化服务
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.Configure<ExternalAuthOptions>(builder.Configuration.GetSection(ExternalAuthOptions.SectionName));
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

// 添加后台服务
builder.Services.AddHostedService<SigningKeyRotationService>();

// 配置 ForwardedHeaders 以支持反向代理
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto | 
                               ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

Console.WriteLine("=============== ADDING IDENTITY ===============");
Console.Out.Flush();

builder.Services.AddIdentity<AppUser, AppRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // 默认配置（将在应用启动后从数据库加载实际配置）
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // MFA/TOTP configuration
        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<TotpTokenProvider>("Authenticator")
    .AddSignInManager();

Console.WriteLine("=============== IDENTITY CONFIGURED ===============");
Console.Out.Flush();

Console.WriteLine("=============== SKIPPING POST CONFIGURE (CAUSES INFINITE LOOP) ===============");
Console.Out.Flush();

// 移除 PostConfigure - 它会导致无限递归！
// 配置将在应用启动后异步加载
// builder.Services.PostConfigure<IdentityOptions>(options => ...)

Console.WriteLine("=============== CONFIGURING COOKIE ===============");
Console.Out.Flush();

// 配置 Cookie 会话超时（从数据库动态加载）
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "OneID.Session";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // 默认会话超时（将在应用启动后从数据库加载实际配置）
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

Console.WriteLine("=============== COOKIE CONFIGURED ===============");
Console.Out.Flush();

// 移除 PostConfigure Cookie - 同样会导致无限递归！
// 配置将在应用启动后异步加载
// builder.Services.PostConfigure<CookieAuthenticationOptions>(...)

Console.WriteLine("=============== SKIPPING POST CONFIGURE COOKIE ===============");
Console.Out.Flush();

Console.WriteLine("=============== ADDING AUTHENTICATION ===============");
Console.Out.Flush();

// 配置外部认证提供者（从数据库动态加载）
// 注意：需要在 Build() 之前配置，所以这里创建临时 ServiceProvider
var authBuilder = builder.Services.AddAuthentication();

Console.WriteLine("=============== AUTHENTICATION ADDED ===============");
Console.Out.Flush();

// Add API Key authentication
authBuilder.AddApiKeyAuthentication();

// 注册动态配置服务，稍后在数据库初始化后加载
builder.Services.AddSingleton<IDynamicExternalAuthConfigurationService, DynamicExternalAuthConfigurationService>();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetUserinfoEndpointUris("/connect/userinfo");
        options.SetIntrospectionEndpointUris("/connect/introspect");
        options.SetRevocationEndpointUris("/connect/revocation");
        options.SetLogoutEndpointUris("/connect/endsession");

        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        options.AllowRefreshTokenFlow();

        options.RegisterScopes("openid", "profile", "email", "offline_access");
        
        // 注册 admin_api scope 为 API Resource，用于 Admin API 的 JWT audience 验证
        options.RegisterScopes("admin_api");

        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();

        // 仅开发环境：允许HTTP（不安全，生产环境必须移除）
        if (builder.Environment.IsDevelopment())
        {
            options.SetIssuer(new Uri("http://localhost:5101/"));
        }

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserinfoEndpointPassthrough()
            .EnableLogoutEndpointPassthrough()
            .DisableTransportSecurityRequirement(); // 仅开发环境：允许HTTP

        // 禁用 Access Token 加密（避免重启后开发证书不匹配导致 Token 无法解密）
        // 注意：Token 仍然会被签名验证，只是不加密内容
        options.DisableAccessTokenEncryption();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

Console.WriteLine("=============== BUILDING APPLICATION ===============");
Console.Out.Flush();

var app = builder.Build();

Console.WriteLine("=============== APPLICATION BUILT ===============");
Console.Out.Flush();

Console.WriteLine("=============== INITIALIZING DATABASE ===============");
Console.Out.Flush();

await app.Services.InitializeIdentityDatabaseAsync();

Console.WriteLine("=============== DATABASE INITIALIZED ===============");
Console.Out.Flush();

// 从数据库加载外部认证提供者配置并动态注册
var logger = app.Services.GetRequiredService<ILogger<Program>>();
await authBuilder.AddDynamicExternalAuthenticationAsync(app.Services, logger);

// 从数据库加载签名密钥到 OpenIddict
await app.LoadSigningKeysFromDatabaseAsync(logger);

var corsSettings = await LoadCorsSettingsAsync(app.Services);

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

// 租户识别（必须在认证之前）
app.UseMiddleware<TenantResolutionMiddleware>();

// 应用安全规则（IP 黑白名单等）
app.UseMiddleware<SecurityRuleMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(policy => ApplyCorsPolicy(policy, corsSettings.Options));
app.UseRateLimiter();
app.UseCustomRequestLocalization(); // 添加请求本地化中间件

// 静态文件服务（用于前端SPA）
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SPA fallback - 所有未匹配的请求返回index.html
app.MapFallbackToFile("index.html");

await app.RunAsync();

static async Task<CorsSettingsResult> LoadCorsSettingsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var provider = scope.ServiceProvider.GetRequiredService<ICorsSettingsProvider>();
    return await provider.GetAsync();
}

static void ApplyCorsPolicy(CorsPolicyBuilder policy, CorsSettingsOptions options)
{
    if (options.AllowAnyOrigin)
    {
        policy.AllowAnyOrigin();
    }
    else if (options.AllowedOrigins.Length > 0)
    {
        policy.WithOrigins(options.AllowedOrigins)
            .AllowCredentials();
    }

    policy.AllowAnyHeader().AllowAnyMethod();
}
