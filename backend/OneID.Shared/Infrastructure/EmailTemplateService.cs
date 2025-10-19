using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using System.Text.RegularExpressions;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 邮件模板服务接口
/// </summary>
public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailTemplate>> GetTemplatesByLanguageAsync(string language, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetTemplateByKeyAsync(string templateKey, string language = "en-US", CancellationToken cancellationToken = default);
    Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmailTemplate> DuplicateTemplateAsync(Guid id, string newLanguage, CancellationToken cancellationToken = default);
    Task EnsureDefaultTemplatesAsync(CancellationToken cancellationToken = default);
    
    // 变量替换功能
    string ReplaceVariables(string template, Dictionary<string, string> variables);
    (string subject, string htmlBody, string? textBody) RenderTemplate(EmailTemplate template, Dictionary<string, string> variables);
    List<string> ExtractVariables(string template);
}

/// <summary>
/// 邮件模板服务实现
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<EmailTemplateService> _logger;
    
    // 变量匹配正则表达式：{{variableName}}
    private static readonly Regex VariableRegex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public EmailTemplateService(
        AppDbContext dbContext,
        ILogger<EmailTemplateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailTemplates
            .OrderBy(t => t.TemplateKey)
            .ThenBy(t => t.Language)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetTemplatesByLanguageAsync(
        string language, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailTemplates
            .Where(t => t.Language == language && t.IsActive)
            .OrderBy(t => t.TemplateKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailTemplates.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<EmailTemplate?> GetTemplateByKeyAsync(
        string templateKey, 
        string language = "en-US", 
        CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.EmailTemplates
            .FirstOrDefaultAsync(
                t => t.TemplateKey == templateKey && t.Language == language && t.IsActive, 
                cancellationToken);

        // 如果找不到指定语言的模板，尝试使用英文作为后备
        if (template == null && language != "en-US")
        {
            template = await _dbContext.EmailTemplates
                .FirstOrDefaultAsync(
                    t => t.TemplateKey == templateKey && t.Language == "en-US" && t.IsActive, 
                    cancellationToken);
        }

        return template;
    }

    public async Task<EmailTemplate> CreateTemplateAsync(
        EmailTemplate template, 
        CancellationToken cancellationToken = default)
    {
        // 检查是否已存在相同的 TemplateKey + Language 组合
        var existing = await _dbContext.EmailTemplates
            .AnyAsync(
                t => t.TemplateKey == template.TemplateKey && t.Language == template.Language, 
                cancellationToken);

        if (existing)
        {
            throw new InvalidOperationException(
                $"Template with key '{template.TemplateKey}' and language '{template.Language}' already exists");
        }

        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        
        // 自动提取可用变量
        var variables = ExtractVariables(template.HtmlBody);
        template.AvailableVariables = string.Join(", ", variables);

        _dbContext.EmailTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email template created: {TemplateKey} ({Language})", 
            template.TemplateKey, template.Language);

        return template;
    }

    public async Task<EmailTemplate> UpdateTemplateAsync(
        EmailTemplate template, 
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.EmailTemplates.FindAsync(
            new object[] { template.Id }, cancellationToken);

        if (existing == null)
        {
            throw new InvalidOperationException($"Template with ID '{template.Id}' not found");
        }

        if (existing.IsDefault)
        {
            _logger.LogWarning("Attempting to modify default template: {TemplateKey}", existing.TemplateKey);
            // 允许修改，但记录警告
        }

        existing.Name = template.Name;
        existing.Description = template.Description;
        existing.Subject = template.Subject;
        existing.HtmlBody = template.HtmlBody;
        existing.TextBody = template.TextBody;
        existing.IsActive = template.IsActive;
        existing.LastModifiedBy = template.LastModifiedBy;
        existing.UpdatedAt = DateTime.UtcNow;
        
        // 更新可用变量
        var variables = ExtractVariables(template.HtmlBody);
        existing.AvailableVariables = string.Join(", ", variables);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email template updated: {TemplateKey} ({Language})", 
            existing.TemplateKey, existing.Language);

        return existing;
    }

    public async Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.EmailTemplates.FindAsync(new object[] { id }, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException($"Template with ID '{id}' not found");
        }

        if (template.IsDefault)
        {
            throw new InvalidOperationException("Cannot delete default system template");
        }

        _dbContext.EmailTemplates.Remove(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email template deleted: {TemplateKey} ({Language})", 
            template.TemplateKey, template.Language);
    }

    public async Task<EmailTemplate> DuplicateTemplateAsync(
        Guid id, 
        string newLanguage, 
        CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.EmailTemplates.FindAsync(new object[] { id }, cancellationToken);

        if (source == null)
        {
            throw new InvalidOperationException($"Template with ID '{id}' not found");
        }

        // 检查目标语言是否已存在
        var existing = await _dbContext.EmailTemplates
            .AnyAsync(
                t => t.TemplateKey == source.TemplateKey && t.Language == newLanguage, 
                cancellationToken);

        if (existing)
        {
            throw new InvalidOperationException(
                $"Template with key '{source.TemplateKey}' and language '{newLanguage}' already exists");
        }

        var newTemplate = new EmailTemplate
        {
            TemplateKey = source.TemplateKey,
            Name = $"{source.Name} ({newLanguage})",
            Description = source.Description,
            Subject = source.Subject,
            HtmlBody = source.HtmlBody,
            TextBody = source.TextBody,
            Language = newLanguage,
            IsActive = true,
            IsDefault = false,
            AvailableVariables = source.AvailableVariables,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.EmailTemplates.Add(newTemplate);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email template duplicated: {TemplateKey} from {SourceLang} to {TargetLang}", 
            source.TemplateKey, source.Language, newLanguage);

        return newTemplate;
    }

    public string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return VariableRegex.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            return variables.TryGetValue(variableName, out var value) ? value : match.Value;
        });
    }

    public (string subject, string htmlBody, string? textBody) RenderTemplate(
        EmailTemplate template, 
        Dictionary<string, string> variables)
    {
        var subject = ReplaceVariables(template.Subject, variables);
        var htmlBody = ReplaceVariables(template.HtmlBody, variables);
        var textBody = !string.IsNullOrEmpty(template.TextBody) 
            ? ReplaceVariables(template.TextBody, variables) 
            : null;

        return (subject, htmlBody, textBody);
    }

    public List<string> ExtractVariables(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return new List<string>();
        }

        var matches = VariableRegex.Matches(template);
        return matches
            .Cast<Match>()
            .Select(m => "{{" + m.Groups[1].Value + "}}")
            .Distinct()
            .OrderBy(v => v)
            .ToList();
    }

    public async Task EnsureDefaultTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var defaultTemplates = GetDefaultTemplates();

        foreach (var template in defaultTemplates)
        {
            var existing = await _dbContext.EmailTemplates
                .FirstOrDefaultAsync(
                    t => t.TemplateKey == template.TemplateKey && t.Language == template.Language, 
                    cancellationToken);

            if (existing == null)
            {
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;
                _dbContext.EmailTemplates.Add(template);
                _logger.LogInformation(
                    "Created default email template: {TemplateKey} ({Language})", 
                    template.TemplateKey, template.Language);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private List<EmailTemplate> GetDefaultTemplates()
    {
        return new List<EmailTemplate>
        {
            // Email Confirmation - English
            new EmailTemplate
            {
                TemplateKey = "email-confirmation",
                Name = "Email Confirmation",
                Description = "Sent when a user needs to confirm their email address",
                Subject = "Confirm Your Email Address",
                Language = "en-US",
                IsDefault = true,
                IsActive = true,
                AvailableVariables = "{{userName}}, {{confirmationLink}}, {{expiryTime}}",
                HtmlBody = @"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hi {{userName}},</h2>
    <p>Thank you for registering! Please confirm your email address by clicking the link below:</p>
    <p><a href='{{confirmationLink}}' style='background-color: #4CAF50; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px;'>Confirm Email</a></p>
    <p>This link will expire in {{expiryTime}}.</p>
    <p>If you didn't create this account, please ignore this email.</p>
</body>
</html>"
            },

            // Password Reset - English
            new EmailTemplate
            {
                TemplateKey = "password-reset",
                Name = "Password Reset",
                Description = "Sent when a user requests a password reset",
                Subject = "Reset Your Password",
                Language = "en-US",
                IsDefault = true,
                IsActive = true,
                AvailableVariables = "{{userName}}, {{resetLink}}, {{expiryTime}}",
                HtmlBody = @"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hi {{userName}},</h2>
    <p>We received a request to reset your password. Click the link below to proceed:</p>
    <p><a href='{{resetLink}}' style='background-color: #2196F3; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px;'>Reset Password</a></p>
    <p>This link will expire in {{expiryTime}}.</p>
    <p>If you didn't request this, please ignore this email.</p>
</body>
</html>"
            },

            // Welcome Email - English
            new EmailTemplate
            {
                TemplateKey = "welcome",
                Name = "Welcome Email",
                Description = "Sent after a user successfully registers",
                Subject = "Welcome to OneID!",
                Language = "en-US",
                IsDefault = true,
                IsActive = true,
                AvailableVariables = "{{userName}}, {{loginUrl}}",
                HtmlBody = @"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Welcome {{userName}}!</h2>
    <p>Thank you for joining OneID. Your account has been successfully created.</p>
    <p>You can now log in to access your account:</p>
    <p><a href='{{loginUrl}}' style='background-color: #4CAF50; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px;'>Go to Login</a></p>
</body>
</html>"
            }
        };
    }
}

