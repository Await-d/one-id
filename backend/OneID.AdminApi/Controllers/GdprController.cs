using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class GdprController(IGdprService gdprService) : ControllerBase
{
    /// <summary>
    /// 导出用户数据（GDPR数据可携带权）
    /// </summary>
    [HttpGet("users/{userId:guid}/export")]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<IActionResult> ExportUserData(Guid userId)
    {
        try
        {
            var json = await gdprService.ExportUserDataAsync(userId);
            
            // 返回JSON文件
            var fileName = $"user_data_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            return File(
                System.Text.Encoding.UTF8.GetBytes(json),
                "application/json",
                fileName
            );
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// 删除用户数据（GDPR被遗忘权）
    /// </summary>
    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUserData(Guid userId, [FromQuery] bool softDelete = false)
    {
        try
        {
            await gdprService.DeleteUserDataAsync(userId, softDelete);
            
            return Ok(new
            {
                Message = softDelete 
                    ? "User data has been anonymized successfully" 
                    : "User data has been deleted successfully",
                UserId = userId,
                DeleteType = softDelete ? "Soft Delete (Anonymized)" : "Hard Delete",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}

