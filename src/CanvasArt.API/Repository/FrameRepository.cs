using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Frames;
using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class FrameRepository : RepositoryBase, IFrameRepository
{
    private const string EntityColumns =
        "Id, Code, Name, Material, Color, Description, ImagePath, ThumbnailPath, BasePrice, IsActive, CreatedAt, UpdatedAt";

    public FrameRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<PagedResult<FrameListItemDto>> QueryAsync(FrameQuery query, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var sortColumn = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => "f.Name",
            "price" => "f.BasePrice",
            "material" => "f.Material",
            _ => "f.CreatedAt"
        };
        var direction = query.IsDescending ? "DESC" : "ASC";

        var filters = """
            WHERE (@ActiveOnly = 0 OR f.IsActive = 1)
              AND (@ActiveOnly = 0 OR f.ImagePath IS NOT NULL)
              AND (@IsActive IS NULL OR f.IsActive = @IsActive)
              AND (@HasImage IS NULL OR (@HasImage = 1 AND f.ImagePath IS NOT NULL) OR (@HasImage = 0 AND f.ImagePath IS NULL))
              AND (@Material IS NULL OR f.Material = @Material)
              AND (@Color IS NULL OR f.Color = @Color)
              AND (@Search IS NULL OR f.Name LIKE @Like OR f.Code LIKE @Like OR f.Material LIKE @Like)
              AND (@CompatibleWithPaintingId IS NULL OR EXISTS (
                    SELECT 1 FROM dbo.FrameCompatibilities fc WHERE fc.FrameId = f.Id AND fc.PaintingId = @CompatibleWithPaintingId))
            """;

        var sql = $"""
            SELECT f.Id, f.Code, f.Name, f.Material, f.Color, f.ThumbnailPath, f.BasePrice, f.IsActive
            FROM dbo.Frames f
            {filters}
            ORDER BY {sortColumn} {direction}, f.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1) FROM dbo.Frames f {filters};
            """;

        var parameters = new
        {
            ActiveOnly = activeOnly ? 1 : 0,
            query.IsActive,
            query.HasImage,
            query.Material,
            query.Color,
            query.CompatibleWithPaintingId,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<FrameListItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<FrameListItemDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Frame?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {EntityColumns} FROM dbo.Frames WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Frame>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<IReadOnlyList<Frame>> GetCompatibleFramesAsync(int paintingId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {EntityColumns} FROM dbo.Frames f
            WHERE f.IsActive = 1 AND f.ImagePath IS NOT NULL
              AND EXISTS (SELECT 1 FROM dbo.FrameCompatibilities fc WHERE fc.FrameId = f.Id AND fc.PaintingId = @PaintingId)
            ORDER BY f.Name;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Frame>(Command(sql, new { PaintingId = paintingId }, cancellationToken))).ToList();
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Frames WHERE Code = @Code AND (@ExcludeId IS NULL OR Id <> @ExcludeId)) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Code = code, ExcludeId = excludeId }, cancellationToken));
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Frames WHERE Id = @Id) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<bool> AllExistAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return true;
        const string sql = "SELECT COUNT(1) FROM dbo.Frames WHERE Id IN @Ids;";
        using var conn = await OpenAsync(cancellationToken);
        var count = await conn.ExecuteScalarAsync<int>(Command(sql, new { Ids = ids }, cancellationToken));
        return count == ids.Distinct().Count();
    }

    public async Task<int> CreateAsync(Frame frame, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Frames (Code, Name, Material, Color, Description, ImagePath, ThumbnailPath, BasePrice, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Code, @Name, @Material, @Color, @Description, @ImagePath, @ThumbnailPath, @BasePrice, @IsActive, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, frame, cancellationToken));
    }

    public async Task UpdateAsync(Frame frame, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Frames
            SET Name = @Name, Material = @Material, Color = @Color, Description = @Description,
                BasePrice = @BasePrice, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, frame, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Frames WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task UpdateImagesAsync(int frameId, string imagePath, string thumbnailPath, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Frames SET ImagePath = @ImagePath, ThumbnailPath = @ThumbnailPath, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = frameId, ImagePath = imagePath, ThumbnailPath = thumbnailPath }, cancellationToken));
    }

    public async Task<bool> IsCompatibleAsync(int paintingId, int frameId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.FrameCompatibilities WHERE PaintingId = @PaintingId AND FrameId = @FrameId) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { PaintingId = paintingId, FrameId = frameId }, cancellationToken));
    }
}
