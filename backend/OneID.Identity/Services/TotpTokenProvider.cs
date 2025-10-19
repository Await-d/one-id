using Microsoft.AspNetCore.Identity;
using OneID.Shared.Domain;

namespace OneID.Identity.Services;

public class TotpTokenProvider : IUserTwoFactorTokenProvider<AppUser>
{
    private readonly IMfaService _mfaService;

    public TotpTokenProvider(IMfaService mfaService)
    {
        _mfaService = mfaService;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<AppUser> manager, AppUser user)
    {
        return Task.FromResult(user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TotpSecret));
    }

    public Task<string> GenerateAsync(string purpose, UserManager<AppUser> manager, AppUser user)
    {
        // TOTP codes are time-based, we don't actually generate them here
        // This is used for validation purposes
        return Task.FromResult(string.Empty);
    }

    public Task<bool> ValidateAsync(string purpose, string token, UserManager<AppUser> manager, AppUser user)
    {
        if (string.IsNullOrEmpty(user.TotpSecret))
            return Task.FromResult(false);

        var secret = _mfaService.DecryptSecret(user.TotpSecret);
        var isValid = _mfaService.ValidateTotp(secret, token);
        
        return Task.FromResult(isValid);
    }
}
