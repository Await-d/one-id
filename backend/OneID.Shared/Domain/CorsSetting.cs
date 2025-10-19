using System;

namespace OneID.Shared.Domain;

public class CorsSetting
{
    public Guid Id { get; set; }
    public string AllowedOrigins { get; set; } = string.Empty;
    public bool AllowAnyOrigin { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
