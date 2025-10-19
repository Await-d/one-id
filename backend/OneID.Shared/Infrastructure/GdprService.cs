using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public interface IGdprService
{
    Task<string> ExportUserDataAsync(Guid userId);
    Task DeleteUserDataAsync(Guid userId, bool softDelete = false);
}

public class GdprService : IGdprService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public GdprService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        IAuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// 导出用户数据（GDPR Right to Data Portability）
    /// </summary>
    public async Task<string> ExportUserDataAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // 收集所有用户相关数据
        var userData = new Dictionary<string, object>
        {
            ["personal_information"] = new
            {
                user_id = user.Id,
                username = user.UserName,
                email = user.Email,
                display_name = user.DisplayName,
                email_confirmed = user.EmailConfirmed,
                phone_number = user.PhoneNumber,
                phone_number_confirmed = user.PhoneNumberConfirmed,
                two_factor_enabled = user.TwoFactorEnabled,
                lockout_end = user.LockoutEnd,
                lockout_enabled = user.LockoutEnabled,
                access_failed_count = user.AccessFailedCount
            }
        };

        // 用户角色
        var roles = await _userManager.GetRolesAsync(user);
        userData["roles"] = roles;

        // 用户声明
        var claims = await _userManager.GetClaimsAsync(user);
        userData["claims"] = claims.Select(c => new { c.Type, c.Value }).ToList();

        // 外部登录
        var externalLogins = await _userManager.GetLoginsAsync(user);
        userData["external_logins"] = externalLogins.Select(l => new
        {
            login_provider = l.LoginProvider,
            provider_key = l.ProviderKey,
            provider_display_name = l.ProviderDisplayName
        }).ToList();

        // API密钥（不包含密钥本身，只包含元数据）
        var apiKeys = await _context.ApiKeys
            .Where(k => k.UserId == userId)
            .Select(k => new
            {
                k.Id,
                k.Name,
                k.KeyPrefix,
                k.CreatedAt,
                k.ExpiresAt,
                k.IsRevoked,
                k.RevokedAt,
                k.Scopes
            })
            .ToListAsync();
        userData["api_keys"] = apiKeys;

        // 用户会话
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .Select(s => new
            {
                s.Id,
                s.IpAddress,
                s.DeviceInfo,
                s.BrowserInfo,
                s.OsInfo,
                s.Location,
                s.CreatedAt,
                s.LastActivityAt,
                s.ExpiresAt,
                s.IsRevoked,
                s.RevokedAt
            })
            .ToListAsync();
        userData["sessions"] = sessions;

        // 审计日志
        var auditLogs = await _context.Set<AuditLog>()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(1000) // 限制最近1000条记录
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.Category,
                a.IpAddress,
                a.UserAgent,
                a.Success,
                a.CreatedAt
            })
            .ToListAsync();
        userData["audit_logs"] = auditLogs;

        // 元数据
        userData["export_metadata"] = new
        {
            export_date = DateTime.UtcNow,
            export_format = "JSON",
            data_controller = "OneID Platform",
            purpose = "GDPR Data Portability Request"
        };

        // 记录导出操作
        await _auditLogService.LogAsync(
            action: "ExportUserData",
            category: "Security",
            success: true,
            details: "User data exported for GDPR compliance",
            errorMessage: null,
            userId: userId,
            userName: user.UserName
        );

        // 序列化为JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(userData, options);
    }

    /// <summary>
    /// 删除用户数据（GDPR Right to be Forgotten）
    /// </summary>
    public async Task DeleteUserDataAsync(Guid userId, bool softDelete = false)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        if (softDelete)
        {
            // 软删除：匿名化用户数据
            await AnonymizeUserAsync(user);
        }
        else
        {
            // 硬删除：完全删除用户及相关数据
            await HardDeleteUserAsync(user);
        }

        // 记录删除操作
        await _auditLogService.LogAsync(
            action: softDelete ? "AnonymizeUserData" : "DeleteUserData",
            category: "Security",
            success: true,
            details: $"User data {(softDelete ? "anonymized" : "deleted")} for GDPR compliance",
            errorMessage: null,
            userId: userId,
            userName: user.UserName ?? "DeletedUser"
        );
    }

    private async Task AnonymizeUserAsync(AppUser user)
    {
        // 匿名化个人信息
        var anonymousId = Guid.NewGuid().ToString("N");
        user.UserName = $"deleted_{anonymousId}";
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.Email = $"deleted_{anonymousId}@example.com";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.DisplayName = "Deleted User";
        user.PhoneNumber = null;
        user.EmailConfirmed = false;
        user.PhoneNumberConfirmed = false;
        user.TwoFactorEnabled = false;
        user.TotpSecret = null;
        user.RecoveryCodes = null;

        // 撤销所有API密钥
        var apiKeys = await _context.ApiKeys.Where(k => k.UserId == user.Id).ToListAsync();
        foreach (var key in apiKeys)
        {
            key.IsRevoked = true;
            key.RevokedAt = DateTime.UtcNow;
            key.RevokedReason = "User data anonymized";
        }

        // 撤销所有会话
        var sessions = await _context.UserSessions.Where(s => s.UserId == user.Id).ToListAsync();
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = "User data anonymized";
        }

        // 移除外部登录
        var logins = await _userManager.GetLoginsAsync(user);
        foreach (var login in logins)
        {
            await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        }

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();
    }

    private async Task HardDeleteUserAsync(AppUser user)
    {
        // 删除相关数据（由于设置了Cascade，会自动删除）
        // - API密钥
        // - 用户会话
        // - 外部登录
        // - 用户角色
        // - 用户声明
        
        // 注意：审计日志通常保留以满足合规要求，但会匿名化用户标识
        var auditLogs = await _context.Set<AuditLog>()
            .Where(a => a.UserId == user.Id)
            .ToListAsync();
        
        foreach (var log in auditLogs)
        {
            log.UserName = "DeletedUser";
            // UserId保留用于数据关联，但用户记录已删除
        }

        // 删除用户
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete user: {errors}");
        }

        await _context.SaveChangesAsync();
    }
}

