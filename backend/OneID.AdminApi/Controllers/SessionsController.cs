using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class SessionsController(ISessionManagementService sessionService) : ControllerBase
{
    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserSessions(Guid userId)
    {
        var sessions = await sessionService.GetUserSessionsAsync(userId);
        
        var result = sessions.Select(s => new
        {
            s.Id,
            s.UserId,
            s.IpAddress,
            s.DeviceInfo,
            s.BrowserInfo,
            s.OsInfo,
            s.Location,
            s.CreatedAt,
            s.LastActivityAt,
            s.ExpiresAt,
            s.IsRevoked,
            s.RevokedAt,
            s.RevokedReason,
            IsActive = s.IsActive
        });
        
        return Ok(result);
    }
    
    /// <summary>
    /// 撤销指定会话
    /// </summary>
    [HttpPost("{sessionId:guid}/revoke")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, [FromBody] RevokeSessionRequest request)
    {
        await sessionService.RevokeSessionAsync(sessionId, request.Reason ?? "Manually revoked by admin");
        return Ok(new { Message = "Session revoked successfully" });
    }
    
    /// <summary>
    /// 撤销用户的所有会话
    /// </summary>
    [HttpPost("user/{userId:guid}/revoke-all")]
    public async Task<IActionResult> RevokeAllUserSessions(Guid userId, [FromBody] RevokeSessionRequest request)
    {
        await sessionService.RevokeAllUserSessionsAsync(userId, request.Reason ?? "All sessions revoked by admin");
        return Ok(new { Message = "All user sessions revoked successfully" });
    }
    
    /// <summary>
    /// 清理过期会话
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupExpiredSessions()
    {
        await sessionService.CleanupExpiredSessionsAsync();
        return Ok(new { Message = "Expired sessions cleaned up successfully" });
    }
}

public record RevokeSessionRequest(string? Reason);

