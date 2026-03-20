using System;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class ScopesControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public ScopesControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllScopes_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/scopes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var scopes = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.NotNull(scopes);
    }

    [Fact]
    public async Task CreateScope_WithValidBody_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            Name = $"test_scope_{Guid.NewGuid():N}",
            DisplayName = "Test Scope",
            Description = "Integration test scope"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/scopes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
