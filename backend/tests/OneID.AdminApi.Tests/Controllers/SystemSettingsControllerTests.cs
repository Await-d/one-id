using System.Net;
using System.Net.Http.Json;
using OneID.AdminApi.Controllers;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class SystemSettingsControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public SystemSettingsControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllSystemSettings_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/systemsettings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var settings = await response.Content.ReadFromJsonAsync<SystemSettingDto[]>();
        Assert.NotNull(settings);
    }

    [Fact]
    public async Task CreateSystemSetting_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateSystemSettingRequest
        {
            Key = $"test.setting.{Guid.NewGuid():N}",
            Value = "test-value",
            Group = "Test",
            ValueType = "String",
            DisplayName = "Test Setting"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/systemsettings", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<SystemSettingDto>();
        Assert.NotNull(created);
        Assert.Equal(request.Key, created.Key);
    }
}
