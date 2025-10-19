using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// Email configuration management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailConfigurationController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<EmailConfigurationController> _logger;
    private IDataProtector? _protector;

    public EmailConfigurationController(
        AppDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<EmailConfigurationController> logger)
    {
        _dbContext = dbContext;
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
    }

    private IDataProtector GetProtector()
    {
        return _protector ??= _dataProtectionProvider.CreateProtector("EmailConfiguration");
    }

    /// <summary>
    /// Get all email configurations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var configurations = await _dbContext.EmailConfigurations
            .OrderByDescending(c => c.IsEnabled)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync();

        var result = configurations.Select(c => new
        {
            c.Id,
            c.TenantId,
            c.Provider,
            c.FromEmail,
            c.FromName,
            c.SmtpHost,
            c.SmtpPort,
            c.SmtpUseSsl,
            c.SmtpUsername,
            HasSmtpPassword = !string.IsNullOrEmpty(c.SmtpPasswordEncrypted),
            HasSendGridApiKey = !string.IsNullOrEmpty(c.SendGridApiKeyEncrypted),
            c.IsEnabled,
            c.CreatedAt,
            c.UpdatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Get active email configuration
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var config = await _dbContext.EmailConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.TenantId == null ? 0 : 1)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            return NotFound(new { message = "No active email configuration found" });
        }

        return Ok(new
        {
            config.Id,
            config.TenantId,
            config.Provider,
            config.FromEmail,
            config.FromName,
            config.SmtpHost,
            config.SmtpPort,
            config.SmtpUseSsl,
            config.SmtpUsername,
            HasSmtpPassword = !string.IsNullOrEmpty(config.SmtpPasswordEncrypted),
            HasSendGridApiKey = !string.IsNullOrEmpty(config.SendGridApiKeyEncrypted),
            config.IsEnabled,
            config.CreatedAt,
            config.UpdatedAt
        });
    }

    /// <summary>
    /// Get email configuration by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var config = await _dbContext.EmailConfigurations.FindAsync(id);

        if (config == null)
        {
            return NotFound(new { message = "Email configuration not found" });
        }

        return Ok(new
        {
            config.Id,
            config.TenantId,
            config.Provider,
            config.FromEmail,
            config.FromName,
            config.SmtpHost,
            config.SmtpPort,
            config.SmtpUseSsl,
            config.SmtpUsername,
            HasSmtpPassword = !string.IsNullOrEmpty(config.SmtpPasswordEncrypted),
            HasSendGridApiKey = !string.IsNullOrEmpty(config.SendGridApiKeyEncrypted),
            config.IsEnabled,
            config.CreatedAt,
            config.UpdatedAt
        });
    }

    /// <summary>
    /// Create email configuration
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmailConfigurationRequest request)
    {
        var config = new EmailConfiguration
        {
            TenantId = request.TenantId,
            Provider = request.Provider,
            FromEmail = request.FromEmail,
            FromName = request.FromName,
            SmtpHost = request.SmtpHost,
            SmtpPort = request.SmtpPort,
            SmtpUseSsl = request.SmtpUseSsl,
            SmtpUsername = request.SmtpUsername,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };

        // Encrypt sensitive data
        if (!string.IsNullOrEmpty(request.SmtpPassword))
        {
            config.EncryptSmtpPassword(GetProtector(), request.SmtpPassword);
        }

        if (!string.IsNullOrEmpty(request.SendGridApiKey))
        {
            config.EncryptSendGridApiKey(GetProtector(), request.SendGridApiKey);
        }

        _dbContext.EmailConfigurations.Add(config);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Email configuration created with ID {Id} for provider {Provider}", config.Id, config.Provider);

        return CreatedAtAction(nameof(GetById), new { id = config.Id }, new { config.Id });
    }

    /// <summary>
    /// Update email configuration
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmailConfigurationRequest request)
    {
        var config = await _dbContext.EmailConfigurations.FindAsync(id);

        if (config == null)
        {
            return NotFound(new { message = "Email configuration not found" });
        }

        config.TenantId = request.TenantId;
        config.Provider = request.Provider;
        config.FromEmail = request.FromEmail;
        config.FromName = request.FromName;
        config.SmtpHost = request.SmtpHost;
        config.SmtpPort = request.SmtpPort;
        config.SmtpUseSsl = request.SmtpUseSsl;
        config.SmtpUsername = request.SmtpUsername;
        config.IsEnabled = request.IsEnabled;
        config.UpdatedAt = DateTime.UtcNow;

        // Update sensitive data if provided
        if (!string.IsNullOrEmpty(request.SmtpPassword))
        {
            config.EncryptSmtpPassword(GetProtector(), request.SmtpPassword);
        }

        if (!string.IsNullOrEmpty(request.SendGridApiKey))
        {
            config.EncryptSendGridApiKey(GetProtector(), request.SendGridApiKey);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Email configuration {Id} updated", id);

        return Ok(new { message = "Email configuration updated successfully" });
    }

    /// <summary>
    /// Delete email configuration
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var config = await _dbContext.EmailConfigurations.FindAsync(id);

        if (config == null)
        {
            return NotFound(new { message = "Email configuration not found" });
        }

        _dbContext.EmailConfigurations.Remove(config);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Email configuration {Id} deleted", id);

        return Ok(new { message = "Email configuration deleted successfully" });
    }

    /// <summary>
    /// Test email configuration by sending a test email
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestConfiguration(int id, [FromBody] TestEmailRequest request)
    {
        var config = await _dbContext.EmailConfigurations.FindAsync(id);

        if (config == null)
        {
            return NotFound(new { message = "Email configuration not found" });
        }

        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return BadRequest(new { message = "Recipient email address is required" });
        }

        try
        {
            var protector = GetProtector();
            var emailService = CreateEmailServiceFromConfig(config, protector);

            var subject = "OneID - Test Email";
            var htmlBody = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #6b7280; font-size: 12px; margin-top: 30px; }}
                        .success {{ color: #10b981; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>✅ Test Email Successful</h1>
                        </div>
                        <div class='content'>
                            <p class='success'>Congratulations! Your email configuration is working correctly.</p>
                            
                            <div class='info'>
                                <p><strong>Configuration Details:</strong></p>
                                <ul>
                                    <li><strong>Provider:</strong> {config.Provider}</li>
                                    <li><strong>From:</strong> {config.FromName} &lt;{config.FromEmail}&gt;</li>
                                    <li><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                                </ul>
                            </div>

                            <p>If you received this email, your OneID email service is properly configured and ready to send notifications.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated test email from OneID</p>
                            <p>&copy; {DateTime.UtcNow.Year} OneID. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            var textBody = $@"
Test Email Successful

Congratulations! Your email configuration is working correctly.

Configuration Details:
- Provider: {config.Provider}
- From: {config.FromName} <{config.FromEmail}>
- Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

If you received this email, your OneID email service is properly configured and ready to send notifications.

---
This is an automated test email from OneID
© {DateTime.UtcNow.Year} OneID. All rights reserved.
";

            await emailService.SendEmailAsync(request.ToEmail, subject, htmlBody, textBody);

            _logger.LogInformation(
                "Test email sent successfully using configuration {Id} to {Email}", 
                id, 
                request.ToEmail);

            return Ok(new 
            { 
                message = "Test email sent successfully",
                provider = config.Provider,
                from = $"{config.FromName} <{config.FromEmail}>",
                to = request.ToEmail,
                sentAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email using configuration {Id}", id);
            return BadRequest(new { message = $"Failed to send test email: {ex.Message}" });
        }
    }

    private OneID.Shared.Infrastructure.IEmailService CreateEmailServiceFromConfig(
        EmailConfiguration config, 
        IDataProtector protector)
    {
        var loggerFactory = HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();

        return config.Provider switch
        {
            "SMTP" => new OneID.Shared.Infrastructure.SmtpEmailService(
                loggerFactory.CreateLogger<OneID.Shared.Infrastructure.SmtpEmailService>(),
                new Microsoft.Extensions.Options.OptionsWrapper<OneID.Shared.Infrastructure.EmailOptions>(
                    new OneID.Shared.Infrastructure.EmailOptions
                    {
                        Provider = "Smtp",
                        FromEmail = config.FromEmail,
                        FromName = config.FromName,
                        Smtp = new OneID.Shared.Infrastructure.SmtpOptions
                        {
                            Host = config.SmtpHost ?? throw new InvalidOperationException("SMTP host is required"),
                            Port = config.SmtpPort ?? 587,
                            UseSsl = config.SmtpUseSsl,
                            Username = config.SmtpUsername,
                            Password = !string.IsNullOrEmpty(config.SmtpPasswordEncrypted)
                                ? protector.Unprotect(config.SmtpPasswordEncrypted)
                                : null
                        }
                    })),

            "SendGrid" => new OneID.Shared.Infrastructure.SendGridEmailService(
                loggerFactory.CreateLogger<OneID.Shared.Infrastructure.SendGridEmailService>(),
                new Microsoft.Extensions.Options.OptionsWrapper<OneID.Shared.Infrastructure.EmailOptions>(
                    new OneID.Shared.Infrastructure.EmailOptions
                    {
                        Provider = "SendGrid",
                        FromEmail = config.FromEmail,
                        FromName = config.FromName,
                        SendGrid = new OneID.Shared.Infrastructure.SendGridOptions
                        {
                            ApiKey = !string.IsNullOrEmpty(config.SendGridApiKeyEncrypted)
                                ? protector.Unprotect(config.SendGridApiKeyEncrypted)
                                : throw new InvalidOperationException("SendGrid API key is required")
                        }
                    })),

            _ => throw new InvalidOperationException($"Unsupported email provider: {config.Provider}")
        };
    }
}

public class CreateEmailConfigurationRequest
{
    public string? TenantId { get; set; }
    public string Provider { get; set; } = "None";
    public string FromEmail { get; set; } = "noreply@oneid.local";
    public string FromName { get; set; } = "OneID";
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SendGridApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateEmailConfigurationRequest
{
    public string? TenantId { get; set; }
    public string Provider { get; set; } = "None";
    public string FromEmail { get; set; } = "noreply@oneid.local";
    public string FromName { get; set; } = "OneID";
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SendGridApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class TestEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
}

