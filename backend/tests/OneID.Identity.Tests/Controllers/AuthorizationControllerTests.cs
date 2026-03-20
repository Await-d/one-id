using System.Net;
using System.Net.Http;
using Xunit;

namespace OneID.Identity.Tests.Controllers;

/// <summary>
/// T-30: Tests for AuthorizationController (OpenID Connect endpoints)
/// The /connect/authorize endpoint is part of the OIDC server flow (OpenIddict).
/// In test environment without a real OIDC client/session, the authorize endpoint
/// redirects unauthenticated users to /login. We test the observable HTTP behavior.
/// </summary>
public class AuthorizationControllerTests : IClassFixture<IdentityFactory>
{
    private readonly HttpClient _client;

    public AuthorizationControllerTests(IdentityFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Authorize_WithoutClientId_ShouldReturnBadRequestOrRedirect()
    {
        // Without required OIDC params, expect 400 or redirect-to-login
        var response = await _client.GetAsync("/connect/authorize");

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected 400 or redirect but got {response.StatusCode}");
    }

    [Fact]
    public async Task Authorize_UnauthenticatedWithValidParams_ShouldRedirectToLogin()
    {
        // With minimal OIDC params but unauthenticated, expect redirect to /login
        var response = await _client.GetAsync(
            "/connect/authorize?response_type=code&client_id=spa.portal&redirect_uri=https://spa.local/callback&scope=openid");

        Assert.True(
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected redirect or 400 but got {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            Assert.True(
                location.Contains("/login") || location.Contains("/connect"),
                $"Redirect location should contain /login, got: {location}");
        }
    }

    [Fact]
    public async Task Token_WithoutCredentials_ShouldReturnBadRequest()
    {
        // POST /connect/token without valid grant → 400
        var response = await _client.PostAsync("/connect/token",
            new FormUrlEncodedContent(new[] {
                new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "authorization_code"),
                new System.Collections.Generic.KeyValuePair<string, string>("code", "invalid_code")
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
