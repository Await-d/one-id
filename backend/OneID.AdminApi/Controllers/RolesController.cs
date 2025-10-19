using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.AdminApi.Services;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// Role management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        return Ok(role);
    }

    /// <summary>
    /// Create new role
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        try
        {
            var role = await _roleService.CreateRoleAsync(request.Name, request.Description);
            return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var success = await _roleService.UpdateRoleAsync(id, request.Name, request.Description);
            
            if (!success)
            {
                return NotFound(new { message = "Role not found" });
            }

            return Ok(new { message = "Role updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _roleService.DeleteRoleAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = "Role not found" });
            }

            return Ok(new { message = "Role deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get users in role
    /// </summary>
    [HttpGet("{id}/users")]
    public async Task<IActionResult> GetUsersInRole(Guid id)
    {
        var users = await _roleService.GetUsersInRoleAsync(id);
        return Ok(users);
    }

    /// <summary>
    /// Add user to role
    /// </summary>
    [HttpPost("{roleId}/users/{userId}")]
    public async Task<IActionResult> AddUserToRole(Guid roleId, Guid userId)
    {
        try
        {
            var success = await _roleService.AddUserToRoleAsync(userId, roleId);
            
            if (!success)
            {
                return NotFound(new { message = "User or role not found" });
            }

            return Ok(new { message = "User added to role successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove user from role
    /// </summary>
    [HttpDelete("{roleId}/users/{userId}")]
    public async Task<IActionResult> RemoveUserFromRole(Guid roleId, Guid userId)
    {
        try
        {
            var success = await _roleService.RemoveUserFromRoleAsync(userId, roleId);
            
            if (!success)
            {
                return NotFound(new { message = "User or role not found" });
            }

            return Ok(new { message = "User removed from role successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get roles for a user
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        var roles = await _roleService.GetUserRolesAsync(userId);
        return Ok(roles);
    }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

