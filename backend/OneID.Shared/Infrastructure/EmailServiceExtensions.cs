using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OneID.Shared.Infrastructure;

public static class EmailServiceExtensions
{
    /// <summary>
    /// Add email service that reads configuration from database
    /// </summary>
    public static IServiceCollection AddEmailService(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, DatabaseEmailService>();
        return services;
    }
    
    /// <summary>
    /// Add email service with configuration from appsettings.json (legacy)
    /// </summary>
    public static IServiceCollection AddEmailServiceFromConfig(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind email options from configuration
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        
        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>();
        
        // Register appropriate email service based on provider
        if (emailOptions?.Provider?.ToLower() == "smtp")
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else if (emailOptions?.Provider?.ToLower() == "sendgrid")
        {
            services.AddScoped<IEmailService, SendGridEmailService>();
        }
        else
        {
            // Default to logging service (for development)
            services.AddScoped<IEmailService, LoggingEmailService>();
        }
        
        return services;
    }
}

