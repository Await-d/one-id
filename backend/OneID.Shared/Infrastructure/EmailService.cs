using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OneID.Shared.Infrastructure;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl);
    Task SendEmailConfirmationAsync(string email, string confirmationToken, string confirmationUrl);
    Task SendWelcomeEmailAsync(string email, string userName);
    Task SendMfaEnabledEmailAsync(string email, string userName);
    Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null);
}

/// <summary>
/// Development-only email service that logs emails to console
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
    {
        var fullUrl = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}";
        _logger.LogInformation(
            "Password reset email for {Email}. Reset URL: {ResetUrl}",
            email, fullUrl);
        
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string email, string confirmationToken, string confirmationUrl)
    {
        var fullUrl = $"{confirmationUrl}?token={Uri.EscapeDataString(confirmationToken)}";
        _logger.LogInformation(
            "Email confirmation for {Email}. Confirmation URL: {ConfirmationUrl}",
            email, fullUrl);

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string userName)
    {
        _logger.LogInformation(
            "Welcome email for {Email} (User: {UserName})",
            email, userName);
        
        return Task.CompletedTask;
    }

    public Task SendMfaEnabledEmailAsync(string email, string userName)
    {
        _logger.LogInformation(
            "MFA enabled notification for {Email} (User: {UserName})",
            email, userName);
        
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        _logger.LogInformation(
            "Sending email to {To} with subject: {Subject}",
            to, subject);

        return Task.CompletedTask;
    }
}

/// <summary>
/// SMTP-based email service using MailKit
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly EmailOptions _options;

    public SmtpEmailService(ILogger<SmtpEmailService> logger, IOptions<EmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
    {
        var fullUrl = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}";
        var htmlBody = EmailTemplates.GetPasswordResetTemplate(fullUrl, email);
        
        await SendEmailAsync(email, "Reset Your Password - OneID", htmlBody);
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken, string confirmationUrl)
    {
        var fullUrl = $"{confirmationUrl}?token={Uri.EscapeDataString(confirmationToken)}";
        var htmlBody = EmailTemplates.GetEmailConfirmationTemplate(fullUrl, email);
        
        await SendEmailAsync(email, "Confirm Your Email - OneID", htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        var htmlBody = EmailTemplates.GetWelcomeTemplate(userName);
        await SendEmailAsync(email, "Welcome to OneID!", htmlBody);
    }

    public async Task SendMfaEnabledEmailAsync(string email, string userName)
    {
        var htmlBody = EmailTemplates.GetMfaEnabledTemplate(userName);
        await SendEmailAsync(email, "Multi-Factor Authentication Enabled - OneID", htmlBody);
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody ?? $"Please view this email in an HTML-capable email client."
            };

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Connect to SMTP server
            await client.ConnectAsync(
                _options.Smtp.Host,
                _options.Smtp.Port,
                _options.Smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_options.Smtp.Username) && !string.IsNullOrEmpty(_options.Smtp.Password))
            {
                await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password);
            }

            // Send email
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To} via SMTP", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SMTP", to);
            throw;
        }
    }
}

/// <summary>
/// SendGrid-based email service
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly EmailOptions _options;
    private readonly SendGridClient _client;

    public SendGridEmailService(ILogger<SendGridEmailService> logger, IOptions<EmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        if (string.IsNullOrEmpty(_options.SendGrid.ApiKey))
        {
            throw new InvalidOperationException("SendGrid API key is not configured");
        }
        
        _client = new SendGridClient(_options.SendGrid.ApiKey);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
    {
        var fullUrl = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}";
        var htmlBody = EmailTemplates.GetPasswordResetTemplate(fullUrl, email);
        
        await SendEmailAsync(email, "Reset Your Password - OneID", htmlBody);
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken, string confirmationUrl)
    {
        var fullUrl = $"{confirmationUrl}?token={Uri.EscapeDataString(confirmationToken)}";
        var htmlBody = EmailTemplates.GetEmailConfirmationTemplate(fullUrl, email);
        
        await SendEmailAsync(email, "Confirm Your Email - OneID", htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        var htmlBody = EmailTemplates.GetWelcomeTemplate(userName);
        await SendEmailAsync(email, "Welcome to OneID!", htmlBody);
    }

    public async Task SendMfaEnabledEmailAsync(string email, string userName)
    {
        var htmlBody = EmailTemplates.GetMfaEnabledTemplate(userName);
        await SendEmailAsync(email, "Multi-Factor Authentication Enabled - OneID", htmlBody);
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var from = new EmailAddress(_options.FromEmail, _options.FromName);
            var toAddress = new EmailAddress(to);
            var plainTextContent = textBody ?? "Please view this email in an HTML-capable email client.";
            
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent, htmlBody);
            
            var response = await _client.SendEmailAsync(msg);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SendGrid", to);
            throw;
        }
    }
}
