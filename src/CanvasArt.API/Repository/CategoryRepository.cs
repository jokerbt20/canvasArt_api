using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Categories;
using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class CategoryRepository : RepositoryBase, ICategoryRepository
{
    private const string EntityColumns =
        "Id, ParentId, Name, Slug, Description, ImagePath, DisplayOrder, IsActive, CreatedAt, UpdatedAt";

    public CategoryRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<PagedResult<CategoryDto>> QueryAsync(CategoryQuery query, CancellationToken cancellationToken = default)
    {
        var sortColumn = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => "c.Name",
            "order" => "c.DisplayOrder",
            _ => "c.DisplayOrder"
        };
        var direction = query.IsDescending ? "DESC" : "ASC";

        var sql = $"""
            SELECT
                c.Id, c.ParentId, c.Name, c.Slug, c.Description, c.ImagePath, c.DisplayOrder, c.IsActive, c.CreatedAt,
                (SELECT COUNT(1) FROM dbo.Paintings p WHERE p.CategoryId = c.Id) AS PaintingCount
            FROM dbo.Categories c
            WHERE (@IsActive IS NULL OR c.IsActive = @IsActive)
              AND (@HasParent = 0 OR c.ParentId = @ParentId)
              AND (@Search IS NULL OR c.Name LIKE @Like OR c.Slug LIKE @Like)
            ORDER BY {sortColumn} {direction}, c.Name ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.Categories c
            WHERE (@IsActive IS NULL OR c.IsActive = @IsActive)
              AND (@HasParent = 0 OR c.ParentId = @ParentId)
              AND (@Search IS NULL OR c.Name LIKE @Like OR c.Slug LIKE @Like);
            """;

        var parameters = new
        {
            query.IsActive,
            HasParent = query.ParentId.HasValue ? 1 : 0,
            query.ParentId,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<CategoryDto>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<CategoryDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.Id, c.ParentId, c.Name, c.Slug, c.Description, c.ImagePath, c.DisplayOrder, c.IsActive, c.CreatedAt,
                (SELECT COUNT(1) FROM dbo.Paintings p WHERE p.CategoryId = c.Id AND p.IsPublished = 1) AS PaintingCount
            FROM dbo.Categories c
            WHERE c.IsActive = 1
            ORDER BY c.DisplayOrder ASC, c.Name ASC;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<CategoryDto>(Command(sql, null, cancellationToken))).ToList();
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {EntityColumns} FROM dbo.Categories WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Category>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {EntityColumns} FROM dbo.Categories WHERE Slug = @Slug;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Category>(Command(sql, new { Slug = slug }, cancellationToken));
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = @Slug AND (@ExcludeId IS NULL OR Id <> @ExcludeId)) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Slug = slug, ExcludeId = excludeId }, cancellationToken));
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Categories WHERE Id = @Id) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Categories WHERE ParentId = @Id) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<bool> HasPaintingsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Paintings WHERE CategoryId = @Id) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Categories (ParentId, Name, Slug, Description, ImagePath, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@ParentId, @Name, @Slug, @Description, @ImagePath, @DisplayOrder, @IsActive, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, category, cancellationToken));
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Categories
            SET ParentId = @ParentId, Name = @Name, Slug = @Slug, Description = @Description,
                ImagePath = @ImagePath, DisplayOrder = @DisplayOrder, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, category, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Categories WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id }, cancellationToken));
    }
}
