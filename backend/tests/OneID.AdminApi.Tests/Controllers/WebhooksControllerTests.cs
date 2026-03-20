using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class WebhooksControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;

    public WebhooksControllerTests(AdminApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllWebhooks_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/webhooks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var webhooks = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.NotNull(webhooks);
    }

    [Fact]
    public async Task GetWebhook_WithInvalidId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/webhooks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWebhook_WithValidBody_ShouldReturnCreated()
    {
        var request = new
        {
            Name = $"Test Webhook {Guid.NewGuid():N}",
            Url = "https://example.com/webhook",
            Description = "Integration test webhook",
            Events = new[] { "user.created" },
            IsActive = true,
            MaxRetries = 3,
            TimeoutSeconds = 30
        };

        var response = await _client.PostAsJsonAsync("/api/webhooks", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWebhook_AfterCreate_ShouldReturnNoContent()
    {
        var createRequest = new
        {
            Name = $"Webhook To Delete {Guid.NewGuid():N}",
            Url = "https://example.com/webhook",
            Events = new[] { "user.deleted" },
            IsActive = true,
            MaxRetries = 1,
            TimeoutSeconds = 10
        };

        var createResponse = await _client.PostAsJsonAsync("/api/webhooks", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<WebhookIdResponse>();
        var deleteResponse = await _client.DeleteAsync($"/api/webhooks/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private record WebhookIdResponse(Guid Id);
}
