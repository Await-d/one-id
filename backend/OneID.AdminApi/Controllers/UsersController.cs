using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Application.Users;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService,
    ILogger<UsersController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await userQueryService.ListAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserSummary>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userQueryService.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<UserSummary>> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        var user = await userQueryService.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// 创建新用户
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserSummary>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await userCommandService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to create user");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<UserSummary>> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await userCommandService.UpdateAsync(userId, request, cancellationToken);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to update user {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await userCommandService.DeleteAsync(userId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    [HttpPost("{userId:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(
        Guid userId,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await userCommandService.ChangePasswordAsync(userId, request, cancellationToken);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to change password for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    [HttpPost("{userId:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await userCommandService.UnlockUserAsync(userId, cancellationToken);
            return Ok(new { message = "User unlocked successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to unlock user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
