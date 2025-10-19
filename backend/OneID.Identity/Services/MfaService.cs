using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using QRCoder;

namespace OneID.Identity.Services;

public interface IMfaService
{
    string GenerateSecret();
    string GenerateQrCodeUrl(string email, string secret, string issuer);
    byte[] GenerateQrCode(string qrCodeUrl);
    bool ValidateTotp(string secret, string code);
    string[] GenerateRecoveryCodes(int count = 10);
    string EncryptSecret(string secret);
    string DecryptSecret(string encryptedSecret);
    string EncryptRecoveryCodes(string[] codes);
    string[] DecryptRecoveryCodes(string encryptedCodes);
}

public class MfaService : IMfaService
{
    private readonly IDataProtector _protector;
    private const string Issuer = "OneID";

    public MfaService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("MfaService.TotpSecret");
    }

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20); // 160 bits
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUrl(string email, string secret, string issuer = Issuer)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }

    public byte[] GenerateQrCode(string qrCodeUrl)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    public bool ValidateTotp(string secret, string code)
    {
        try
        {
            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key);
            
            // Allow 1 time step before and after for clock skew
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public string[] GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var code = Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .ToUpperInvariant();
            codes.Add(code.Insert(4, "-")); // Format: XXXX-XXXX
        }
        return codes.ToArray();
    }

    public string EncryptSecret(string secret)
    {
        return _protector.Protect(secret);
    }

    public string DecryptSecret(string encryptedSecret)
    {
        return _protector.Unprotect(encryptedSecret);
    }

    public string EncryptRecoveryCodes(string[] codes)
    {
        var json = JsonSerializer.Serialize(codes);
        return _protector.Protect(json);
    }

    public string[] DecryptRecoveryCodes(string encryptedCodes)
    {
        var json = _protector.Unprotect(encryptedCodes);
        return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
    }
}
