using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public interface ISessionManagementService
{
    Task<UserSession> CreateSessionAsync(Guid userId, string sessionToken, string? ipAddress, string? userAgent);
    Task<List<UserSession>> GetUserSessionsAsync(Guid userId);
    Task<UserSession?> GetSessionByTokenAsync(string sessionToken);
    Task RevokeSessionAsync(Guid sessionId, string reason);
    Task RevokeAllUserSessionsAsync(Guid userId, string reason);
    Task UpdateSessionActivityAsync(string sessionToken);
    Task CleanupExpiredSessionsAsync();
}

public class SessionManagementService : ISessionManagementService
{
    private readonly AppDbContext _context;

    public SessionManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserSession> CreateSessionAsync(Guid userId, string sessionToken, string? ipAddress, string? userAgent)
    {
        var tokenHash = HashToken(sessionToken);
        var (deviceInfo, browserInfo, osInfo) = ParseUserAgent(userAgent);
        
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionTokenHash = tokenHash,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo,
            BrowserInfo = browserInfo,
            OsInfo = osInfo,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // 默认30天过期
            IsRevoked = false
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<List<UserSession>> GetUserSessionsAsync(Guid userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }

    public async Task<UserSession?> GetSessionByTokenAsync(string sessionToken)
    {
        var tokenHash = HashToken(sessionToken);
        return await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionTokenHash == tokenHash);
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId, string reason)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateSessionActivityAsync(string sessionToken)
    {
        var tokenHash = HashToken(sessionToken);
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionTokenHash == tokenHash);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _context.UserSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.UserSessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static (string? deviceInfo, string? browserInfo, string? osInfo) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return (null, null, null);
        }

        // 简单的User-Agent解析逻辑
        string? browserInfo = null;
        string? osInfo = null;
        string? deviceInfo = null;

        // 检测浏览器
        if (userAgent.Contains("Firefox"))
            browserInfo = "Firefox";
        else if (userAgent.Contains("Edg"))
            browserInfo = "Edge";
        else if (userAgent.Contains("Chrome"))
            browserInfo = "Chrome";
        else if (userAgent.Contains("Safari"))
            browserInfo = "Safari";

        // 检测操作系统
        if (userAgent.Contains("Windows"))
            osInfo = "Windows";
        else if (userAgent.Contains("Mac OS"))
            osInfo = "macOS";
        else if (userAgent.Contains("Linux"))
            osInfo = "Linux";
        else if (userAgent.Contains("Android"))
            osInfo = "Android";
        else if (userAgent.Contains("iOS"))
            osInfo = "iOS";

        // 检测设备类型
        if (userAgent.Contains("Mobile"))
            deviceInfo = "Mobile";
        else if (userAgent.Contains("Tablet"))
            deviceInfo = "Tablet";
        else
            deviceInfo = "Desktop";

        return (deviceInfo, browserInfo, osInfo);
    }
}

