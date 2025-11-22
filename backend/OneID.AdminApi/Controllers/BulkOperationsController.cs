using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BulkOperationsController : ControllerBase
{
    private readonly IBulkUserOperationsService _bulkOperationsService;
    private readonly ILogger<BulkOperationsController> _logger;

    public BulkOperationsController(
        IBulkUserOperationsService bulkOperationsService,
        ILogger<BulkOperationsController> logger)
    {
        _bulkOperationsService = bulkOperationsService;
        _logger = logger;
    }

    /// <summary>
    /// 批量分配角色
    /// </summary>
    [HttpPost("assign-roles")]
    public async Task<ActionResult<BulkOperationResult>> AssignRoles([FromBody] AssignRolesRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        if (request.RoleNames == null || !request.RoleNames.Any())
        {
            return BadRequest(new { message = "Role names are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.AssignRolesToUsersAsync(
            request.UserIds, 
            request.RoleNames, 
            operatedBy);

        _logger.LogInformation(
            "Bulk assign roles operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量撤销角色
    /// </summary>
    [HttpPost("remove-roles")]
    public async Task<ActionResult<BulkOperationResult>> RemoveRoles([FromBody] RemoveRolesRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        if (request.RoleNames == null || !request.RoleNames.Any())
        {
            return BadRequest(new { message = "Role names are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.RemoveRolesFromUsersAsync(
            request.UserIds, 
            request.RoleNames, 
            operatedBy);

        _logger.LogInformation(
            "Bulk remove roles operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    [HttpPost("enable-users")]
    public async Task<ActionResult<BulkOperationResult>> EnableUsers([FromBody] BulkUsersRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.EnableUsersAsync(request.UserIds, operatedBy);

        _logger.LogInformation(
            "Bulk enable users operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量禁用用户
    /// </summary>
    [HttpPost("disable-users")]
    public async Task<ActionResult<BulkOperationResult>> DisableUsers([FromBody] BulkUsersRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.DisableUsersAsync(request.UserIds, operatedBy);

        _logger.LogInformation(
            "Bulk disable users operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量锁定用户
    /// </summary>
    [HttpPost("lock-users")]
    public async Task<ActionResult<BulkOperationResult>> LockUsers([FromBody] LockUsersRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        var operatedBy = User.Identity?.Name;
        var lockoutEnd = request.LockoutEndUtc.HasValue 
            ? new DateTimeOffset(request.LockoutEndUtc.Value) 
            : (DateTimeOffset?)null;

        var result = await _bulkOperationsService.LockUsersAsync(request.UserIds, lockoutEnd, operatedBy);

        _logger.LogInformation(
            "Bulk lock users operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量解锁用户
    /// </summary>
    [HttpPost("unlock-users")]
    public async Task<ActionResult<BulkOperationResult>> UnlockUsers([FromBody] BulkUsersRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.UnlockUsersAsync(request.UserIds, operatedBy);

        _logger.LogInformation(
            "Bulk unlock users operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量撤销用户会话
    /// </summary>
    [HttpPost("revoke-sessions")]
    public async Task<ActionResult<BulkOperationResult>> RevokeSessions([FromBody] BulkUsersRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.RevokeUserSessionsAsync(request.UserIds, operatedBy);

        _logger.LogInformation(
            "Bulk revoke sessions operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量重置密码
    /// </summary>
    [HttpPost("reset-passwords")]
    public async Task<ActionResult<BulkOperationResult>> ResetPasswords([FromBody] ResetPasswordsRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.ResetPasswordsAsync(
            request.UserIds, 
            request.SendEmail, 
            operatedBy);

        _logger.LogInformation(
            "Bulk reset passwords operation completed: {SuccessCount}/{TotalCount} successful",
            result.SuccessCount, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    [HttpPost("delete-users")]
    public async Task<ActionResult<BulkOperationResult>> DeleteUsers([FromBody] BulkUsersRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return BadRequest(new { message = "User IDs are required" });
        }

        var operatedBy = User.Identity?.Name;
        var result = await _bulkOperationsService.DeleteUsersAsync(request.UserIds, operatedBy);

        _logger.LogWarning(
            "Bulk delete users operation completed: {SuccessCount}/{TotalCount} successful by {Operator}",
            result.SuccessCount, result.TotalCount, operatedBy);

        return Ok(result);
    }
}

#region Request DTOs

public class BulkUsersRequest
{
    public List<Guid> UserIds { get; set; } = new();
}

public class AssignRolesRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
}

public class RemoveRolesRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
}

public class LockUsersRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public DateTime? LockoutEndUtc { get; set; }
}

public class ResetPasswordsRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public bool SendEmail { get; set; } = true;
}

#endregion

