namespace OneID.Shared.Infrastructure;

public class EmailOptions
{
    public const string SectionName = "Email";
    
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
    
    /// <summary>
    /// SMTP settings
    /// </summary>
    public SmtpOptions Smtp { get; set; } = new();
    
    /// <summary>
    /// SendGrid settings
    /// </summary>
    public SendGridOptions SendGrid { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class SendGridOptions
{
    public string? ApiKey { get; set; }
}

