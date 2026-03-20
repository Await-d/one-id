using System.Net;
using System.Net.Http.Json;
using OneID.AdminApi.Controllers;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class SigningKeysControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;

    public SigningKeysControllerTests(AdminApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllSigningKeys_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/signingkeys");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var keys = await response.Content.ReadFromJsonAsync<SigningKeyDto[]>();
        Assert.NotNull(keys);
    }

    [Fact]
    public async Task GenerateRsaKey_ShouldReturnCreated()
    {
        var request = new GenerateRsaKeyRequest(KeySize: 2048, ValidityDays: 90, Notes: "Integration test key");
        var response = await _client.PostAsJsonAsync("/api/signingkeys/rsa", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
