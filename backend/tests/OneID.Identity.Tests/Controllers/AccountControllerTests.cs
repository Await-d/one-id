using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OneID.Shared.DTOs;
using Xunit;

namespace OneID.Identity.Tests.Controllers;

public class AccountControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AccountControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterRequest(
            UserName: $"testuser_{Guid.NewGuid():N}",
            Email: $"test_{Guid.NewGuid():N}@example.com",
            Password: "Password123",
            DisplayName: "Test User"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.UserId);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldReturnError()
    {
        // Arrange
        var username = $"duplicate_{Guid.NewGuid():N}";
        var request1 = new RegisterRequest(
            UserName: username,
            Email: $"email1_{Guid.NewGuid():N}@example.com",
            Password: "Password123",
            DisplayName: "User 1"
        );
        
        var request2 = new RegisterRequest(
            UserName: username,
            Email: $"email2_{Guid.NewGuid():N}@example.com",
            Password: "Password123",
            DisplayName: "User 2"
        );

        // Act
        await _client.PostAsJsonAsync("/api/account/register", request1);
        var response = await _client.PostAsJsonAsync("/api/account/register", request2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnError()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var request1 = new RegisterRequest(
            UserName: $"user1_{Guid.NewGuid():N}",
            Email: email,
            Password: "Password123",
            DisplayName: "User 1"
        );
        
        var request2 = new RegisterRequest(
            UserName: $"user2_{Guid.NewGuid():N}",
            Email: email,
            Password: "Password123",
            DisplayName: "User 2"
        );

        // Act
        await _client.PostAsJsonAsync("/api/account/register", request1);
        var response = await _client.PostAsJsonAsync("/api/account/register", request2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequest(
            UserName: $"testuser_{Guid.NewGuid():N}",
            Email: $"test_{Guid.NewGuid():N}@example.com",
            Password: "weak", // Too short, no digits
            DisplayName: "Test User"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = $"forgot_{Guid.NewGuid():N}@example.com";
        
        // First register a user
        var registerRequest = new RegisterRequest(
            UserName: $"user_{Guid.NewGuid():N}",
            Email: email,
            Password: "Password123",
            DisplayName: "Test User"
        );
        await _client.PostAsJsonAsync("/api/account/register", registerRequest);

        var forgotRequest = new ForgotPasswordRequest(Email: email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/forgot-password", forgotRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ShouldStillReturnSuccess()
    {
        // Arrange - Security best practice: don't reveal if email exists
        var forgotRequest = new ForgotPasswordRequest(Email: $"nonexistent_{Guid.NewGuid():N}@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/forgot-password", forgotRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ResendConfirmation_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = $"resend_{Guid.NewGuid():N}@example.com";
        
        // First register a user
        var registerRequest = new RegisterRequest(
            UserName: $"user_{Guid.NewGuid():N}",
            Email: email,
            Password: "Password123",
            DisplayName: "Test User"
        );
        await _client.PostAsJsonAsync("/api/account/register", registerRequest);

        var resendRequest = new { email };

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/resend-confirmation", resendRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_WithNonExistentEmail_ShouldStillReturnSuccess()
    {
        // Arrange - Security best practice
        var resendRequest = new { email = $"nonexistent_{Guid.NewGuid():N}@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/resend-confirmation", resendRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ShouldReturnError()
    {
        // Arrange
        var username = $"unconfirmed_{Guid.NewGuid():N}";
        var email = $"unconfirmed_{Guid.NewGuid():N}@example.com";
        var password = "Password123";

        // Register user
        var registerRequest = new RegisterRequest(
            UserName: username,
            Email: email,
            Password: password,
            DisplayName: "Test User"
        );
        await _client.PostAsJsonAsync("/api/account/register", registerRequest);

        var loginRequest = new LoginRequest(
            UserName: username,
            Password: password
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/login", loginRequest);

        // Assert - Should fail because email is not confirmed
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.False(result.Success);
    }
}

