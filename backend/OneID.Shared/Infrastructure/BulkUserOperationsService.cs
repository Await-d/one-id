using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 批量用户操作服务实现
/// </summary>
public class BulkUserOperationsService : IBulkUserOperationsService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<BulkUserOperationsService> _logger;

    public BulkUserOperationsService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        SignInManager<AppUser> signInManager,
        AppDbContext dbContext,
        IEmailService emailService,
        ILogger<BulkUserOperationsService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<BulkOperationResult> AssignRolesToUsersAsync(
        List<Guid> userIds,
        List<string> roleNames,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    foreach (var roleName in roleNames)
                    {
                        if (!await _roleManager.RoleExistsAsync(roleName))
                        {
                            throw new InvalidOperationException($"Role '{roleName}' does not exist");
                        }

                        if (!await _userManager.IsInRoleAsync(user, roleName))
                        {
                            var addResult = await _userManager.AddToRoleAsync(user, roleName);
                            if (!addResult.Succeeded)
                            {
                                throw new InvalidOperationException(string.Join(", ", addResult.Errors.Select(e => e.Description)));
                            }
                        }
                    }

                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation(
                        "Assigned roles {Roles} to user {UserId} by {Operator}",
                        string.Join(", ", roleNames), user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex, "Failed to assign roles to user {UserId}", user.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully assigned roles to {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk assign roles operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> RemoveRolesFromUsersAsync(
        List<Guid> userIds,
        List<string> roleNames,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    foreach (var roleName in roleNames)
                    {
                        if (await _userManager.IsInRoleAsync(user, roleName))
                        {
                            var removeResult = await _userManager.RemoveFromRoleAsync(user, roleName);
                            if (!removeResult.Succeeded)
                            {
                                throw new InvalidOperationException(string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                            }
                        }
                    }

                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation(
                        "Removed roles {Roles} from user {UserId} by {Operator}",
                        string.Join(", ", roleNames), user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex, "Failed to remove roles from user {UserId}", user.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully removed roles from {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk remove roles operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> EnableUsersAsync(
        List<Guid> userIds,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    // 解锁用户账户
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.ResetAccessFailedCountAsync(user);
                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation("Enabled user {UserId} by {Operator}", user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully enabled {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk enable users operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> DisableUsersAsync(
        List<Guid> userIds,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    // 锁定用户账户（永久锁定）
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation("Disabled user {UserId} by {Operator}", user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully disabled {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk disable users operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> LockUsersAsync(
        List<Guid> userIds,
        DateTimeOffset? lockoutEnd = null,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };
        var defaultLockoutEnd = lockoutEnd ?? DateTimeOffset.UtcNow.AddYears(100); // 默认永久锁定

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    var lockResult = await _userManager.SetLockoutEndDateAsync(user, defaultLockoutEnd);
                    if (!lockResult.Succeeded)
                    {
                        throw new InvalidOperationException(string.Join(", ", lockResult.Errors.Select(e => e.Description)));
                    }

                    await _userManager.SetLockoutEnabledAsync(user, true);

                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation("Locked user {UserId} until {LockoutEnd} by {Operator}", 
                        user.Id, defaultLockoutEnd, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex, "Failed to lock user {UserId}", user.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully locked {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk lock users operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> UnlockUsersAsync(
        List<Guid> userIds,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);
                    if (!unlockResult.Succeeded)
                    {
                        throw new InvalidOperationException(string.Join(", ", unlockResult.Errors.Select(e => e.Description)));
                    }

                    await _userManager.ResetAccessFailedCountAsync(user);

                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation("Unlocked user {UserId} by {Operator}", user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex, "Failed to unlock user {UserId}", user.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully unlocked {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk unlock users operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> RevokeUserSessionsAsync(
        List<Guid> userIds,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    // 更新安全戳以使所有现有 Cookie 和 Token 无效
                    await _userManager.UpdateSecurityStampAsync(user);

                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation("Revoked sessions for user {UserId} by {Operator}", user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex, "Failed to revoke sessions for user {UserId}", user.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully revoked sessions for {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk revoke sessions operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> ResetPasswordsAsync(
        List<Guid> userIds,
        bool sendEmail = true,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    // 生成密码重置令牌
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                    if (sendEmail && !string.IsNullOrEmpty(user.Email))
                    {
                        // 发送密码重置邮件
                        var resetUrl = $"https://your-domain.com/reset-password"; // 这里应该从配置读取
                        await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, resetUrl);
                    }

                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogInformation("Generated password reset for user {UserId} by {Operator}", user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex, "Failed to reset password for user {UserId}", user.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully reset passwords for {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk reset passwords operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }

    public async Task<BulkOperationResult> DeleteUsersAsync(
        List<Guid> userIds,
        string? operatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalCount = userIds.Count };

        try
        {
            var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                try
                {
                    var deleteResult = await _userManager.DeleteAsync(user);
                    if (!deleteResult.Succeeded)
                    {
                        throw new InvalidOperationException(string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
                    }

                    result.SucceededUserIds.Add(user.Id);
                    result.SuccessCount++;

                    _logger.LogWarning("Deleted user {UserId} by {Operator}", user.Id, operatedBy ?? "System");
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        UserId = user.Id,
                        UserIdentifier = user.Email ?? user.UserName ?? user.Id.ToString(),
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex, "Failed to delete user {UserId}", user.Id);
                }
            }

            result.Success = result.FailureCount == 0;
            result.Message = $"Successfully deleted {result.SuccessCount}/{result.TotalCount} users";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk delete users operation failed");
            result.Success = false;
            result.Message = $"Operation failed: {ex.Message}";
            return result;
        }
    }
}

