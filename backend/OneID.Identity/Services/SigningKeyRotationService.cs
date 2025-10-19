using OneID.Shared.Infrastructure;

namespace OneID.Identity.Services;

/// <summary>
/// 签名密钥自动轮换后台服务
/// </summary>
public class SigningKeyRotationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SigningKeyRotationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(12); // 每12小时检查一次
    private readonly int _warningDays = 30; // 过期前30天开始警告
    private readonly int _autoRotateDays = 7; // 过期前7天自动生成新密钥

    public SigningKeyRotationService(
        IServiceProvider serviceProvider,
        ILogger<SigningKeyRotationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Signing Key Rotation Service started");

        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRotateKeysAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking signing key rotation");
            }

            // 等待下次检查
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Signing Key Rotation Service stopped");
    }

    private async Task CheckAndRotateKeysAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var signingKeyService = scope.ServiceProvider.GetRequiredService<ISigningKeyService>();

        // 检查 RSA 密钥
        await CheckAndRotateKeyTypeAsync(signingKeyService, "RSA", 2048, cancellationToken);

        // 检查 ECDSA 密钥（如果启用）
        // await CheckAndRotateKeyTypeAsync(signingKeyService, "EC", "P-256", cancellationToken);
    }

    private async Task CheckAndRotateKeyTypeAsync(
        ISigningKeyService signingKeyService,
        string keyType,
        object keyParameter, // RSA: keySize (int), EC: curve (string)
        CancellationToken cancellationToken)
    {
        var activeKey = await signingKeyService.GetActiveKeyAsync(keyType, cancellationToken);

        if (activeKey == null)
        {
            _logger.LogWarning("No active {KeyType} key found. Please generate and activate a key manually.", keyType);
            return;
        }

        var daysUntilExpiry = (activeKey.ExpiresAt - DateTime.UtcNow)?.TotalDays ?? 0;

        // 发出警告
        if (daysUntilExpiry <= _warningDays && daysUntilExpiry > _autoRotateDays)
        {
            _logger.LogWarning(
                "{KeyType} signing key {KeyId} (Version: {Version}) will expire in {Days} days ({ExpiresAt}). Please plan key rotation.",
                keyType,
                activeKey.Id,
                activeKey.Version,
                (int)daysUntilExpiry,
                activeKey.ExpiresAt);
        }

        // 自动生成新密钥
        if (daysUntilExpiry <= _autoRotateDays)
        {
            _logger.LogWarning(
                "{KeyType} signing key {KeyId} (Version: {Version}) will expire soon in {Days} days. Generating new key...",
                keyType,
                activeKey.Id,
                activeKey.Version,
                (int)daysUntilExpiry);

            try
            {
                // 生成新密钥
                var newKey = keyType switch
                {
                    "RSA" => await signingKeyService.GenerateRsaKeyAsync(
                        keySize: (int)keyParameter,
                        validityDays: 90,
                        notes: $"Auto-generated to replace key {activeKey.Id} (Version {activeKey.Version})",
                        cancellationToken: cancellationToken),
                    "EC" => await signingKeyService.GenerateEcdsaKeyAsync(
                        curve: (string)keyParameter,
                        validityDays: 90,
                        notes: $"Auto-generated to replace key {activeKey.Id} (Version {activeKey.Version})",
                        cancellationToken: cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown key type: {keyType}")
                };

                _logger.LogInformation(
                    "New {KeyType} signing key {KeyId} (Version: {Version}) generated successfully. Please activate it manually via Admin Portal.",
                    keyType,
                    newKey.Id,
                    newKey.Version);

                // 注意：不自动激活新密钥，需要管理员手动激活
                // 这样可以避免在生产环境中出现意外的密钥切换
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate new {KeyType} signing key", keyType);
            }
        }

        // 清理过期密钥
        if (daysUntilExpiry < -30) // 过期超过30天后清理
        {
            try
            {
                var cleanedCount = await signingKeyService.CleanupExpiredKeysAsync(retentionDays: 30, cancellationToken);
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired signing keys", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired signing keys");
            }
        }
    }
}

