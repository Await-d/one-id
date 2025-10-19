namespace OneID.Shared.Application.Users;

public interface IUserCommandService
{
    Task<UserSummary> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserSummary> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
