using System;
using System.Linq;
using OpenIddict.Abstractions;
using OneID.Shared.Application.Clients;
using OneID.AdminApi.Validation;

namespace OneID.AdminApi.Services;

public sealed class ClientCommandService(
    IOpenIddictApplicationManager applicationManager,
    IClientValidationSettingsProvider settingsProvider) : IClientCommandService
{
    public async Task<ClientSummary> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);
        ClientRequestValidator.Validate(request, settings.Options);

        var existing = await applicationManager.FindByClientIdAsync(request.ClientId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Client '{request.ClientId}' already exists.");
        }

        var descriptor = BuildDescriptor(request);

        await applicationManager.CreateAsync(descriptor, cancellationToken);

        return new ClientSummary(
            descriptor.ClientId!,
            descriptor.DisplayName ?? descriptor.ClientId!,
            descriptor.ClientType ?? OpenIddictConstants.ClientTypes.Public,
            descriptor.RedirectUris.Select(uri => uri.ToString()).ToArray(),
            descriptor.PostLogoutRedirectUris.Select(uri => uri.ToString()).ToArray(),
            ExtractScopes(descriptor));
    }

    public async Task DeleteAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("ClientId is required", nameof(clientId));
        }

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Client '{clientId}' was not found.");
        }

        await applicationManager.DeleteAsync(existing, cancellationToken);
    }

    public async Task<ClientSummary> UpdateAsync(string clientId, UpdateClientRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("ClientId is required", nameof(clientId));
        }

        var settings = await settingsProvider.GetAsync(cancellationToken);
        ClientRequestValidator.Validate(request, settings.Options);

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Client '{clientId}' was not found.");
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId
        };
        await applicationManager.PopulateAsync(descriptor, existing, cancellationToken);

        descriptor.DisplayName = request.DisplayName;
        descriptor.ClientType = request.ClientType;
        descriptor.RedirectUris.Clear();
        descriptor.RedirectUris.Add(new Uri(request.RedirectUri));

        descriptor.PostLogoutRedirectUris.Clear();
        if (!string.IsNullOrWhiteSpace(request.PostLogoutRedirectUri))
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(request.PostLogoutRedirectUri));
        }

        descriptor.Permissions.RemoveWhere(static permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.OrdinalIgnoreCase));
        foreach (var scope in request.Scopes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            descriptor.ClientSecret = request.ClientSecret;
        }

        await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);

        return new ClientSummary(
            descriptor.ClientId!,
            descriptor.DisplayName ?? descriptor.ClientId!,
            descriptor.ClientType ?? OpenIddictConstants.ClientTypes.Public,
            descriptor.RedirectUris.Select(uri => uri.ToString()).ToArray(),
            descriptor.PostLogoutRedirectUris.Select(uri => uri.ToString()).ToArray(),
            ExtractScopes(descriptor));
    }

    public async Task<ClientSummary> UpdateScopesAsync(string clientId, UpdateClientScopesRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("ClientId is required", nameof(clientId));
        }

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Client '{clientId}' was not found.");
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, existing, cancellationToken);

        descriptor.Permissions.RemoveWhere(static permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.OrdinalIgnoreCase));
        foreach (var scope in request.Scopes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);

        return new ClientSummary(
            descriptor.ClientId!,
            descriptor.DisplayName ?? descriptor.ClientId!,
            descriptor.ClientType ?? OpenIddictConstants.ClientTypes.Public,
            descriptor.RedirectUris.Select(uri => uri.ToString()).ToArray(),
            descriptor.PostLogoutRedirectUris.Select(uri => uri.ToString()).ToArray(),
            ExtractScopes(descriptor));
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(CreateClientRequest request)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = request.ClientId,
            DisplayName = request.DisplayName,
            ClientType = request.ClientType,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit
        };

        descriptor.RedirectUris.Add(new Uri(request.RedirectUri));

        if (!string.IsNullOrWhiteSpace(request.PostLogoutRedirectUri))
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(request.PostLogoutRedirectUri));
        }

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);

        foreach (var scope in request.Scopes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            descriptor.ClientSecret = request.ClientSecret;
        }

        return descriptor;
    }

    private static string[] ExtractScopes(OpenIddictApplicationDescriptor descriptor)
    {
        return descriptor.Permissions
            .Where(static permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.OrdinalIgnoreCase))
            .Select(permission => permission[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
            .ToArray();
    }
}
