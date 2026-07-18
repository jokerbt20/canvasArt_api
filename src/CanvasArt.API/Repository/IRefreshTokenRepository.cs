using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IRefreshTokenRepository
{
    Task<long> CreateAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task MarkUsedAndReplacedAsync(long id, string replacedByToken, DateTime whenUtc, CancellationToken cancellationToken = default);
    Task RevokeAsync(long id, DateTime whenUtc, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(int userId, DateTime whenUtc, CancellationToken cancellationToken = default);
}
