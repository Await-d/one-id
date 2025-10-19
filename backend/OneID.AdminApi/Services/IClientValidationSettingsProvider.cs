using OneID.AdminApi.Configuration;

namespace OneID.AdminApi.Services;

public sealed record ClientValidationSettingsResult(ClientValidationOptions Options, DateTime UpdatedAt);

public interface IClientValidationSettingsProvider
{
    Task<ClientValidationSettingsResult> GetAsync(CancellationToken cancellationToken = default);
    Task<ClientValidationSettingsResult> SetAsync(ClientValidationOptions options, CancellationToken cancellationToken = default);
}
