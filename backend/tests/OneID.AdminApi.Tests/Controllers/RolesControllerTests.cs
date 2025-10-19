using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OneID.AdminApi.Controllers;
using OneID.AdminApi.Services;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection("Integration")]
public class RolesControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public RolesControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllRoles_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var roles = await response.Content.ReadFromJsonAsync<RoleDto[]>();
        Assert.NotNull(roles);
    }

    [Fact]
    public async Task CreateRole_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = $"TestRole_{Guid.NewGuid()}",
            Description = "Test role created by integration test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/roles", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var createdRole = await response.Content.ReadFromJsonAsync<RoleDto>();
        Assert.NotNull(createdRole);
        Assert.Equal(request.Name, createdRole.Name);
        Assert.Equal(request.Description, createdRole.Description);
    }

    [Fact]
    public async Task UpdateRole_ShouldReturnOk()
    {
        // Arrange - Create a role first
        var createRequest = new CreateRoleRequest
        {
            Name = $"RoleToUpdate_{Guid.NewGuid()}",
            Description = "Original description"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/roles", createRequest);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        var updateRequest = new UpdateRoleRequest
        {
            Name = createdRole!.Name,
            Description = "Updated description"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/roles/{createdRole.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_ShouldReturnOk()
    {
        // Arrange - Create a role first
        var createRequest = new CreateRoleRequest
        {
            Name = $"RoleToDelete_{Guid.NewGuid()}",
            Description = "Will be deleted"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/roles", createRequest);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/roles/{createdRole!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify role is deleted
        var getResponse = await _client.GetAsync($"/api/roles/{createdRole.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetRoleById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/roles/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        var roleName = $"UniqueRole_{Guid.NewGuid()}";
        var request1 = new CreateRoleRequest { Name = roleName, Description = "First" };
        var request2 = new CreateRoleRequest { Name = roleName, Description = "Duplicate" };

        await _client.PostAsJsonAsync("/api/roles", request1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/roles", request2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsersInRole_ShouldReturnUserList()
    {
        // Arrange - Create a role
        var createRequest = new CreateRoleRequest
        {
            Name = $"RoleWithUsers_{Guid.NewGuid()}",
            Description = "Role for user testing"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/roles", createRequest);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Act
        var response = await _client.GetAsync($"/api/roles/{createdRole!.Id}/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var users = await response.Content.ReadFromJsonAsync<UserRoleDto[]>();
        Assert.NotNull(users);
    }
}

