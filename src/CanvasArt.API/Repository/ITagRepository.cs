using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Tag tag, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
