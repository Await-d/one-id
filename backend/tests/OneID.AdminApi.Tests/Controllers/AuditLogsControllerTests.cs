using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OneID.Shared.Data;
using OneID.Shared.Domain;
using Xunit;

namespace OneID.AdminApi.Tests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class AuditLogsControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;
    private readonly HttpClient _client;

    public AuditLogsControllerTests(AdminApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsPagedResult()
    {
        await SeedLogsAsync([
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "UserCreated",
                Category = "User",
                Success = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "LoginSucceeded",
                Category = "Authentication",
                Success = true,
                CreatedAt = DateTime.UtcNow
            }
        ]);

        var response = await _client.GetAsync("/api/auditlogs?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AuditLogResponse>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Total);
        Assert.Single(payload.Logs);
        Assert.Equal("LoginSucceeded", payload.Logs[0].Action);
        Assert.Equal(1, payload.Page);
        Assert.Equal(1, payload.PageSize);

        var secondPage = await _client.GetAsync("/api/auditlogs?page=2&pageSize=1");
        var secondPayload = await secondPage.Content.ReadFromJsonAsync<AuditLogResponse>();

        Assert.NotNull(secondPayload);
        Assert.Equal("UserCreated", secondPayload!.Logs[0].Action);
        Assert.Equal(2, secondPayload.Page);
    }

    [Fact]
    public async Task GetCategories_ReturnsDistinctSortedList()
    {
        await SeedLogsAsync([
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "LoginSucceeded",
                Category = "Authentication",
                Success = true,
                CreatedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "ClientCreated",
                Category = "Client",
                Success = true,
                CreatedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "ClientDeleted",
                Category = "Client",
                Success = true,
                CreatedAt = DateTime.UtcNow
            }
        ]);

        var response = await _client.GetAsync("/api/auditlogs/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await response.Content.ReadFromJsonAsync<string[]>();

        Assert.NotNull(categories);
        Assert.Equal(new[] { "Authentication", "Client" }, categories);
    }

    [Fact]
    public async Task GetAuditLogs_WithKeywordFilters()
    {
        await SeedLogsAsync([
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "ClientCreated",
                Category = "Client",
                Details = "Created SPA client",
                Success = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "UserLoginFailed",
                Category = "Authentication",
                ErrorMessage = "Invalid password",
                Success = false,
                CreatedAt = DateTime.UtcNow
            }
        ]);

        var response = await _client.GetAsync("/api/auditlogs?keyword=login&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AuditLogResponse>();

        Assert.NotNull(payload);
        Assert.Single(payload!.Logs);
        Assert.Equal("UserLoginFailed", payload.Logs[0].Action);
    }

    [Fact]
    public async Task ExportAuditLogs_ReturnsCsvFile()
    {
        await SeedLogsAsync([
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "UserLogin",
                Category = "Authentication",
                Details = "Login success",
                Success = true,
                CreatedAt = DateTime.UtcNow,
                UserName = "admin",
                IpAddress = "127.0.0.1"
            }
        ]);

        var response = await _client.GetAsync("/api/auditlogs/export?keyword=admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("UserLogin", content);
        Assert.Contains("admin", content);
    }

    private async Task SeedLogsAsync(IEnumerable<AuditLog> logs)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = dbContext.Set<AuditLog>();
        dbContext.RemoveRange(existing);
        await dbContext.SaveChangesAsync();

        await dbContext.AddRangeAsync(logs);
        await dbContext.SaveChangesAsync();
    }

    private sealed record AuditLogResponse(
        IReadOnlyList<AuditLogItem> Logs,
        int Total,
        int Page,
        int PageSize);

    private sealed record AuditLogItem(
        Guid Id,
        Guid? UserId,
        string? UserName,
        string Action,
        string Category,
        string? Details,
        string? IpAddress,
        bool Success,
        string? ErrorMessage,
        DateTime CreatedAt);
}
