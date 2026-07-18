using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Categories;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface ICategoryRepository
{
    Task<PagedResult<CategoryDto>> QueryAsync(CategoryQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasPaintingsAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
