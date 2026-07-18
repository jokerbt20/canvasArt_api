using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
}
