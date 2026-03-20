using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class BulkOperationsControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public BulkOperationsControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AssignRoles_WithEmptyUserIds_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new { UserIds = new List<Guid>(), RoleNames = new List<string> { "Admin" } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bulkoperations/assign-roles", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignRoles_WithValidBody_ShouldReturnOk()
    {
        // Arrange
        var request = new
        {
            UserIds = new List<Guid> { Guid.NewGuid() },
            RoleNames = new List<string> { "Admin" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bulkoperations/assign-roles", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EnableUsers_WithValidGuids_ShouldReturnOk()
    {
        // Arrange
        var request = new { UserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bulkoperations/enable-users", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LockUsers_WithValidGuids_ShouldReturnOk()
    {
        // Arrange
        var request = new { UserIds = new List<Guid> { Guid.NewGuid() } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bulkoperations/lock-users", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}