using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Distributors;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Models.Enums;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class DistributorRepository : RepositoryBase, IDistributorRepository
{
    private const string DistributorColumns = "Id, Name, Email, Phone, IsActive, CreatedAt, UpdatedAt";
    private const string PromoCodeColumns = "Id, DistributorId, Code, DiscountPercentage, IsActive, StartsAt, EndsAt, CreatedAt, UpdatedAt";

    public DistributorRepository(IDbConnectionFactory factory) : base(factory) { }

    // ----- Distributors -----

    public async Task<PagedResult<DistributorDto>> QueryAsync(DistributorQuery query, CancellationToken cancellationToken = default)
    {
        var filters = """
            WHERE (@IsActive IS NULL OR d.IsActive = @IsActive)
              AND (@Search IS NULL OR d.Name LIKE @Like OR d.Email LIKE @Like)
            """;

        var sql = $"""
            SELECT d.Id, d.Name, d.Email, d.Phone, d.IsActive,
                   (SELECT COUNT(1) FROM dbo.PromoCodes pc WHERE pc.DistributorId = d.Id) AS PromoCodeCount,
                   d.CreatedAt
            FROM dbo.Distributors d
            {filters}
            ORDER BY d.Name ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1) FROM dbo.Distributors d {filters};
            """;

        var parameters = new
        {
            query.IsActive,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<DistributorDto>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<DistributorDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<Distributor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {DistributorColumns} FROM dbo.Distributors WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Distributor>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Distributors WHERE Id = @Id) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateAsync(Distributor distributor, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Distributors (Name, Email, Phone, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Email, @Phone, @IsActive, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, distributor, cancellationToken));
    }

    public async Task UpdateAsync(Distributor distributor, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Distributors
            SET Name = @Name, Email = @Email, Phone = @Phone, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, distributor, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            // Promo codes have a non-cascading FK to the distributor, so remove them first. Any
            // orders that referenced these codes / this distributor are SET NULL by their own FKs.
            await conn.ExecuteAsync(Command("DELETE FROM dbo.PromoCodes WHERE DistributorId = @Id;", new { Id = id }, cancellationToken, tx));
            await conn.ExecuteAsync(Command("DELETE FROM dbo.Distributors WHERE Id = @Id;", new { Id = id }, cancellationToken, tx));
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ----- Promo codes -----

    public async Task<IReadOnlyList<PromoCodeDto>> GetPromoCodesByDistributorAsync(int distributorId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT pc.Id, pc.DistributorId, d.Name AS DistributorName, pc.Code, pc.DiscountPercentage, pc.IsActive,
                   pc.StartsAt, pc.EndsAt, pc.CreatedAt,
                   CAST(CASE WHEN pc.IsActive = 1 AND d.IsActive = 1 AND (pc.StartsAt IS NULL OR pc.StartsAt <= SYSUTCDATETIME()) AND (pc.EndsAt IS NULL OR SYSUTCDATETIME() < DATEADD(DAY, 1, CAST(pc.EndsAt AS DATE))) THEN 1 ELSE 0 END AS BIT) AS IsCurrentlyValid
            FROM dbo.PromoCodes pc
            INNER JOIN dbo.Distributors d ON d.Id = pc.DistributorId
            WHERE pc.DistributorId = @DistributorId
            ORDER BY pc.Code ASC;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<PromoCodeDto>(Command(sql, new { DistributorId = distributorId }, cancellationToken))).ToList();
    }

    public async Task<PromoCode?> GetPromoCodeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {PromoCodeColumns} FROM dbo.PromoCodes WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<PromoCode>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<PromoCode?> GetPromoCodeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT pc.Id, pc.DistributorId, pc.Code, pc.DiscountPercentage, pc.IsActive, pc.StartsAt, pc.EndsAt, pc.CreatedAt, pc.UpdatedAt,
                   d.Name AS DistributorName, d.IsActive AS DistributorIsActive
            FROM dbo.PromoCodes pc
            INNER JOIN dbo.Distributors d ON d.Id = pc.DistributorId
            WHERE pc.Code = @Code;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<PromoCode>(Command(sql, new { Code = code }, cancellationToken));
    }

    public async Task<bool> PromoCodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.PromoCodes WHERE Code = @Code AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            ) THEN 1 ELSE 0 END;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Code = code, ExcludeId = excludeId }, cancellationToken));
    }

    public async Task<int> CreatePromoCodeAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.PromoCodes (DistributorId, Code, DiscountPercentage, IsActive, StartsAt, EndsAt, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@DistributorId, @Code, @DiscountPercentage, @IsActive, @StartsAt, @EndsAt, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, promoCode, cancellationToken));
    }

    public async Task UpdatePromoCodeAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.PromoCodes
            SET Code = @Code, DiscountPercentage = @DiscountPercentage, IsActive = @IsActive,
                StartsAt = @StartsAt, EndsAt = @EndsAt, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, promoCode, cancellationToken));
    }

    public async Task DeletePromoCodeAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command("DELETE FROM dbo.PromoCodes WHERE Id = @Id;", new { Id = id }, cancellationToken));
    }

    // ----- Dashboard -----

    public async Task<IReadOnlyList<DistributorSalesRow>> GetSalesDashboardAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        // Attributed orders exclude cancelled ones. Distributors with no sales still appear (LEFT JOIN).
        var sql = $"""
            SELECT d.Id AS DistributorId, d.Name AS DistributorName, d.IsActive,
                   COUNT(o.Id) AS OrderCount,
                   ISNULL(SUM(o.GrandTotal), 0) AS TotalSales,
                   ISNULL(SUM(o.PromoDiscount), 0) AS TotalPromoDiscount
            FROM dbo.Distributors d
            LEFT JOIN dbo.Orders o
                ON o.DistributorId = d.Id
               AND o.Status <> @Cancelled
               AND (@FromDate IS NULL OR o.CreatedAt >= @FromDate)
               AND (@ToDate IS NULL OR o.CreatedAt < @ToDate)
            GROUP BY d.Id, d.Name, d.IsActive
            ORDER BY TotalSales DESC, d.Name ASC;
            """;

        var parameters = new
        {
            Cancelled = (int)OrderStatus.Cancelled,
            FromDate = fromUtc,
            ToDate = toUtc
        };

        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<DistributorSalesRow>(Command(sql, parameters, cancellationToken))).ToList();
    }
}
