using System.Globalization;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 多语言邮件模板
/// </summary>
public static class EmailTemplatesI18n
{
    /// <summary>
    /// 获取密码重置邮件模板
    /// </summary>
    public static string GetPasswordResetTemplate(string resetUrl, string userName = "User", string culture = "en")
    {
        var isChinese = culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        
        var title = isChinese ? "重置密码" : "Reset Password";
        var greeting = isChinese ? $"你好 {userName}," : $"Hello {userName},";
        var description = isChinese 
            ? "我们收到了重置您OneID账户密码的请求。点击下方按钮重置密码："
            : "We received a request to reset the password for your OneID account. Click the button below to reset your password:";
        var buttonText = isChinese ? "重置密码" : "Reset Password";
        var expireNote = isChinese 
            ? "此链接将在1小时后过期。如果您没有请求重置密码，请忽略此邮件。"
            : "This link will expire in 1 hour. If you didn't request a password reset, please ignore this email.";
        var footer = isChinese 
            ? "此邮件由系统自动发送，请勿回复。"
            : "This is an automated email, please do not reply.";
        
        return GetEmailTemplate(title, greeting, description, buttonText, resetUrl, expireNote, footer);
    }
    
    /// <summary>
    /// 获取邮箱验证邮件模板
    /// </summary>
    public static string GetEmailConfirmationTemplate(string confirmUrl, string userName = "User", string culture = "en")
    {
        var isChinese = culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        
        var title = isChinese ? "确认邮箱地址" : "Confirm Email Address";
        var greeting = isChinese ? $"欢迎 {userName}!" : $"Welcome {userName}!";
        var description = isChinese 
            ? "感谢您注册OneID！请点击下方按钮验证您的邮箱地址："
            : "Thank you for signing up for OneID! Please click the button below to verify your email address:";
        var buttonText = isChinese ? "验证邮箱" : "Verify Email";
        var expireNote = isChinese 
            ? "此链接将在24小时后过期。"
            : "This link will expire in 24 hours.";
        var footer = isChinese 
            ? "此邮件由系统自动发送，请勿回复。"
            : "This is an automated email, please do not reply.";
        
        return GetEmailTemplate(title, greeting, description, buttonText, confirmUrl, expireNote, footer);
    }
    
    /// <summary>
    /// 获取欢迎邮件模板
    /// </summary>
    public static string GetWelcomeEmailTemplate(string userName, string loginUrl, string culture = "en")
    {
        var isChinese = culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        
        var title = isChinese ? "欢迎加入OneID" : "Welcome to OneID";
        var greeting = isChinese ? $"你好 {userName}!" : $"Hello {userName}!";
        var description = isChinese 
            ? "您的邮箱已成功验证。您现在可以使用OneID统一身份认证平台的所有功能。"
            : "Your email has been successfully verified. You can now access all features of the OneID unified identity authentication platform.";
        var buttonText = isChinese ? "立即登录" : "Sign In Now";
        var features = isChinese 
            ? "OneID提供：<br>• 安全的单点登录 (SSO)<br>• 双因素认证 (2FA)<br>• 多个外部账户关联<br>• API密钥管理"
            : "OneID provides:<br>• Secure Single Sign-On (SSO)<br>• Two-Factor Authentication (2FA)<br>• Multiple External Account Linking<br>• API Key Management";
        var footer = isChinese 
            ? "此邮件由系统自动发送，请勿回复。"
            : "This is an automated email, please do not reply.";
        
        return GetEmailTemplate(title, greeting, description, buttonText, loginUrl, features, footer);
    }
    
    /// <summary>
    /// 获取MFA启用通知邮件模板
    /// </summary>
    public static string GetMfaEnabledEmailTemplate(string userName, string culture = "en")
    {
        var isChinese = culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        
        var title = isChinese ? "双因素认证已启用" : "Two-Factor Authentication Enabled";
        var greeting = isChinese ? $"你好 {userName}," : $"Hello {userName},";
        var description = isChinese 
            ? "您的OneID账户已成功启用双因素认证。这为您的账户提供了额外的安全保护。"
            : "Two-factor authentication has been successfully enabled for your OneID account. This provides an additional layer of security for your account.";
        var securityNote = isChinese 
            ? "从现在开始，每次登录时您都需要输入验证码。请确保您的身份验证器应用已正确设置。"
            : "From now on, you will need to enter a verification code each time you sign in. Please ensure your authenticator app is properly configured.";
        var supportNote = isChinese 
            ? "如果您没有启用双因素认证，请立即联系我们的支持团队。"
            : "If you didn't enable two-factor authentication, please contact our support team immediately.";
        var footer = isChinese 
            ? "此邮件由系统自动发送，请勿回复。"
            : "This is an automated email, please do not reply.";
        
        return GetSecurityNotificationTemplate(title, greeting, description, securityNote, supportNote, footer);
    }
    
    /// <summary>
    /// 基础邮件模板
    /// </summary>
    private static string GetEmailTemplate(string title, string greeting, string description, string buttonText, string buttonUrl, string note, string footer)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .button {{
            display: inline-block;
            padding: 14px 32px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin: 20px 0;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin: 0; font-size: 28px;"">{title}</h1>
        </div>
        <div class=""content"">
            <p style=""font-size: 16px; margin-bottom: 20px;"">{greeting}</p>
            <p style=""font-size: 14px; color: #666; margin-bottom: 30px;"">{description}</p>
            <div style=""text-align: center;"">
                <a href=""{buttonUrl}"" class=""button"">{buttonText}</a>
            </div>
            <p style=""font-size: 13px; color: #999; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;"">{note}</p>
        </div>
        <div class=""footer"">
            <p style=""margin: 0;"">{footer}</p>
            <p style=""margin: 5px 0 0 0;"">© 2025 OneID. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
    
    /// <summary>
    /// 安全通知邮件模板
    /// </summary>
    private static string GetSecurityNotificationTemplate(string title, string greeting, string description, string securityNote, string supportNote, string footer)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #10b981 0%, #059669 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .security-badge {{
            display: inline-block;
            padding: 8px 16px;
            background: #10b981;
            color: white;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
            margin-bottom: 20px;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin: 0; font-size: 28px;"">{title}</h1>
        </div>
        <div class=""content"">
            <p style=""font-size: 16px; margin-bottom: 20px;"">{greeting}</p>
            <div style=""text-align: center; margin-bottom: 20px;"">
                <span class=""security-badge"">🔒 Security Update</span>
            </div>
            <p style=""font-size: 14px; color: #666; margin-bottom: 20px;"">{description}</p>
            <p style=""font-size: 14px; color: #666; margin-bottom: 20px;"">{securityNote}</p>
            <div style=""background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin-top: 30px;"">
                <p style=""margin: 0; font-size: 13px; color: #856404;"">{supportNote}</p>
            </div>
        </div>
        <div class=""footer"">
            <p style=""margin: 0;"">{footer}</p>
            <p style=""margin: 5px 0 0 0;"">© 2025 OneID. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}

