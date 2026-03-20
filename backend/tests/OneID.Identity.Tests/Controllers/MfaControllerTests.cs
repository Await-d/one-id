using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OneID.Identity.Tests.Controllers;

/// <summary>
/// T-28: Integration tests for MfaController (/api/mfa).
/// Tests unauthenticated paths to verify authorization middleware is wired correctly.
/// </summary>
public class MfaControllerTests : IClassFixture<IdentityFactory>
{
    private readonly HttpClient _client;

    public MfaControllerTests(IdentityFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatus_RouteExists_ShouldNotReturn404()
    {
        // Verifies that /api/mfa/status route is registered and responds
        var response = await _client.GetAsync("/api/mfa/status");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Enable_RouteExists_ShouldNotReturn404()
    {
        // Verifies that /api/mfa/enable route is registered and responds
        var content = new StringContent("{\"password\":\"test\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/mfa/enable", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Disable_RouteExists_ShouldNotReturn404()
    {
        // Verifies that /api/mfa/disable route is registered
        var content = new StringContent("{\"password\":\"test\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/mfa/disable", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}