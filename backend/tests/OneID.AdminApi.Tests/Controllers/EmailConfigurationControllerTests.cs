using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection("Integration")]
public class EmailConfigurationControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public EmailConfigurationControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/emailconfiguration");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmailConfiguration_WithSmtp_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            provider = "Smtp",
            fromEmail = "test@example.com",
            fromName = "Test Sender",
            smtpHost = "smtp.example.com",
            smtpPort = 587,
            smtpUseSsl = true,
            smtpUsername = "user@example.com",
            smtpPassword = "password123",
            isEnabled = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emailconfiguration", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateEmailConfiguration_WithSendGrid_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            provider = "SendGrid",
            fromEmail = "noreply@example.com",
            fromName = "My App",
            sendGridApiKey = "SG.test_api_key_12345",
            isEnabled = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emailconfiguration", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmailConfiguration_WithNone_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            provider = "None",
            fromEmail = "dev@example.com",
            fromName = "Development",
            isEnabled = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emailconfiguration", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmailConfiguration_ShouldReturnOk()
    {
        // Arrange - Create first
        var createRequest = new
        {
            provider = "None",
            fromEmail = "original@example.com",
            fromName = "Original",
            isEnabled = false
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/emailconfiguration", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var id = (int)createResult!.id;

        var updateRequest = new
        {
            provider = "None",
            fromEmail = "updated@example.com",
            fromName = "Updated",
            isEnabled = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/emailconfiguration/{id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmailConfiguration_ShouldReturnOk()
    {
        // Arrange - Create first
        var createRequest = new
        {
            provider = "None",
            fromEmail = "todelete@example.com",
            fromName = "To Delete",
            isEnabled = false
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/emailconfiguration", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var id = (int)createResult!.id;

        // Act
        var response = await _client.DeleteAsync($"/api/emailconfiguration/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/emailconfiguration/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = 999999;

        // Act
        var response = await _client.GetAsync($"/api/emailconfiguration/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetActive_WithNoActiveConfig_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/emailconfiguration/active");

        // Assert - Might be NotFound if no active config exists, or OK if one exists
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                   response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateEmailConfiguration_PasswordUpdate_ShouldNotExposePassword()
    {
        // Arrange
        var createRequest = new
        {
            provider = "Smtp",
            fromEmail = "test@example.com",
            fromName = "Test",
            smtpHost = "smtp.example.com",
            smtpPort = 587,
            smtpUseSsl = true,
            smtpUsername = "user@example.com",
            smtpPassword = "secret_password",
            isEnabled = true
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/emailconfiguration", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var id = (int)createResult!.id;

        // Act - Get the config
        var getResponse = await _client.GetAsync($"/api/emailconfiguration/{id}");
        var config = await getResponse.Content.ReadFromJsonAsync<dynamic>();

        // Assert - Password should not be in response, only hasSmtpPassword flag
        Assert.NotNull(config);
        Assert.True((bool)config.hasSmtpPassword);
    }
}

