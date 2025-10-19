using System;
using System.ComponentModel.DataAnnotations;

namespace OneID.AdminApi.Models;

public sealed class UpdateCorsSettingsRequest
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    public bool AllowAnyOrigin { get; set; }
        = false;
}

public sealed record CorsSettingsResponse(
    string[] AllowedOrigins,
    bool AllowAnyOrigin,
    DateTime UpdatedAt);
