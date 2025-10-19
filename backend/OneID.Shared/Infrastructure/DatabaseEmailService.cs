using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using SendGrid;
using SendGrid.Helpers.Mail;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// Email service that reads configuration from database
/// </summary>
public class DatabaseEmailService : IEmailService
{
    private readonly ILogger<DatabaseEmailService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private IDataProtector? _protector;

    public DatabaseEmailService(
        ILogger<DatabaseEmailService> logger,
        AppDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider)
    {
        _logger = logger;
        _dbContext = dbContext;
        _dataProtectionProvider = dataProtectionProvider;
    }

    private IDataProtector GetProtector()
    {
        return _protector ??= _dataProtectionProvider.CreateProtector("EmailConfiguration");
    }

    private async Task<EmailConfiguration?> GetActiveConfigurationAsync()
    {
        return await _dbContext.EmailConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.TenantId == null ? 0 : 1) // Prefer global config
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
        var fullUrl = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}";
        var htmlBody = EmailTemplatesI18n.GetPasswordResetTemplate(fullUrl, email, culture);
        var subject = culture.StartsWith("zh") ? "重置密码 - OneID" : "Reset Your Password - OneID";
        
        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken, string confirmationUrl)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
        var fullUrl = $"{confirmationUrl}?token={Uri.EscapeDataString(confirmationToken)}";
        var htmlBody = EmailTemplatesI18n.GetEmailConfirmationTemplate(fullUrl, email, culture);
        var subject = culture.StartsWith("zh") ? "确认邮箱 - OneID" : "Confirm Your Email - OneID";
        
        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
        var htmlBody = EmailTemplatesI18n.GetWelcomeEmailTemplate(userName, "http://localhost:5101/signin", culture);
        var subject = culture.StartsWith("zh") ? "欢迎加入OneID" : "Welcome to OneID!";
        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendMfaEnabledEmailAsync(string email, string userName)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
        var htmlBody = EmailTemplatesI18n.GetMfaEnabledEmailTemplate(userName, culture);
        var subject = culture.StartsWith("zh") ? "双因素认证已启用" : "Multi-Factor Authentication Enabled - OneID";
        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        var config = await GetActiveConfigurationAsync();
        
        if (config == null)
        {
            _logger.LogWarning("No active email configuration found. Email not sent to {To}", to);
            return;
        }

        if (config.Provider.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Email provider is 'None'. Logging email to {To} with subject: {Subject}",
                to, subject);
            return;
        }

        try
        {
            if (config.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
            {
                await SendViaSmtpAsync(config, to, subject, htmlBody, textBody);
            }
            else if (config.Provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
            {
                await SendViaSendGridAsync(config, to, subject, htmlBody, textBody);
            }
            else
            {
                _logger.LogError("Unknown email provider: {Provider}", config.Provider);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    private async Task SendViaSmtpAsync(
        EmailConfiguration config,
        string to,
        string subject,
        string htmlBody,
        string? textBody)
    {
        if (string.IsNullOrEmpty(config.SmtpHost))
        {
            _logger.LogError("SMTP host is not configured");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody ?? "Please view this email in an HTML-capable email client."
        };

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        
        var port = config.SmtpPort ?? 587;
        
        await client.ConnectAsync(
            config.SmtpHost,
            port,
            config.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        if (!string.IsNullOrEmpty(config.SmtpUsername))
        {
            var password = config.DecryptSmtpPassword(GetProtector());
            if (!string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(config.SmtpUsername, password);
            }
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent successfully to {To} via SMTP", to);
    }

    private async Task SendViaSendGridAsync(
        EmailConfiguration config,
        string to,
        string subject,
        string htmlBody,
        string? textBody)
    {
        var apiKey = config.DecryptSendGridApiKey(GetProtector());
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("SendGrid API key is not configured");
            return;
        }

        var client = new SendGridClient(apiKey);
        
        var from = new EmailAddress(config.FromEmail, config.FromName);
        var toAddress = new EmailAddress(to);
        var plainTextContent = textBody ?? "Please view this email in an HTML-capable email client.";
        
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent, htmlBody);
        
        var response = await client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email sent successfully to {To} via SendGrid", to);
        }
        else
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError(
                "Failed to send email to {To} via SendGrid. Status: {Status}, Body: {Body}",
                to, response.StatusCode, body);
            throw new Exception($"SendGrid API returned status code {response.StatusCode}");
        }
    }
}

