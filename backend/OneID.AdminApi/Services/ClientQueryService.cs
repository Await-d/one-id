using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using OneID.Shared.Application.Clients;

namespace OneID.AdminApi.Services;

public sealed class ClientQueryService(IOpenIddictApplicationManager applicationManager) : IClientQueryService
{
    public async Task<IReadOnlyList<ClientSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ClientSummary>();
        await foreach (var application in applicationManager.ListAsync(cancellationToken: cancellationToken))
        {
            var clientId = await applicationManager.GetClientIdAsync(application, cancellationToken);
            var displayName = await applicationManager.GetDisplayNameAsync(application, cancellationToken);
            var clientType = await applicationManager.GetClientTypeAsync(application, cancellationToken);
            var redirectUris = await applicationManager.GetRedirectUrisAsync(application, cancellationToken);
            var postLogoutRedirectUris = await applicationManager.GetPostLogoutRedirectUrisAsync(application, cancellationToken);
            var permissions = await applicationManager.GetPermissionsAsync(application, cancellationToken);

            results.Add(new ClientSummary(
                clientId ?? string.Empty,
                displayName ?? clientId ?? string.Empty,
                clientType ?? OpenIddictConstants.ClientTypes.Public,
                redirectUris.Select(uri => uri.ToString()).ToArray(),
                postLogoutRedirectUris.Select(uri => uri.ToString()).ToArray(),
                permissions
                    .Where(static permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.OrdinalIgnoreCase))
                    .Select(permission => permission[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
                    .ToArray()));
        }

        return results;
    }
}
