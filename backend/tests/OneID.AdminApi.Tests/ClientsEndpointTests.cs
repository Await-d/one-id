using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OneID.Shared.Application.Clients;
using Xunit;

namespace OneID.AdminApi.Tests;

[Collection(IntegrationTestCollection.Name)]
public class ClientsEndpointTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;

    public ClientsEndpointTests(AdminApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsClientSummaries()
    {
        var response = await _client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<ClientSummary>>();

        Assert.NotNull(payload);
        Assert.NotEmpty(payload!);
        var client = payload!.FirstOrDefault(c => c.ClientId == "spa.portal");
        Assert.NotNull(client);
        Assert.NotNull(client!.Scopes);
    }

    [Fact]
    public async Task Post_CreatesNewClient()
    {
        var request = new CreateClientRequest
        {
            ClientId = "spa.dashboard",
            DisplayName = "Dashboard",
            RedirectUri = "https://dashboard.local/callback",
            PostLogoutRedirectUri = "https://dashboard.local",
            Scopes = new[] { "openid", "profile", "email" },
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ClientSecret = "secret123"
        };

        var response = await _client.PostAsJsonAsync("/api/clients", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ClientSummary>();

        Assert.NotNull(payload);
        Assert.Equal(request.ClientId, payload!.ClientId);
        Assert.Contains(request.PostLogoutRedirectUri!, payload.PostLogoutRedirectUris);
        Assert.Equal(request.ClientType, payload.ClientType);
    }

    [Fact]
    public async Task Post_DuplicateClient_ReturnsConflict()
    {
        var request = new CreateClientRequest
        {
            ClientId = "spa.portal",
            DisplayName = "Portal",
            RedirectUri = "https://portal.local/callback"
        };

        var response = await _client.PostAsJsonAsync("/api/clients", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_LocalHttpRedirect_AllowsLoopback()
    {
        var request = new CreateClientRequest
        {
            ClientId = "spa.localdev",
            DisplayName = "Local Dev",
            RedirectUri = "http://localhost:5173/callback"
        };

        var response = await _client.PostAsJsonAsync("/api/clients", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidRedirectUri_ReturnsBadRequest()
    {
        var request = new CreateClientRequest
        {
            ClientId = "spa.invalid",
            DisplayName = "Invalid",
            RedirectUri = "http://malicious.example.com/callback"
        };

        var response = await _client.PostAsJsonAsync("/api/clients", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("RedirectUri"));
    }

    [Fact]
    public async Task Delete_RemovesClient()
    {
        var create = new CreateClientRequest
        {
            ClientId = "spa.delete",
            DisplayName = "Delete",
            RedirectUri = "https://delete.local/callback"
        };

        var postResponse = await _client.PostAsJsonAsync("/api/clients", create);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/clients/{create.ClientId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetFromJsonAsync<IReadOnlyList<ClientSummary>>("/api/clients");
        Assert.DoesNotContain(listResponse!, c => c.ClientId == create.ClientId);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync("/api/clients/unknown.client");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesClient()
    {
        var create = new CreateClientRequest
        {
            ClientId = "spa.update",
            DisplayName = "Update",
            RedirectUri = "https://update.local/callback"
        };

        await _client.PostAsJsonAsync("/api/clients", create);

        var update = new UpdateClientRequest
        {
            DisplayName = "Update2",
            RedirectUri = "https://update.local/new-callback",
            PostLogoutRedirectUri = "https://update.local/logout",
            Scopes = new[] { "openid", "profile", "email" }
        };

        var response = await _client.PutAsJsonAsync($"/api/clients/{create.ClientId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ClientSummary>();

        Assert.NotNull(payload);
        Assert.Equal(update.DisplayName, payload!.DisplayName);
        Assert.Contains(update.PostLogoutRedirectUri!, payload.PostLogoutRedirectUris);
        Assert.Contains("email", payload.Scopes);
    }

    [Fact]
    public async Task Put_NotFound_Returns404()
    {
        var update = new UpdateClientRequest
        {
            DisplayName = "Missing",
            RedirectUri = "https://missing.local/callback"
        };

        var response = await _client.PutAsJsonAsync("/api/clients/missing", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateScopes_ReplacesPermissions()
    {
        var create = new CreateClientRequest
        {
            ClientId = "spa.scopes",
            DisplayName = "Scopes",
            RedirectUri = "https://scopes.local/callback"
        };

        await _client.PostAsJsonAsync("/api/clients", create);

        var scopesRequest = new UpdateClientScopesRequest
        {
            Scopes = new[] { "openid", "profile", "email" }
        };

        var response = await _client.PutAsJsonAsync($"/api/clients/{create.ClientId}/scopes", scopesRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ClientSummary>();

        Assert.NotNull(payload);
        Assert.Equal(create.ClientId, payload!.ClientId);
        Assert.True(scopesRequest.Scopes.All(scope => payload.Scopes.Contains(scope)));
    }

    [Fact]
    public async Task UpdateScopes_NotFound_Returns404()
    {
        var scopesRequest = new UpdateClientScopesRequest
        {
            Scopes = new[] { "openid" }
        };

        var response = await _client.PutAsJsonAsync("/api/clients/missing/scopes", scopesRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
