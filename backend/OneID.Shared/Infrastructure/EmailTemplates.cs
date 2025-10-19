namespace OneID.Shared.Infrastructure;

public static class EmailTemplates
{
    public static string GetPasswordResetTemplate(string resetUrl, string userName = "User")
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
            border-top: 1px solid #dee2e6;
        }}
        .warning {{
            background: #fff3cd;
            border: 1px solid #ffc107;
            border-radius: 4px;
            padding: 12px;
            margin: 20px 0;
            color: #856404;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin: 0; font-size: 28px;"">🔐 Password Reset</h1>
        </div>
        <div class=""content"">
            <p>Hello {userName},</p>
            <p>We received a request to reset your password for your OneID account.</p>
            <p style=""text-align: center;"">
                <a href=""{resetUrl}"" class=""button"">Reset Password</a>
            </p>
            <p style=""color: #6c757d; font-size: 14px;"">
                Or copy and paste this link into your browser:<br>
                <a href=""{resetUrl}"" style=""color: #667eea; word-break: break-all;"">{resetUrl}</a>
            </p>
            <div class=""warning"">
                <strong>⚠️ Security Notice:</strong><br>
                This link will expire in 1 hour. If you didn't request a password reset, please ignore this email.
            </div>
        </div>
        <div class=""footer"">
            <p>This is an automated message from OneID. Please do not reply to this email.</p>
            <p>&copy; {DateTime.UtcNow.Year} OneID. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    public static string GetEmailConfirmationTemplate(string confirmationUrl, string userName = "User")
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
            border-top: 1px solid #dee2e6;
        }}
        .icon {{
            font-size: 48px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin: 0; font-size: 28px;"">✉️ Confirm Your Email</h1>
        </div>
        <div class=""content"">
            <p>Hello {userName},</p>
            <p>Thank you for registering with OneID! To complete your registration, please verify your email address.</p>
            <p style=""text-align: center;"">
                <a href=""{confirmationUrl}"" class=""button"">Confirm Email Address</a>
            </p>
            <p style=""color: #6c757d; font-size: 14px;"">
                Or copy and paste this link into your browser:<br>
                <a href=""{confirmationUrl}"" style=""color: #667eea; word-break: break-all;"">{confirmationUrl}</a>
            </p>
            <p style=""color: #6c757d; font-size: 14px; margin-top: 30px;"">
                If you didn't create an account with OneID, you can safely ignore this email.
            </p>
        </div>
        <div class=""footer"">
            <p>This is an automated message from OneID. Please do not reply to this email.</p>
            <p>&copy; {DateTime.UtcNow.Year} OneID. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    public static string GetWelcomeTemplate(string userName)
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
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
            border-top: 1px solid #dee2e6;
        }}
        .feature {{
            margin: 20px 0;
            padding: 15px;
            background: #f8f9fa;
            border-radius: 6px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin: 0; font-size: 28px;"">🎉 Welcome to OneID!</h1>
        </div>
        <div class=""content"">
            <p>Hello {userName},</p>
            <p>Your email has been successfully verified! Welcome to OneID - your unified identity authentication platform.</p>
            
            <div class=""feature"">
                <h3 style=""margin-top: 0;"">🔐 Single Sign-On</h3>
                <p style=""margin-bottom: 0;"">Access all your applications with one secure account.</p>
            </div>
            
            <div class=""feature"">
                <h3 style=""margin-top: 0;"">🛡️ Multi-Factor Authentication</h3>
                <p style=""margin-bottom: 0;"">Enable 2FA for enhanced security.</p>
            </div>
            
            <div class=""feature"">
                <h3 style=""margin-top: 0;"">🔑 API Key Management</h3>
                <p style=""margin-bottom: 0;"">Generate API keys for programmatic access.</p>
            </div>
            
            <p style=""margin-top: 30px;"">Need help? Check out our documentation or contact support.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message from OneID. Please do not reply to this email.</p>
            <p>&copy; {DateTime.UtcNow.Year} OneID. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    public static string GetMfaEnabledTemplate(string userName)
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
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
            border-top: 1px solid #dee2e6;
        }}
        .info {{
            background: #d1fae5;
            border: 1px solid #10b981;
            border-radius: 4px;
            padding: 12px;
            margin: 20px 0;
            color: #065f46;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin: 0; font-size: 28px;"">🛡️ MFA Enabled</h1>
        </div>
        <div class=""content"">
            <p>Hello {userName},</p>
            <p>Multi-factor authentication has been successfully enabled on your OneID account.</p>
            <div class=""info"">
                <strong>✅ Your account is now more secure!</strong><br>
                You'll need to enter a verification code from your authenticator app when signing in.
            </div>
            <p><strong>Important:</strong> Make sure you've saved your recovery codes in a safe place. You'll need them if you lose access to your authenticator app.</p>
            <p>If you didn't enable MFA, please contact support immediately.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message from OneID. Please do not reply to this email.</p>
            <p>&copy; {DateTime.UtcNow.Year} OneID. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}

