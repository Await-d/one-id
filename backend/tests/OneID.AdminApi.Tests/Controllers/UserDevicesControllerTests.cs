using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class UserDevicesControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public UserDevicesControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUserDevices_WithRandomUserId_ShouldReturnOkWithArray()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/userdevices/user/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var devices = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.NotNull(devices);
    }

    [Fact]
    public async Task GetDevice_WithRandomDeviceId_ShouldReturnOkOrNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/userdevices/{deviceId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {response.StatusCode}");
    }

    [Fact]
    public async Task DeleteDevice_WithRandomDeviceId_ShouldReturnOkOrNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/userdevices/{deviceId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 204 or 404, got {response.StatusCode}");
    }
}
