using JobsApi.Domain.Entities;   // ← this using is needed (User is in Domain)

namespace JobsApi.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
    Task<bool> ExistsAsync(string email, CancellationToken ct);
}