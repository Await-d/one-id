using System;
using Microsoft.AspNetCore.Identity;

namespace OneID.Shared.Domain;

public class AppRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
