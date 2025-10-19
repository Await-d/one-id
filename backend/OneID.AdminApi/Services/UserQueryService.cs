using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Application.Users;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Services;

public sealed class UserQueryService(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager) : IUserQueryService
{
    public async Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.ToListAsync(cancellationToken);
        var summaries = new List<UserSummary>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var logins = await userManager.GetLoginsAsync(user);
            
            summaries.Add(new UserSummary(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.EmailConfirmed,
                user.DisplayName,
                user.IsExternal,
                user.LockoutEnabled,
                user.LockoutEnd,
                user.AccessFailedCount,
                roles.ToArray(),
                logins.Select(l => new OneID.Shared.Application.Users.ExternalLoginInfo(
                    l.LoginProvider,
                    l.ProviderKey,
                    l.ProviderDisplayName)).ToArray()));
        }

        return summaries;
    }

    public async Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var logins = await userManager.GetLoginsAsync(user);

        return new UserSummary(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            user.DisplayName,
            user.IsExternal,
            user.LockoutEnabled,
            user.LockoutEnd,
            user.AccessFailedCount,
            roles.ToArray(),
            logins.Select(l => new OneID.Shared.Application.Users.ExternalLoginInfo(
                l.LoginProvider,
                l.ProviderKey,
                l.ProviderDisplayName)).ToArray());
    }

    public async Task<UserSummary?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var logins = await userManager.GetLoginsAsync(user);

        return new UserSummary(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            user.DisplayName,
            user.IsExternal,
            user.LockoutEnabled,
            user.LockoutEnd,
            user.AccessFailedCount,
            roles.ToArray(),
            logins.Select(l => new OneID.Shared.Application.Users.ExternalLoginInfo(
                l.LoginProvider,
                l.ProviderKey,
                l.ProviderDisplayName)).ToArray());
    }
}
