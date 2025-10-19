using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.AdminApi.Configuration;
using OneID.AdminApi.Models;
using OneID.AdminApi.Services;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/client-settings")]
[Authorize]
public sealed class ClientSettingsController(IClientValidationSettingsProvider settingsProvider) : ControllerBase
{
    [HttpGet("validation")]
    public async Task<ActionResult<ClientValidationSettingsResponse>> GetValidationSettings(CancellationToken cancellationToken)
    {
        var result = await settingsProvider.GetAsync(cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPut("validation")]
    public async Task<ActionResult<ClientValidationSettingsResponse>> UpdateValidationSettings(
        [FromBody] UpdateClientValidationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.AllowedSchemes.Length == 0)
        {
            ModelState.AddModelError(nameof(request.AllowedSchemes), "必须至少包含一个 Scheme。");
            return ValidationProblem(ModelState);
        }

        var options = new ClientValidationOptions
        {
            AllowedSchemes = request.AllowedSchemes.Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray(),
            AllowHttpOnLoopback = request.AllowHttpOnLoopback,
            AllowedHosts = request.AllowedHosts.Select(h => h.Trim()).Where(h => !string.IsNullOrEmpty(h)).ToArray()
        };

        var result = await settingsProvider.SetAsync(options, cancellationToken);
        return Ok(ToResponse(result));
    }

    private static ClientValidationSettingsResponse ToResponse(ClientValidationSettingsResult result)
    {
        return new ClientValidationSettingsResponse(
            result.Options.AllowedSchemes,
            result.Options.AllowHttpOnLoopback,
            result.Options.AllowedHosts,
            result.UpdatedAt);
    }
}
