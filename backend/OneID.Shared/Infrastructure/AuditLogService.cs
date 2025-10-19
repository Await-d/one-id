using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public interface IAuditLogService
{
    Task LogAsync(string action, string category, bool success, string? details = null, string? errorMessage = null, Guid? userId = null, string? userName = null);
}

public class AuditLogService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor, ILogger<AuditLogService> logger) : IAuditLogService
{
    public async Task LogAsync(
        string action,
        string category,
        bool success,
        string? details = null,
        string? errorMessage = null,
        Guid? userId = null,
        string? userName = null)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            if (userId == null && httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            if (userName == null && httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                userName = httpContext.User.Identity.Name;
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                Category = category,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Set<AuditLog>().Add(auditLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for action {Action}", action);
        }
    }
}
