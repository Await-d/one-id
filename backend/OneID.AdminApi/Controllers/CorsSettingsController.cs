using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.AdminApi.Models;
using OneID.AdminApi.Services;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/cors-settings")]
[Authorize]
public sealed class CorsSettingsController(ICorsSettingsProvider provider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CorsSettingsResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var result = await provider.GetAsync(cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPut]
    public async Task<ActionResult<CorsSettingsResponse>> UpdateAsync(
        [FromBody] UpdateCorsSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var sanitizedOrigins = request.AllowedOrigins
            .Select(origin => origin.Trim())
            .Where(origin => !string.IsNullOrEmpty(origin))
            .ToArray();

        if (!request.AllowAnyOrigin && sanitizedOrigins.Length == 0)
        {
            ModelState.AddModelError(nameof(request.AllowedOrigins), "当不允许任意来源时，必须至少配置一个来源。");
            return ValidationProblem(ModelState);
        }

        var result = await provider.SetAsync(new CorsSettingsOptions(sanitizedOrigins, request.AllowAnyOrigin), cancellationToken);
        return Ok(ToResponse(result));
    }

    private static CorsSettingsResponse ToResponse(CorsSettingsResult result)
    {
        return new CorsSettingsResponse(
            result.Options.AllowedOrigins,
            result.Options.AllowAnyOrigin,
            result.UpdatedAt);
    }
}
