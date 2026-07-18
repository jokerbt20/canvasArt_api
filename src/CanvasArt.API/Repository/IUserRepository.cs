using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(int userId, DateTime whenUtc, CancellationToken cancellationToken = default);
    Task UpdatePasswordAsync(int userId, string passwordHash, DateTime whenUtc, CancellationToken cancellationToken = default);
    Task<PagedResult<User>> QueryAsync(PagedQuery query, CancellationToken cancellationToken = default);
}
