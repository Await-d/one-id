using System;

namespace OneID.Shared.Domain;

public class ClientValidationSetting
{
    public Guid Id { get; set; }
    public string AllowedSchemes { get; set; } = "https";
    public bool AllowHttpOnLoopback { get; set; } = true;
    public string AllowedHosts { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
