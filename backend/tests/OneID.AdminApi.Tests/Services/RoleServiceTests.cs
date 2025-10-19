using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneID.AdminApi.Services;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using Xunit;

namespace OneID.AdminApi.Tests.Services;

public class RoleServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AppDbContext _dbContext;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleService _roleService;

    public RoleServiceTests()
    {
        var services = new ServiceCollection();

        // Setup in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 4;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Add logging
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        _roleManager = _serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<AppUser>>();

        var logger = _serviceProvider.GetRequiredService<ILogger<RoleService>>();
        _roleService = new RoleService(_roleManager, _userManager, logger);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldCreateRole()
    {
        // Arrange
        var roleName = "TestRole";
        var description = "Test role description";

        // Act
        var result = await _roleService.CreateRoleAsync(roleName, description);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(roleName, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(0, result.UserCount);
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldReturnAllRoles()
    {
        // Arrange
        await _roleService.CreateRoleAsync("Role1", "Description 1");
        await _roleService.CreateRoleAsync("Role2", "Description 2");
        await _roleService.CreateRoleAsync("Role3", "Description 3");

        // Act
        var roles = await _roleService.GetAllRolesAsync();

        // Assert
        Assert.NotNull(roles);
        Assert.Equal(3, roles.Count);
    }

    [Fact]
    public async Task GetRoleByIdAsync_ShouldReturnRole()
    {
        // Arrange
        var createdRole = await _roleService.CreateRoleAsync("TestRole", "Description");

        // Act
        var role = await _roleService.GetRoleByIdAsync(createdRole.Id);

        // Assert
        Assert.NotNull(role);
        Assert.Equal(createdRole.Id, role.Id);
        Assert.Equal("TestRole", role.Name);
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldUpdateRole()
    {
        // Arrange
        var createdRole = await _roleService.CreateRoleAsync("OldName", "Old Description");
        var newName = "NewName";
        var newDescription = "New Description";

        // Act
        var result = await _roleService.UpdateRoleAsync(createdRole.Id, newName, newDescription);

        // Assert
        Assert.True(result);
        
        var updatedRole = await _roleService.GetRoleByIdAsync(createdRole.Id);
        Assert.Equal(newName, updatedRole!.Name);
        Assert.Equal(newDescription, updatedRole.Description);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldDeleteRole()
    {
        // Arrange
        var createdRole = await _roleService.CreateRoleAsync("ToDelete", "Will be deleted");

        // Act
        var result = await _roleService.DeleteRoleAsync(createdRole.Id);

        // Assert
        Assert.True(result);
        
        var deletedRole = await _roleService.GetRoleByIdAsync(createdRole.Id);
        Assert.Null(deletedRole);
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldAddUserToRole()
    {
        // Arrange
        var user = new AppUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };
        await _userManager.CreateAsync(user, "Password123");

        var role = await _roleService.CreateRoleAsync("TestRole", "Description");

        // Act
        var result = await _roleService.AddUserToRoleAsync(user.Id, role.Id);

        // Assert
        Assert.True(result);
        
        var userRoles = await _roleService.GetUserRolesAsync(user.Id);
        Assert.Contains("TestRole", userRoles);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldRemoveUserFromRole()
    {
        // Arrange
        var user = new AppUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };
        await _userManager.CreateAsync(user, "Password123");

        var role = await _roleService.CreateRoleAsync("TestRole", "Description");
        await _roleService.AddUserToRoleAsync(user.Id, role.Id);

        // Act
        var result = await _roleService.RemoveUserFromRoleAsync(user.Id, role.Id);

        // Assert
        Assert.True(result);
        
        var userRoles = await _roleService.GetUserRolesAsync(user.Id);
        Assert.DoesNotContain("TestRole", userRoles);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_ShouldReturnUsersInRole()
    {
        // Arrange
        var user1 = new AppUser { UserName = "user1", Email = "user1@example.com", EmailConfirmed = true };
        var user2 = new AppUser { UserName = "user2", Email = "user2@example.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user1, "Password123");
        await _userManager.CreateAsync(user2, "Password123");

        var role = await _roleService.CreateRoleAsync("TestRole", "Description");
        await _roleService.AddUserToRoleAsync(user1.Id, role.Id);
        await _roleService.AddUserToRoleAsync(user2.Id, role.Id);

        // Act
        var usersInRole = await _roleService.GetUsersInRoleAsync(role.Id);

        // Assert
        Assert.NotNull(usersInRole);
        Assert.Equal(2, usersInRole.Count);
        Assert.Contains(usersInRole, u => u.UserName == "user1");
        Assert.Contains(usersInRole, u => u.UserName == "user2");
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnUserRoles()
    {
        // Arrange
        var user = new AppUser { UserName = "testuser", Email = "test@example.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Password123");

        var role1 = await _roleService.CreateRoleAsync("Role1", "Description 1");
        var role2 = await _roleService.CreateRoleAsync("Role2", "Description 2");

        await _roleService.AddUserToRoleAsync(user.Id, role1.Id);
        await _roleService.AddUserToRoleAsync(user.Id, role2.Id);

        // Act
        var userRoles = await _roleService.GetUserRolesAsync(user.Id);

        // Assert
        Assert.NotNull(userRoles);
        Assert.Equal(2, userRoles.Count);
        Assert.Contains("Role1", userRoles);
        Assert.Contains("Role2", userRoles);
    }

    [Fact]
    public async Task CreateRoleAsync_WithDuplicateName_ShouldThrowException()
    {
        // Arrange
        await _roleService.CreateRoleAsync("DuplicateRole", "First");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _roleService.CreateRoleAsync("DuplicateRole", "Second");
        });
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}

