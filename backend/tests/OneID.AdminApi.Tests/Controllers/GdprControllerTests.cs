using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection("Integration")]
public class GdprControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public GdprControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ExportUserData_WithRandomGuid_ShouldReturnNotFound()
    {
        // Arrange
        var randomUserId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/gdpr/users/{randomUserId}/export");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserData_WithRandomGuid_ShouldReturnNotFound()
    {
        // Arrange
        var randomUserId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/gdpr/users/{randomUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
