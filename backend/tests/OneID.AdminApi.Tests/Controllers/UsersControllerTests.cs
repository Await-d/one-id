using System.Net;
using System.Net.Http.Json;
using OneID.Shared.Application.Users;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class UsersControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public UsersControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOkWithArray()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<UserSummary[]>();
        Assert.NotNull(users);
    }

    [Fact]
    public async Task GetUser_WithInvalidGuid_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/users/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithValidBody_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateUserRequest(
            UserName: $"testuser_{Guid.NewGuid():N}",
            Email: $"test_{Guid.NewGuid():N}@example.com",
            Password: "Test1234!",
            DisplayName: "Integration Test User",
            EmailConfirmed: true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<UserSummary>();
        Assert.NotNull(created);
        Assert.Equal(request.UserName, created.UserName);
        Assert.Equal(request.Email, created.Email);
    }

    [Fact]
    public async Task UpdateUser_WithExistingUser_ShouldReturnOk()
    {
        // Arrange - create a user first
        var createRequest = new CreateUserRequest(
            UserName: $"updateuser_{Guid.NewGuid():N}",
            Email: $"update_{Guid.NewGuid():N}@example.com",
            Password: "Test1234!",
            DisplayName: "Original Name",
            EmailConfirmed: true);

        var createResponse = await _client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserSummary>();

        var updateRequest = new UpdateUserRequest(
            DisplayName: "Updated Name",
            EmailConfirmed: true,
            LockoutEnabled: false);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{createdUser!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithExistingUser_ShouldReturnNoContent()
    {
        // Arrange - create a user first
        var createRequest = new CreateUserRequest(
            UserName: $"deleteuser_{Guid.NewGuid():N}",
            Email: $"delete_{Guid.NewGuid():N}@example.com",
            Password: "Test1234!",
            DisplayName: "To Be Deleted",
            EmailConfirmed: true);

        var createResponse = await _client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserSummary>();

        // Act
        var response = await _client.DeleteAsync($"/api/users/{createdUser!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UnlockUser_WithExistingUser_ShouldReturnOk()
    {
        // Arrange - create a user first
        var createRequest = new CreateUserRequest(
            UserName: $"unlockuser_{Guid.NewGuid():N}",
            Email: $"unlock_{Guid.NewGuid():N}@example.com",
            Password: "Test1234!",
            DisplayName: "User To Unlock",
            EmailConfirmed: true);

        var createResponse = await _client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserSummary>();

        // Act
        var response = await _client.PostAsync($"/api/users/{createdUser!.Id}/unlock", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
