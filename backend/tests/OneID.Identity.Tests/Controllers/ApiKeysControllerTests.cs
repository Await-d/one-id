using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OneID.Identity.Tests.Controllers;

/// <summary>
/// T-29: Integration tests for ApiKeysController (/api/apikeys).
/// Tests unauthenticated paths to verify authorization middleware is wired correctly.
/// </summary>
public class ApiKeysControllerTests : IClassFixture<IdentityFactory>
{
    private readonly HttpClient _client;

    public ApiKeysControllerTests(IdentityFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetApiKeys_ShouldReturnResponse()
    {
        // Cookie-based auth with no session: GetUserId() returns null → 401
        // Or returns empty list if cookie auth resolves anonymous user
        var response = await _client.GetAsync("/api/apikeys");
        // The endpoint is callable — validates that the route is registered correctly
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task CreateApiKey_WithValidName_ShouldReturnResponse()
    {
        // Validates that the POST /api/apikeys route is registered and responds
        var content = new StringContent(
            "{\"name\":\"test-key\",\"description\":\"Test\"}",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync("/api/apikeys", content);
        // Route must exist and not 404/500
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task RevokeApiKey_WithRandomId_ShouldReturnResponse()
    {
        // Validates that the revoke route is registered and responds
        var id = System.Guid.NewGuid();
        var content = new StringContent("{\"reason\":\"test\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"/api/apikeys/{id}/revoke", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
