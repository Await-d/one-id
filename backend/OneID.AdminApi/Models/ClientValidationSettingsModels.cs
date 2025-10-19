using System.ComponentModel.DataAnnotations;

namespace OneID.AdminApi.Models;

public sealed class UpdateClientValidationSettingsRequest
{
    [Required]
    [MinLength(1)]
    public string[] AllowedSchemes { get; set; } = Array.Empty<string>();

    public bool AllowHttpOnLoopback { get; set; }
        = true;

    public string[] AllowedHosts { get; set; } = Array.Empty<string>();
}

public sealed record ClientValidationSettingsResponse(
    string[] AllowedSchemes,
    bool AllowHttpOnLoopback,
    string[] AllowedHosts,
    DateTime UpdatedAt);
