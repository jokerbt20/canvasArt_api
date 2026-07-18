using System.Data;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Paintings;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class PaintingRepository : RepositoryBase, IPaintingRepository
{
    public PaintingRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<PagedResult<PaintingListItemDto>> QueryAsync(PaintingQuery query, bool publishedOnly, CancellationToken cancellationToken = default)
    {
        var sortColumn = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => "p.Name",
            "price" => "FromPrice",
            "featured" => "p.IsFeatured",
            "views" => "p.ViewCount",
            _ => "p.CreatedAt"
        };
        var direction = query.IsDescending ? "DESC" : "ASC";

        var filters = """
            WHERE (@PublishedOnly = 0 OR p.IsPublished = 1)
              AND (@IsPublished IS NULL OR p.IsPublished = @IsPublished)
              AND (@IsFeatured IS NULL OR p.IsFeatured = @IsFeatured)
              AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
              AND (@CategorySlug IS NULL OR c.Slug = @CategorySlug)
              AND (@Color IS NULL OR p.Colors LIKE @ColorLike)
              AND (@Search IS NULL OR p.Name LIKE @Like OR p.Code LIKE @Like OR p.Description LIKE @Like)
              AND (@TagId IS NULL OR EXISTS (SELECT 1 FROM dbo.PaintingTags pt WHERE pt.PaintingId = p.Id AND pt.TagId = @TagId))
              AND (@MinPrice IS NULL OR EXISTS (SELECT 1 FROM dbo.PaintingSizes ps WHERE ps.PaintingId = p.Id AND ps.IsActive = 1 AND ps.Price >= @MinPrice))
              AND (@MaxPrice IS NULL OR EXISTS (SELECT 1 FROM dbo.PaintingSizes ps WHERE ps.PaintingId = p.Id AND ps.IsActive = 1 AND ps.Price <= @MaxPrice))
            """;

        var sql = $"""
            SELECT
                p.Id, p.Code, p.Name, p.Slug, p.CategoryId, c.Name AS CategoryName,
                (SELECT TOP 1 pi.ThumbnailPath FROM dbo.PaintingImages pi WHERE pi.PaintingId = p.Id
                    ORDER BY pi.IsPrimary DESC, pi.DisplayOrder ASC, pi.Id ASC) AS ThumbnailPath,
                ISNULL((SELECT MIN(ps.Price) FROM dbo.PaintingSizes ps WHERE ps.PaintingId = p.Id AND ps.IsActive = 1), 0) AS FromPrice,
                p.IsPublished, p.IsFeatured, p.CreatedAt
            FROM dbo.Paintings p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            {filters}
            ORDER BY {sortColumn} {direction}, p.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.Paintings p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            {filters};
            """;

        var parameters = new
        {
            PublishedOnly = publishedOnly ? 1 : 0,
            query.IsPublished,
            query.IsFeatured,
            query.CategoryId,
            query.CategorySlug,
            query.Color,
            ColorLike = $"%{query.Color}%",
            query.TagId,
            query.MinPrice,
            query.MaxPrice,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<PaintingListItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<PaintingListItemDto>(items, total, query.Page, query.PageSize);
    }

    public Task<PaintingAggregate?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAggregateAsync("p.Id = @Key", id, cancellationToken);

    public Task<PaintingAggregate?> GetAggregateBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        GetAggregateAsync("p.Slug = @Key", slug, cancellationToken);

    private async Task<PaintingAggregate?> GetAggregateAsync(string whereClause, object key, CancellationToken ct)
    {
        var sql = $"""
            SELECT p.Id, p.Code, p.Name, p.Slug, p.Description, p.Context, p.Colors, p.CategoryId,
                   p.IsPublished, p.IsFeatured, p.ViewCount, p.CreatedAt, p.UpdatedAt,
                   c.Name AS CategoryName, c.Slug AS CategorySlug
            FROM dbo.Paintings p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            WHERE {whereClause};

            SELECT ps.Id, ps.PaintingId, ps.Label, ps.WidthCm, ps.HeightCm, ps.Price, ps.Stock, ps.Sku, ps.IsDefault, ps.DisplayOrder, ps.IsActive
            FROM dbo.PaintingSizes ps
            INNER JOIN dbo.Paintings p ON p.Id = ps.PaintingId
            WHERE {whereClause}
            ORDER BY ps.DisplayOrder, ps.Id;

            SELECT pi.Id, pi.PaintingId, pi.OriginalPath, pi.ResizedPath, pi.ThumbnailPath, pi.WatermarkPath,
                   pi.FileName, pi.ContentType, pi.FileSizeBytes, pi.Width, pi.Height, pi.IsPrimary, pi.DisplayOrder, pi.CreatedAt
            FROM dbo.PaintingImages pi
            INNER JOIN dbo.Paintings p ON p.Id = pi.PaintingId
            WHERE {whereClause}
            ORDER BY pi.IsPrimary DESC, pi.DisplayOrder, pi.Id;

            SELECT t.Id, t.Name, t.Slug, t.CreatedAt
            FROM dbo.Tags t
            INNER JOIN dbo.PaintingTags pt ON pt.TagId = t.Id
            INNER JOIN dbo.Paintings p ON p.Id = pt.PaintingId
            WHERE {whereClause}
            ORDER BY t.Name;

            SELECT f.Id, f.Code, f.Name, f.Material, f.Color, f.Description, f.ImagePath, f.ThumbnailPath, f.BasePrice, f.Stock, f.IsActive, f.CreatedAt, f.UpdatedAt
            FROM dbo.Frames f
            INNER JOIN dbo.FrameCompatibilities fc ON fc.FrameId = f.Id
            INNER JOIN dbo.Paintings p ON p.Id = fc.PaintingId
            WHERE {whereClause} AND f.IsActive = 1
            ORDER BY f.Name;
            """;

        using var conn = await OpenAsync(ct);
        using var multi = await conn.QueryMultipleAsync(Command(sql, new { Key = key }, ct));

        var painting = await multi.ReadSingleOrDefaultAsync<Painting>();
        if (painting is null)
            return null;

        var sizes = (await multi.ReadAsync<PaintingSize>()).ToList();
        var images = (await multi.ReadAsync<PaintingImage>()).ToList();
        var tags = (await multi.ReadAsync<Tag>()).ToList();
        var frames = (await multi.ReadAsync<Frame>()).ToList();

        return new PaintingAggregate(painting, sizes, images, tags, frames);
    }

    public async Task<Painting?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Code, Name, Slug, Description, Context, Colors, CategoryId, IsPublished, IsFeatured, ViewCount, CreatedAt, UpdatedAt
            FROM dbo.Paintings WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Painting>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Paintings WHERE Code = @Code AND (@ExcludeId IS NULL OR Id <> @ExcludeId)) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Code = code, ExcludeId = excludeId }, cancellationToken));
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Paintings WHERE Slug = @Slug AND (@ExcludeId IS NULL OR Id <> @ExcludeId)) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Slug = slug, ExcludeId = excludeId }, cancellationToken));
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Paintings WHERE Id = @Id) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateAsync(
        Painting painting, IReadOnlyList<PaintingSize> sizes, IReadOnlyList<int> tagIds,
        IReadOnlyList<int> compatibleFrameIds, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            const string insertSql = """
                INSERT INTO dbo.Paintings (Code, Name, Slug, Description, Context, Colors, CategoryId, IsPublished, IsFeatured, ViewCount, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id
                VALUES (@Code, @Name, @Slug, @Description, @Context, @Colors, @CategoryId, @IsPublished, @IsFeatured, 0, @CreatedAt, @UpdatedAt);
                """;
            var id = await conn.ExecuteScalarAsync<int>(Command(insertSql, painting, cancellationToken, tx));

            await InsertChildrenAsync(conn, tx, id, sizes, tagIds, compatibleFrameIds, cancellationToken);

            tx.Commit();
            return id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(
        Painting painting, IReadOnlyList<PaintingSize> sizes, IReadOnlyList<int> tagIds,
        IReadOnlyList<int> compatibleFrameIds, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            const string updateSql = """
                UPDATE dbo.Paintings
                SET Name = @Name, Slug = @Slug, Description = @Description, Context = @Context, Colors = @Colors,
                    CategoryId = @CategoryId, IsPublished = @IsPublished, IsFeatured = @IsFeatured, UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
                """;
            await conn.ExecuteAsync(Command(updateSql, painting, cancellationToken, tx));

            await conn.ExecuteAsync(Command("DELETE FROM dbo.PaintingSizes WHERE PaintingId = @Id;", new { painting.Id }, cancellationToken, tx));
            await conn.ExecuteAsync(Command("DELETE FROM dbo.PaintingTags WHERE PaintingId = @Id;", new { painting.Id }, cancellationToken, tx));
            await conn.ExecuteAsync(Command("DELETE FROM dbo.FrameCompatibilities WHERE PaintingId = @Id;", new { painting.Id }, cancellationToken, tx));

            await InsertChildrenAsync(conn, tx, painting.Id, sizes, tagIds, compatibleFrameIds, cancellationToken);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static async Task InsertChildrenAsync(
        IDbConnection conn, IDbTransaction tx, int paintingId,
        IReadOnlyList<PaintingSize> sizes, IReadOnlyList<int> tagIds, IReadOnlyList<int> frameIds, CancellationToken ct)
    {
        if (sizes.Count > 0)
        {
            const string sizeSql = """
                INSERT INTO dbo.PaintingSizes (PaintingId, Label, WidthCm, HeightCm, Price, Stock, Sku, IsDefault, DisplayOrder, IsActive)
                VALUES (@PaintingId, @Label, @WidthCm, @HeightCm, @Price, @Stock, @Sku, @IsDefault, @DisplayOrder, @IsActive);
                """;
            var rows = sizes.Select(s => new
            {
                PaintingId = paintingId,
                s.Label, s.WidthCm, s.HeightCm, s.Price, s.Stock, s.Sku, s.IsDefault, s.DisplayOrder, s.IsActive
            });
            await conn.ExecuteAsync(new CommandDefinition(sizeSql, rows, tx, cancellationToken: ct));
        }

        if (tagIds.Count > 0)
        {
            const string tagSql = "INSERT INTO dbo.PaintingTags (PaintingId, TagId) VALUES (@PaintingId, @TagId);";
            var rows = tagIds.Select(t => new { PaintingId = paintingId, TagId = t });
            await conn.ExecuteAsync(new CommandDefinition(tagSql, rows, tx, cancellationToken: ct));
        }

        if (frameIds.Count > 0)
        {
            const string frameSql = "INSERT INTO dbo.FrameCompatibilities (PaintingId, FrameId, CreatedAt) VALUES (@PaintingId, @FrameId, SYSUTCDATETIME());";
            var rows = frameIds.Select(f => new { PaintingId = paintingId, FrameId = f });
            await conn.ExecuteAsync(new CommandDefinition(frameSql, rows, tx, cancellationToken: ct));
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Paintings WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task IncrementViewCountAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Paintings SET ViewCount = ViewCount + 1 WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<PaintingSize?> GetSizeAsync(int paintingSizeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, PaintingId, Label, WidthCm, HeightCm, Price, Stock, Sku, IsDefault, DisplayOrder, IsActive
            FROM dbo.PaintingSizes WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<PaintingSize>(Command(sql, new { Id = paintingSizeId }, cancellationToken));
    }

    public async Task<string?> GetPrimaryThumbnailAsync(int paintingId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 1 ThumbnailPath FROM dbo.PaintingImages
            WHERE PaintingId = @PaintingId
            ORDER BY IsPrimary DESC, DisplayOrder ASC, Id ASC;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<string?>(Command(sql, new { PaintingId = paintingId }, cancellationToken));
    }

    public async Task<int> AddImageAsync(PaintingImage image, bool makePrimaryIfFirst, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @Count INT = (SELECT COUNT(1) FROM dbo.PaintingImages WHERE PaintingId = @PaintingId);
            DECLARE @NextOrder INT = ISNULL((SELECT MAX(DisplayOrder) FROM dbo.PaintingImages WHERE PaintingId = @PaintingId), -1) + 1;
            DECLARE @Primary BIT = CASE WHEN @Count = 0 AND @MakePrimaryIfFirst = 1 THEN 1 ELSE 0 END;

            INSERT INTO dbo.PaintingImages
                (PaintingId, OriginalPath, ResizedPath, ThumbnailPath, WatermarkPath, FileName, ContentType, FileSizeBytes, Width, Height, IsPrimary, DisplayOrder, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@PaintingId, @OriginalPath, @ResizedPath, @ThumbnailPath, @WatermarkPath, @FileName, @ContentType, @FileSizeBytes, @Width, @Height, @Primary, @NextOrder, @CreatedAt);
            """;
        var parameters = new
        {
            image.PaintingId, image.OriginalPath, image.ResizedPath, image.ThumbnailPath, image.WatermarkPath,
            image.FileName, image.ContentType, image.FileSizeBytes, image.Width, image.Height, image.CreatedAt,
            MakePrimaryIfFirst = makePrimaryIfFirst ? 1 : 0
        };
        using var conn = await OpenAsync(cancellationToken);
        var id = await conn.ExecuteScalarAsync<int>(Command(sql, parameters, cancellationToken));

        // Reflect computed values back for the caller.
        image.Id = id;
        var row = await GetImageAsync(id, cancellationToken);
        if (row is not null)
        {
            image.IsPrimary = row.IsPrimary;
            image.DisplayOrder = row.DisplayOrder;
        }
        return id;
    }

    public async Task<PaintingImage?> GetImageAsync(int imageId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, PaintingId, OriginalPath, ResizedPath, ThumbnailPath, WatermarkPath, FileName, ContentType, FileSizeBytes, Width, Height, IsPrimary, DisplayOrder, CreatedAt
            FROM dbo.PaintingImages WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<PaintingImage>(Command(sql, new { Id = imageId }, cancellationToken));
    }

    public async Task<IReadOnlyList<PaintingImage>> GetImagesAsync(int paintingId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, PaintingId, OriginalPath, ResizedPath, ThumbnailPath, WatermarkPath, FileName, ContentType, FileSizeBytes, Width, Height, IsPrimary, DisplayOrder, CreatedAt
            FROM dbo.PaintingImages WHERE PaintingId = @PaintingId
            ORDER BY IsPrimary DESC, DisplayOrder, Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<PaintingImage>(Command(sql, new { PaintingId = paintingId }, cancellationToken))).ToList();
    }

    public async Task DeleteImageAsync(int imageId, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            var wasPrimary = await conn.ExecuteScalarAsync<int>(Command(
                "SELECT ISNULL((SELECT CAST(IsPrimary AS INT) FROM dbo.PaintingImages WHERE Id = @Id), 0);",
                new { Id = imageId }, cancellationToken, tx));
            var paintingId = await conn.ExecuteScalarAsync<int?>(Command(
                "SELECT PaintingId FROM dbo.PaintingImages WHERE Id = @Id;", new { Id = imageId }, cancellationToken, tx));

            await conn.ExecuteAsync(Command("DELETE FROM dbo.PaintingImages WHERE Id = @Id;", new { Id = imageId }, cancellationToken, tx));

            // Promote another image to primary if we removed the primary one.
            if (wasPrimary == 1 && paintingId is int pid)
            {
                await conn.ExecuteAsync(Command("""
                    UPDATE dbo.PaintingImages
                    SET IsPrimary = 1
                    WHERE Id = (SELECT TOP 1 Id FROM dbo.PaintingImages WHERE PaintingId = @PaintingId ORDER BY DisplayOrder, Id);
                    """, new { PaintingId = pid }, cancellationToken, tx));
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task SetPrimaryImageAsync(int paintingId, int imageId, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(Command("UPDATE dbo.PaintingImages SET IsPrimary = 0 WHERE PaintingId = @PaintingId;", new { PaintingId = paintingId }, cancellationToken, tx));
            await conn.ExecuteAsync(Command("UPDATE dbo.PaintingImages SET IsPrimary = 1 WHERE Id = @Id AND PaintingId = @PaintingId;", new { Id = imageId, PaintingId = paintingId }, cancellationToken, tx));
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
