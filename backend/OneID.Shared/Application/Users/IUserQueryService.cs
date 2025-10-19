namespace OneID.Shared.Application.Users;

public interface IUserQueryService
{
    Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken cancellationToken = default);
    Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSummary?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
