using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Services;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId);
    Task<RoleDto> CreateRoleAsync(string name, string? description);
    Task<bool> UpdateRoleAsync(Guid roleId, string name, string? description);
    Task<bool> DeleteRoleAsync(Guid roleId);
    Task<List<UserRoleDto>> GetUsersInRoleAsync(Guid roleId);
    Task<bool> AddUserToRoleAsync(Guid userId, Guid roleId);
    Task<bool> RemoveUserFromRoleAsync(Guid userId, Guid roleId);
    Task<List<string>> GetUserRolesAsync(Guid userId);
}

public class RoleService : IRoleService
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        
        var roleDtos = new List<RoleDto>();
        foreach (var role in roles)
        {
            var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                NormalizedName = role.NormalizedName!,
                Description = role.Description,
                UserCount = userCount
            });
        }
        
        return roleDtos;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return null;

        var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
        
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            NormalizedName = role.NormalizedName!,
            Description = role.Description,
            UserCount = userCount
        };
    }

    public async Task<RoleDto> CreateRoleAsync(string name, string? description)
    {
        var role = new AppRole
        {
            Name = name,
            Description = description
        };

        var result = await _roleManager.CreateAsync(role);
        
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("Role {RoleName} created with ID {RoleId}", name, role.Id);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.NormalizedName!,
            Description = role.Description,
            UserCount = 0
        };
    }

    public async Task<bool> UpdateRoleAsync(Guid roleId, string name, string? description)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return false;

        role.Name = name;
        role.Description = description;

        var result = await _roleManager.UpdateAsync(role);
        
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to update role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("Role {RoleId} updated", roleId);
        return true;
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return false;

        var result = await _roleManager.DeleteAsync(role);
        
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to delete role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("Role {RoleId} deleted", roleId);
        return true;
    }

    public async Task<List<UserRoleDto>> GetUsersInRoleAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return new List<UserRoleDto>();

        var users = await _userManager.GetUsersInRoleAsync(role.Name!);
        
        return users.Select(u => new UserRoleDto
        {
            UserId = u.Id,
            UserName = u.UserName!,
            Email = u.Email!,
            DisplayName = u.DisplayName
        }).ToList();
    }

    public async Task<bool> AddUserToRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return false;

        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to add user to role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("User {UserId} added to role {RoleName}", userId, role.Name);
        return true;
    }

    public async Task<bool> RemoveUserFromRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return false;

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to remove user from role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("User {UserId} removed from role {RoleName}", userId, role.Name);
        return true;
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return new List<string>();

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
}

public class UserRoleDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

