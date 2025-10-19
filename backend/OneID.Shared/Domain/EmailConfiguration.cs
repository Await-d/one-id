using Microsoft.AspNetCore.DataProtection;

namespace OneID.Shared.Domain;

/// <summary>
/// Email configuration stored in database
/// </summary>
public class EmailConfiguration
{
    public int Id { get; set; }
    
    /// <summary>
    /// Tenant ID for multi-tenancy support (null for global configuration)
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Email provider: None, Smtp, SendGrid
    /// </summary>
    public string Provider { get; set; } = "None";
    
    /// <summary>
    /// From email address
    /// </summary>
    public string FromEmail { get; set; } = "noreply@oneid.local";
    
    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = "OneID";
    
    // SMTP Settings
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    public string? SmtpUsername { get; set; }
    
    /// <summary>
    /// Encrypted SMTP password
    /// </summary>
    public string? SmtpPasswordEncrypted { get; set; }
    
    // SendGrid Settings
    /// <summary>
    /// Encrypted SendGrid API key
    /// </summary>
    public string? SendGridApiKeyEncrypted { get; set; }
    
    /// <summary>
    /// Is this configuration enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Decrypt SMTP password
    /// </summary>
    public string? DecryptSmtpPassword(IDataProtector protector)
    {
        if (string.IsNullOrEmpty(SmtpPasswordEncrypted))
            return null;
        
        try
        {
            return protector.Unprotect(SmtpPasswordEncrypted);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Encrypt SMTP password
    /// </summary>
    public void EncryptSmtpPassword(IDataProtector protector, string password)
    {
        SmtpPasswordEncrypted = protector.Protect(password);
    }
    
    /// <summary>
    /// Decrypt SendGrid API key
    /// </summary>
    public string? DecryptSendGridApiKey(IDataProtector protector)
    {
        if (string.IsNullOrEmpty(SendGridApiKeyEncrypted))
            return null;
        
        try
        {
            return protector.Unprotect(SendGridApiKeyEncrypted);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Encrypt SendGrid API key
    /// </summary>
    public void EncryptSendGridApiKey(IDataProtector protector, string apiKey)
    {
        SendGridApiKeyEncrypted = protector.Protect(apiKey);
    }
}

