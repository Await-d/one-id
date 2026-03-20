using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class AnomalyDetectionControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public AnomalyDetectionControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAnomalousLogins_ShouldReturnOkWithArray()
    {
        // Act
        var response = await _client.GetAsync("/api/anomalydetection/anomalous-logins");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var logins = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.NotNull(logins);
    }

    [Fact]
    public async Task GetRiskScore_WithRandomUserId_ShouldReturnOkOrNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/anomalydetection/risk-score/{userId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {response.StatusCode}");
    }
}
