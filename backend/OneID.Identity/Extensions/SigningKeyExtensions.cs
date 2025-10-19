using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using OneID.Shared.Infrastructure;

namespace OneID.Identity.Extensions;

public static class SigningKeyExtensions
{
    /// <summary>
    /// 从数据库加载签名密钥并配置到 OpenIddict
    /// </summary>
    public static async Task LoadSigningKeysFromDatabaseAsync(
        this WebApplication app,
        ILogger logger)
    {
        using var scope = app.Services.CreateScope();
        var signingKeyService = scope.ServiceProvider.GetRequiredService<ISigningKeyService>();
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var protector = dataProtectionProvider.CreateProtector("SigningKey.PrivateKey");

        // 获取激活的 RSA 密钥
        var rsaKey = await signingKeyService.GetActiveKeyAsync("RSA");
        if (rsaKey != null)
        {
            try
            {
                var privateKeyPem = protector.Unprotect(rsaKey.EncryptedPrivateKey);
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);

                var signingKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: true))
                {
                    KeyId = rsaKey.Id.ToString()
                };

                // 将密钥添加到 OpenIddict
                var serverOptions = scope.ServiceProvider.GetRequiredService<OpenIddictServerOptions>();
                serverOptions.SigningCredentials.Add(new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.RsaSha256));

                logger.LogInformation(
                    "Loaded RSA signing key {KeyId} (Version: {Version}) from database",
                    rsaKey.Id,
                    rsaKey.Version);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load RSA signing key {KeyId} from database", rsaKey.Id);
            }
        }
        else
        {
            logger.LogWarning("No active RSA signing key found in database. Using default development key.");
        }

        // 获取激活的 ECDSA 密钥
        var ecKey = await signingKeyService.GetActiveKeyAsync("EC");
        if (ecKey != null)
        {
            try
            {
                var privateKeyPem = protector.Unprotect(ecKey.EncryptedPrivateKey);
                var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(privateKeyPem);

                var signingKey = new ECDsaSecurityKey(ecdsa)
                {
                    KeyId = ecKey.Id.ToString()
                };

                var algorithm = ecKey.Algorithm switch
                {
                    "ES256" => SecurityAlgorithms.EcdsaSha256,
                    "ES384" => SecurityAlgorithms.EcdsaSha384,
                    "ES512" => SecurityAlgorithms.EcdsaSha512,
                    _ => SecurityAlgorithms.EcdsaSha256
                };

                var serverOptions = scope.ServiceProvider.GetRequiredService<OpenIddictServerOptions>();
                serverOptions.SigningCredentials.Add(new SigningCredentials(signingKey, algorithm));

                logger.LogInformation(
                    "Loaded ECDSA signing key {KeyId} (Version: {Version}, Algorithm: {Algorithm}) from database",
                    ecKey.Id,
                    ecKey.Version,
                    ecKey.Algorithm);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load ECDSA signing key {KeyId} from database", ecKey.Id);
            }
        }

        // 检查是否需要轮换密钥
        var shouldRotate = await signingKeyService.ShouldRotateKeyAsync("RSA", warningDays: 30);
        if (shouldRotate)
        {
            logger.LogWarning(
                "RSA signing key rotation recommended. Please generate and activate a new key via Admin Portal.");
        }
    }
}

