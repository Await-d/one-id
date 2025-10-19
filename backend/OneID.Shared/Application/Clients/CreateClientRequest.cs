using System.ComponentModel.DataAnnotations;
using OpenIddict.Abstractions;

namespace OneID.Shared.Application.Clients;

public sealed class CreateClientRequest
{
    [Required]
    [StringLength(100)]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [StringLength(512)]
    [Url]
    public string RedirectUri { get; init; } = string.Empty;

    [StringLength(512)]
    [Url]
    public string? PostLogoutRedirectUri { get; init; }

    public string[] Scopes { get; init; } = ["openid", "profile"];

    public string ClientType { get; init; } = OpenIddictConstants.ClientTypes.Public;

    [StringLength(200)]
    public string? ClientSecret { get; init; }
}
