using System.Net;
using System.Net.Http.Json;
using OneID.AdminApi.Models;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class CorsSettingsControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;

    public CorsSettingsControllerTests(AdminApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAsync_ReturnsSettings()
    {
        var response = await _client.GetAsync("/api/cors-settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CorsSettingsResponse>();
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var request = new UpdateCorsSettingsRequest
        {
            AllowedOrigins = new[] { "https://app.example.com" },
            AllowAnyOrigin = false,
        };

        var response = await _client.PutAsJsonAsync("/api/cors-settings", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CorsSettingsResponse>();
        Assert.NotNull(payload);
        Assert.Contains("https://app.example.com", payload!.AllowedOrigins);

        var followUp = await _client.GetFromJsonAsync<CorsSettingsResponse>("/api/cors-settings");
        Assert.NotNull(followUp);
        Assert.False(followUp!.AllowAnyOrigin);
        Assert.Contains("https://app.example.com", followUp.AllowedOrigins);
    }
}
