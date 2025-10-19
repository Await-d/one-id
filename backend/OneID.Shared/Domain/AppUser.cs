using System;
using Microsoft.AspNetCore.Identity;

namespace OneID.Shared.Domain;

public class AppUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public bool IsExternal { get; set; }
    public Guid? TenantId { get; set; }
    
    // MFA/TOTP
    public string? TotpSecret { get; set; } // Encrypted TOTP secret
    public string? RecoveryCodes { get; set; } // Encrypted JSON array of recovery codes
}
