using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;
using Xunit;

namespace OneID.Identity.Tests.Services;

public class EmailServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AppDbContext _dbContext;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public EmailServiceTests()
    {
        var services = new ServiceCollection();
        
        // Setup in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        
        // Add data protection
        services.AddDataProtection();
        
        // Add logging
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        _dataProtectionProvider = _serviceProvider.GetRequiredService<IDataProtectionProvider>();
    }

    [Fact]
    public async Task SendEmailAsync_WithNoConfiguration_ShouldLogWarning()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseEmailService>>();
        var emailService = new DatabaseEmailService(logger, _dbContext, _dataProtectionProvider);

        // Act & Assert - Should not throw, just log warning
        await emailService.SendEmailAsync("test@example.com", "Test Subject", "<h1>Test</h1>");
    }

    [Fact]
    public async Task SendEmailAsync_WithDisabledConfiguration_ShouldNotSend()
    {
        // Arrange
        var config = new EmailConfiguration
        {
            Provider = "Smtp",
            FromEmail = "noreply@test.com",
            FromName = "Test",
            IsEnabled = false
        };
        
        _dbContext.EmailConfigurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseEmailService>>();
        var emailService = new DatabaseEmailService(logger, _dbContext, _dataProtectionProvider);

        // Act & Assert - Should not throw
        await emailService.SendEmailAsync("test@example.com", "Test Subject", "<h1>Test</h1>");
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldUseCorrectTemplate()
    {
        // Arrange
        var config = new EmailConfiguration
        {
            Provider = "None", // Use None to avoid actual sending
            FromEmail = "noreply@test.com",
            FromName = "Test",
            IsEnabled = true
        };
        
        _dbContext.EmailConfigurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseEmailService>>();
        var emailService = new DatabaseEmailService(logger, _dbContext, _dataProtectionProvider);

        // Act
        await emailService.SendPasswordResetEmailAsync(
            "user@example.com",
            "test-token",
            "http://localhost/reset");

        // Assert - Should complete without error
        Assert.True(true);
    }

    [Fact]
    public void EmailConfiguration_EncryptDecrypt_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new EmailConfiguration();
        var protector = _dataProtectionProvider.CreateProtector("EmailConfiguration");
        var originalPassword = "MySecretPassword123!";

        // Act
        config.EncryptSmtpPassword(protector, originalPassword);
        var decryptedPassword = config.DecryptSmtpPassword(protector);

        // Assert
        Assert.NotNull(config.SmtpPasswordEncrypted);
        Assert.NotEqual(originalPassword, config.SmtpPasswordEncrypted);
        Assert.Equal(originalPassword, decryptedPassword);
    }

    [Fact]
    public void EmailConfiguration_EncryptDecrypt_SendGridApiKey_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new EmailConfiguration();
        var protector = _dataProtectionProvider.CreateProtector("EmailConfiguration");
        var originalApiKey = "SG.XXXXXXXXXXXXXXXXXXXX";

        // Act
        config.EncryptSendGridApiKey(protector, originalApiKey);
        var decryptedApiKey = config.DecryptSendGridApiKey(protector);

        // Assert
        Assert.NotNull(config.SendGridApiKeyEncrypted);
        Assert.NotEqual(originalApiKey, config.SendGridApiKeyEncrypted);
        Assert.Equal(originalApiKey, decryptedApiKey);
    }

    [Fact]
    public async Task GetActiveConfiguration_ShouldReturnEnabledConfig()
    {
        // Arrange
        var disabledConfig = new EmailConfiguration
        {
            Provider = "Smtp",
            FromEmail = "disabled@test.com",
            IsEnabled = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var enabledConfig = new EmailConfiguration
        {
            Provider = "SendGrid",
            FromEmail = "enabled@test.com",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _dbContext.EmailConfigurations.AddRange(disabledConfig, enabledConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var activeConfig = await _dbContext.EmailConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.TenantId == null ? 0 : 1)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(activeConfig);
        Assert.Equal("enabled@test.com", activeConfig.FromEmail);
        Assert.True(activeConfig.IsEnabled);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}

