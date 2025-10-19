using Microsoft.AspNetCore.Identity;
using OneID.Shared.Application.Users;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Services;

public sealed class UserCommandService(
    UserManager<AppUser> userManager,
    ILogger<UserCommandService> logger) : IUserCommandService
{
    public async Task<UserSummary> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = request.EmailConfirmed,
            DisplayName = request.DisplayName,
            IsExternal = false
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create user: {Errors}", errors);
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        logger.LogInformation("Created user {UserName} with ID {UserId}", user.UserName, user.Id);

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

    public async Task<UserSummary> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var updated = false;

        if (request.DisplayName != null && user.DisplayName != request.DisplayName)
        {
            user.DisplayName = request.DisplayName;
            updated = true;
        }

        if (request.EmailConfirmed.HasValue && user.EmailConfirmed != request.EmailConfirmed.Value)
        {
            user.EmailConfirmed = request.EmailConfirmed.Value;
            updated = true;
        }

        if (request.LockoutEnabled.HasValue && user.LockoutEnabled != request.LockoutEnabled.Value)
        {
            user.LockoutEnabled = request.LockoutEnabled.Value;
            updated = true;
        }

        if (updated)
        {
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to update user {UserId}: {Errors}", userId, errors);
                throw new InvalidOperationException($"Failed to update user: {errors}");
            }

            logger.LogInformation("Updated user {UserId}", userId);
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

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to delete user {UserId}: {Errors}", userId, errors);
            throw new InvalidOperationException($"Failed to delete user: {errors}");
        }

        logger.LogInformation("Deleted user {UserId}", userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // Remove old password and add new one
        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to remove password for user {UserId}: {Errors}", userId, errors);
            throw new InvalidOperationException($"Failed to remove password: {errors}");
        }

        var addResult = await userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
        {
            var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to add password for user {UserId}: {Errors}", userId, errors);
            throw new InvalidOperationException($"Failed to add password: {errors}");
        }

        logger.LogInformation("Changed password for user {UserId}", userId);
    }

    public async Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var result = await userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to unlock user {UserId}: {Errors}", userId, errors);
            throw new InvalidOperationException($"Failed to unlock user: {errors}");
        }

        // Reset access failed count
        await userManager.ResetAccessFailedCountAsync(user);

        logger.LogInformation("Unlocked user {UserId}", userId);
    }
}
