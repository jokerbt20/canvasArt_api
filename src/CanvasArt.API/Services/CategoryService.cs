using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Categories;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IDateTimeProvider _clock;

    public CategoryService(ICategoryRepository categories, IDateTimeProvider clock)
    {
        _categories = categories;
        _clock = clock;
    }

    public Task<PagedResult<CategoryDto>> QueryAsync(CategoryQuery query, CancellationToken cancellationToken = default) =>
        _categories.QueryAsync(query, cancellationToken);

    public Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        _categories.GetAllActiveAsync(cancellationToken);

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _categories.GetByIdAsync(id, cancellationToken)
                     ?? throw new NotFoundException("Category", id);
        return ToDto(entity, 0);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, null, cancellationToken);
        await ValidateParentAsync(request.ParentId, null, cancellationToken);

        var now = _clock.UtcNow;
        var entity = new Category
        {
            ParentId = request.ParentId,
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        entity.Id = await _categories.CreateAsync(entity, cancellationToken);
        return ToDto(entity, 0);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _categories.GetByIdAsync(id, cancellationToken)
                     ?? throw new NotFoundException("Category", id);

        if (request.ParentId == id)
            throw new ValidationException("A category cannot be its own parent.");

        entity.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, id, cancellationToken);
        await ValidateParentAsync(request.ParentId, id, cancellationToken);

        entity.ParentId = request.ParentId;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = _clock.UtcNow;

        await _categories.UpdateAsync(entity, cancellationToken);
        return ToDto(entity, 0);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _categories.ExistsAsync(id, cancellationToken))
            throw new NotFoundException("Category", id);
        if (await _categories.HasChildrenAsync(id, cancellationToken))
            throw new ConflictException("Cannot delete a category that has sub-categories.");
        if (await _categories.HasPaintingsAsync(id, cancellationToken))
            throw new ConflictException("Cannot delete a category that still contains paintings.");

        await _categories.DeleteAsync(id, cancellationToken);
    }

    private async Task ValidateParentAsync(int? parentId, int? selfId, CancellationToken ct)
    {
        if (parentId is null)
            return;
        if (parentId == selfId)
            throw new ValidationException("A category cannot be its own parent.");
        if (!await _categories.ExistsAsync(parentId.Value, ct))
            throw new ValidationException($"Parent category {parentId} does not exist.");
    }

    private async Task<string> EnsureUniqueSlugAsync(string? requested, string name, int? excludeId, CancellationToken ct)
    {
        var slug = SlugHelper.Generate(string.IsNullOrWhiteSpace(requested) ? name : requested!);
        if (string.IsNullOrEmpty(slug))
            throw new ValidationException("Unable to derive a slug from the supplied name.");
        if (await _categories.SlugExistsAsync(slug, excludeId, ct))
            throw new ConflictException($"A category with slug '{slug}' already exists.");
        return slug;
    }

    private static CategoryDto ToDto(Category c, int paintingCount) => new()
    {
        Id = c.Id,
        ParentId = c.ParentId,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        ImagePath = c.ImagePath,
        DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive,
        PaintingCount = paintingCount,
        CreatedAt = c.CreatedAt
    };
}
