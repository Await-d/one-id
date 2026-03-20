using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class SecurityRulesControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;

    public SecurityRulesControllerTests(AdminApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/securityrules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rules = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.NotNull(rules);
    }

    [Fact]
    public async Task CreateRule_WithValidBody_ShouldReturnCreated()
    {
        var request = new
        {
            RuleType = "IpBlacklist",
            RuleValue = $"10.{new Random().Next(0, 255)}.{new Random().Next(0, 255)}.1",
            Description = "Integration test rule"
        };

        var response = await _client.PostAsJsonAsync("/api/securityrules", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithRandomGuid_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/securityrules/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AfterCreate_ShouldReturnNoContent()
    {
        var request = new
        {
            RuleType = "IpBlacklist",
            RuleValue = $"10.0.{new Random().Next(0, 255)}.1",
            Description = "Rule to delete"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/securityrules", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<SecurityRuleIdResponse>();
        var deleteResponse = await _client.DeleteAsync($"/api/securityrules/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    private record SecurityRuleIdResponse(Guid Id);
}
