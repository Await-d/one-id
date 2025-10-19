using System.Net;
using System.Net.Http.Json;
using OneID.AdminApi.Configuration;
using OneID.AdminApi.Models;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class ClientSettingsControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;

    public ClientSettingsControllerTests(AdminApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetValidationSettings_ReturnsDefaultValues()
    {
        var response = await _client.GetAsync("/api/client-settings/validation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ClientValidationSettingsResponse>();
        Assert.NotNull(payload);
        Assert.Contains("https", payload!.AllowedSchemes);
    }

    [Fact]
    public async Task UpdateValidationSettings_PersistsChanges()
    {
        var request = new UpdateClientValidationSettingsRequest
        {
            AllowedSchemes = new[] { "https" },
            AllowHttpOnLoopback = false,
            AllowedHosts = new[] { "example.com" }
        };

        var response = await _client.PutAsJsonAsync("/api/client-settings/validation", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ClientValidationSettingsResponse>();
        Assert.NotNull(payload);
        Assert.Single(payload!.AllowedSchemes);
        Assert.Equal("https", payload.AllowedSchemes[0]);
        Assert.False(payload.AllowHttpOnLoopback);
        Assert.Contains("example.com", payload.AllowedHosts);

        var followUp = await _client.GetFromJsonAsync<ClientValidationSettingsResponse>("/api/client-settings/validation");
        Assert.NotNull(followUp);
        Assert.False(followUp!.AllowHttpOnLoopback);
        Assert.Contains("example.com", followUp.AllowedHosts);
    }
}
