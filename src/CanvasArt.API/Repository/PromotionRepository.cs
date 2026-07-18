using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Promotions;
using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class PromotionRepository : RepositoryBase, IPromotionRepository
{
    private const string SingleColumns =
        "Id, Name, Description, PromotionType, DiscountType, DiscountValue, TargetPaintingId, TargetFrameId, TargetCategoryId, StartDate, EndDate, IsActive, Priority, CreatedAt, UpdatedAt";

    private const string ComboColumns =
        "Id, Name, Description, PaintingId, FrameId, DiscountType, DiscountValue, StartDate, EndDate, IsActive, Priority, CreatedAt, UpdatedAt";

    public PromotionRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<PagedResult<PromotionDto>> QueryAsync(PromotionQuery query, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var filters = """
            WHERE (@PromotionType IS NULL OR pr.PromotionType = @PromotionType)
              AND (@IsActive IS NULL OR pr.IsActive = @IsActive)
              AND (@OnlyCurrent = 0 OR (pr.IsActive = 1 AND pr.StartDate <= @Now AND pr.EndDate >= @Now))
              AND (@Search IS NULL OR pr.Name LIKE @Like)
            """;

        var sql = $"""
            SELECT pr.Id, pr.Name, pr.Description, pr.PromotionType, pr.DiscountType, pr.DiscountValue,
                   pr.TargetPaintingId, pr.TargetFrameId, pr.TargetCategoryId, pr.StartDate, pr.EndDate,
                   pr.IsActive,
                   CAST(CASE WHEN pr.IsActive = 1 AND pr.StartDate <= @Now AND pr.EndDate >= @Now THEN 1 ELSE 0 END AS BIT) AS IsCurrentlyActive,
                   pr.Priority, pr.CreatedAt
            FROM dbo.Promotions pr
            {filters}
            ORDER BY pr.Priority DESC, pr.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1) FROM dbo.Promotions pr {filters};
            """;

        var parameters = new
        {
            PromotionType = (int?)query.PromotionType,
            query.IsActive,
            OnlyCurrent = query.OnlyCurrentlyActive == true ? 1 : 0,
            Now = nowUtc,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<PromotionDto>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<PromotionDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {SingleColumns} FROM dbo.Promotions WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Promotion>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Promotions
                (Name, Description, PromotionType, DiscountType, DiscountValue, TargetPaintingId, TargetFrameId, TargetCategoryId, StartDate, EndDate, IsActive, Priority, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@Name, @Description, @PromotionType, @DiscountType, @DiscountValue, @TargetPaintingId, @TargetFrameId, @TargetCategoryId, @StartDate, @EndDate, @IsActive, @Priority, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, promotion, cancellationToken));
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Promotions
            SET Name = @Name, Description = @Description, PromotionType = @PromotionType, DiscountType = @DiscountType,
                DiscountValue = @DiscountValue, TargetPaintingId = @TargetPaintingId, TargetFrameId = @TargetFrameId,
                TargetCategoryId = @TargetCategoryId, StartDate = @StartDate, EndDate = @EndDate, IsActive = @IsActive,
                Priority = @Priority, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, promotion, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command("DELETE FROM dbo.Promotions WHERE Id = @Id;", new { Id = id }, cancellationToken));
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SingleColumns} FROM dbo.Promotions
            WHERE IsActive = 1 AND StartDate <= @Now AND EndDate >= @Now
            ORDER BY Priority DESC;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Promotion>(Command(sql, new { Now = nowUtc }, cancellationToken))).ToList();
    }

    // ----- Combination promotions -----

    public async Task<PagedResult<CombinationPromotionDto>> QueryCombinationsAsync(PromotionQuery query, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var filters = """
            WHERE (@IsActive IS NULL OR cp.IsActive = @IsActive)
              AND (@OnlyCurrent = 0 OR (cp.IsActive = 1 AND cp.StartDate <= @Now AND cp.EndDate >= @Now))
              AND (@Search IS NULL OR cp.Name LIKE @Like)
            """;

        var sql = $"""
            SELECT cp.Id, cp.Name, cp.Description, cp.PaintingId, p.Name AS PaintingName, cp.FrameId, f.Name AS FrameName,
                   cp.DiscountType, cp.DiscountValue, cp.StartDate, cp.EndDate, cp.IsActive,
                   CAST(CASE WHEN cp.IsActive = 1 AND cp.StartDate <= @Now AND cp.EndDate >= @Now THEN 1 ELSE 0 END AS BIT) AS IsCurrentlyActive,
                   cp.Priority, cp.CreatedAt
            FROM dbo.CombinationPromotions cp
            INNER JOIN dbo.Paintings p ON p.Id = cp.PaintingId
            INNER JOIN dbo.Frames f ON f.Id = cp.FrameId
            {filters}
            ORDER BY cp.Priority DESC, cp.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1) FROM dbo.CombinationPromotions cp {filters};
            """;

        var parameters = new
        {
            query.IsActive,
            OnlyCurrent = query.OnlyCurrentlyActive == true ? 1 : 0,
            Now = nowUtc,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<CombinationPromotionDto>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<CombinationPromotionDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<CombinationPromotion?> GetCombinationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {ComboColumns} FROM dbo.CombinationPromotions WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<CombinationPromotion>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateCombinationAsync(CombinationPromotion promotion, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.CombinationPromotions
                (Name, Description, PaintingId, FrameId, DiscountType, DiscountValue, StartDate, EndDate, IsActive, Priority, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@Name, @Description, @PaintingId, @FrameId, @DiscountType, @DiscountValue, @StartDate, @EndDate, @IsActive, @Priority, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, promotion, cancellationToken));
    }

    public async Task UpdateCombinationAsync(CombinationPromotion promotion, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.CombinationPromotions
            SET Name = @Name, Description = @Description, PaintingId = @PaintingId, FrameId = @FrameId,
                DiscountType = @DiscountType, DiscountValue = @DiscountValue, StartDate = @StartDate, EndDate = @EndDate,
                IsActive = @IsActive, Priority = @Priority, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, promotion, cancellationToken));
    }

    public async Task DeleteCombinationAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command("DELETE FROM dbo.CombinationPromotions WHERE Id = @Id;", new { Id = id }, cancellationToken));
    }

    public async Task<IReadOnlyList<CombinationPromotion>> GetActiveCombinationsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {ComboColumns} FROM dbo.CombinationPromotions
            WHERE IsActive = 1 AND StartDate <= @Now AND EndDate >= @Now
            ORDER BY Priority DESC;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<CombinationPromotion>(Command(sql, new { Now = nowUtc }, cancellationToken))).ToList();
    }
}
