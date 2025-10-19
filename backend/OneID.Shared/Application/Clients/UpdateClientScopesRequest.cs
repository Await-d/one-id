using System.ComponentModel.DataAnnotations;

namespace OneID.Shared.Application.Clients;

public sealed class UpdateClientScopesRequest
{
    [Required]
    [MinLength(1)]
    public string[] Scopes { get; init; } = Array.Empty<string>();
}
