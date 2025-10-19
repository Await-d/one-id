using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Domain;

namespace OneID.Shared.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<EmailConfiguration> EmailConfigurations => Set<EmailConfiguration>();
    public DbSet<ClientValidationSetting> ClientValidationSettings => Set<ClientValidationSetting>();
    public DbSet<CorsSetting> CorsSettings => Set<CorsSetting>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<SigningKey> SigningKeys => Set<SigningKey>();
    public DbSet<SecurityRule> SecurityRules => Set<SecurityRule>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IpAccessRule> IpAccessRules => Set<IpAccessRule>();
    public DbSet<LoginTimeRestriction> LoginTimeRestrictions => Set<LoginTimeRestriction>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // 在开发环境中禁用pending model changes警告
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 使用默认的public schema而不是自定义schema
        // builder.HasDefaultSchema("oneid");

        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(128);
            entity.Property(u => u.TotpSecret).HasMaxLength(1000); // Encrypted
            entity.Property(u => u.RecoveryCodes).HasMaxLength(2000); // Encrypted JSON array
        });

        builder.UseOpenIddict();

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Category);
        });

        builder.Entity<ExternalAuthProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ClientSecret).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CallbackPath).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Enabled);
            entity.HasIndex(e => e.TenantId);
        });

        builder.Entity<ClientValidationSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllowedSchemes).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AllowedHosts).HasMaxLength(2000);
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        builder.Entity<CorsSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllowedOrigins).HasMaxLength(4000);
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        builder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(64); // SHA256 hash
            entity.Property(e => e.KeyPrefix).HasMaxLength(20);
            entity.Property(e => e.RevokedReason).HasMaxLength(500);
            entity.Property(e => e.Scopes).HasMaxLength(2000);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsRevoked);
        });

        builder.Entity<EmailConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FromEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FromName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SmtpHost).HasMaxLength(256);
            entity.Property(e => e.SmtpUsername).HasMaxLength(256);
            entity.Property(e => e.SmtpPasswordEncrypted).HasMaxLength(1000);
            entity.Property(e => e.SendGridApiKeyEncrypted).HasMaxLength(1000);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsEnabled);
        });

        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionTokenHash).IsRequired().HasMaxLength(64); // SHA256 hash
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.DeviceInfo).HasMaxLength(200);
            entity.Property(e => e.BrowserInfo).HasMaxLength(200);
            entity.Property(e => e.OsInfo).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.RevokedReason).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionTokenHash).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsRevoked);
            entity.HasIndex(e => e.TenantId);
        });

        builder.Entity<SigningKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Use).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Algorithm).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EncryptedPrivateKey).IsRequired();
            entity.Property(e => e.PublicKey).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Version);
        });

        builder.Entity<SecurityRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RuleValue).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasIndex(e => e.RuleType);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.TenantId);
        });

        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Domain).HasMaxLength(100);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => e.IsActive);
        });

        builder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Group).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ValueType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(450);
            
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.Group);
            entity.HasIndex(e => e.SortOrder);
        });

        builder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HtmlBody).IsRequired();
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.AvailableVariables).HasMaxLength(2000);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(450);
            
            entity.HasIndex(e => new { e.TemplateKey, e.Language }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        builder.Entity<IpAccessRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TargetRoleName).HasMaxLength(256);
            entity.Property(e => e.CreatedBy).HasMaxLength(450);
            
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => new { e.RuleType, e.IsEnabled });
            entity.HasIndex(e => e.Priority);
        });

        builder.Entity<LoginTimeRestriction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AllowedDaysOfWeek).HasMaxLength(50);
            entity.Property(e => e.DailyStartTime).HasMaxLength(5);
            entity.Property(e => e.DailyEndTime).HasMaxLength(5);
            entity.Property(e => e.TimeZone).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TargetRoleName).HasMaxLength(256);
            entity.Property(e => e.CreatedBy).HasMaxLength(450);
            
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.Priority);
        });

        builder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.AnomalyReason).HasMaxLength(500);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LoginTime);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.IsAnomalous);
            entity.HasIndex(e => new { e.UserId, e.LoginTime });
        });

        builder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceFingerprint).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DeviceName).HasMaxLength(200);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.BrowserVersion).HasMaxLength(50);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.OsVersion).HasMaxLength(50);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.ScreenResolution).HasMaxLength(50);
            entity.Property(e => e.TimeZone).HasMaxLength(100);
            entity.Property(e => e.Language).HasMaxLength(50);
            entity.Property(e => e.Platform).HasMaxLength(100);
            entity.Property(e => e.LastIpAddress).HasMaxLength(50);
            entity.Property(e => e.LastLocation).HasMaxLength(200);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.DeviceFingerprint }).IsUnique();
            entity.HasIndex(e => e.LastUsedAt);
            entity.HasIndex(e => new { e.UserId, e.IsTrusted });
        });

        builder.Entity<Webhook>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Events).IsRequired();
            entity.Property(e => e.Secret).HasMaxLength(256);
            
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
        });

        builder.Entity<WebhookLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Response).HasMaxLength(4000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            
            entity.HasIndex(e => e.WebhookId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.WebhookId, e.CreatedAt });
            
            entity.HasOne(e => e.Webhook)
                  .WithMany()
                  .HasForeignKey(e => e.WebhookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
