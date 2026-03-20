using System;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class SessionsControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public SessionsControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUserSessions_WithValidUserId_ShouldReturnOk()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/sessions/user/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sessions = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.NotNull(sessions);
    }

    [Fact]
    public async Task RevokeSession_WithRandomSessionId_ShouldReturnOkOrNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new { Reason = "test" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/revoke", request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404 but got {response.StatusCode}");
    }
}
