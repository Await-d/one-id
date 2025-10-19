using System.Globalization;
using System.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 本地化服务，用于获取多语言错误消息和其他本地化字符串
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="culture">文化信息，null则使用当前文化</param>
    /// <returns>本地化字符串</returns>
    string GetString(string key, CultureInfo? culture = null);
    
    /// <summary>
    /// 获取格式化的本地化字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的本地化字符串</returns>
    string GetString(string key, params object[] args);
    
    /// <summary>
    /// 设置当前文化
    /// </summary>
    /// <param name="culture">文化名称，如 "zh-CN", "en-US"</param>
    void SetCulture(string culture);
    
    /// <summary>
    /// 获取当前文化
    /// </summary>
    CultureInfo CurrentCulture { get; }
}

public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;
    
    public LocalizationService()
    {
        // 使用ErrorMessages资源文件
        _resourceManager = new ResourceManager(
            "OneID.Shared.Resources.ErrorMessages", 
            typeof(LocalizationService).Assembly);
        _currentCulture = CultureInfo.CurrentCulture;
    }
    
    public CultureInfo CurrentCulture => _currentCulture;
    
    public string GetString(string key, CultureInfo? culture = null)
    {
        try
        {
            var targetCulture = culture ?? _currentCulture;
            var value = _resourceManager.GetString(key, targetCulture);
            return value ?? key; // 如果找不到资源，返回key本身
        }
        catch
        {
            return key;
        }
    }
    
    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }
    
    public void SetCulture(string culture)
    {
        try
        {
            _currentCulture = new CultureInfo(culture);
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentCulture;
        }
        catch
        {
            // 如果文化名称无效，保持当前文化
        }
    }
}

/// <summary>
/// 用于在HTTP请求上下文中管理文化信息的中间件
/// </summary>
public class RequestLocalizationMiddleware
{
    private readonly RequestDelegate _next;
    
    public RequestLocalizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ILocalizationService localizationService)
    {
        // 从请求头获取语言设置
        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            // 解析Accept-Language头
            var languages = acceptLanguage.Split(',')
                .Select(lang => lang.Split(';')[0].Trim())
                .ToList();
            
            // 支持的语言
            var supportedCultures = new[] { "zh-CN", "zh", "en-US", "en" };
            
            // 找到第一个支持的语言
            var culture = languages
                .FirstOrDefault(lang => supportedCultures.Contains(lang, StringComparer.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(culture))
            {
                // 标准化文化名称
                culture = culture.ToLower() switch
                {
                    "zh" or "zh-cn" => "zh",
                    "en" or "en-us" => "en",
                    _ => "en"
                };
                
                localizationService.SetCulture(culture);
            }
        }
        
        await _next(context);
    }
}

/// <summary>
/// 扩展方法，用于注册本地化服务
/// </summary>
public static class LocalizationServiceExtensions
{
    public static IServiceCollection AddLocalizationService(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationService, LocalizationService>();
        return services;
    }
    
    public static IApplicationBuilder UseCustomRequestLocalization(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLocalizationMiddleware>();
        return app;
    }
}

