using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using OneID.AdminApi.Controllers;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection("Integration")]
public class TenantsControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public TenantsControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllTenants_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tenants = await response.Content.ReadFromJsonAsync<TenantDto[]>();
        Assert.NotNull(tenants);
    }

    [Fact]
    public async Task CreateTenant_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            Name = $"testtenant{Guid.NewGuid():N}",
            DisplayName = "Test Tenant",
            Domain = "test.example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tenants", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TenantDto>();
        Assert.NotNull(created);
        Assert.Equal(request.Name, created.Name);
    }

    [Fact]
    public async Task GetTenantById_WithRandomGuid_ShouldReturnNotFound()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{randomId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
