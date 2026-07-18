using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class SlideRepository : RepositoryBase, ISlideRepository
{
    private const string Columns =
        "Id, Title, Subtitle, ImagePath, LinkUrl, ButtonText, DisplayOrder, IsActive, StartDate, EndDate, CreatedAt, UpdatedAt";

    public SlideRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<IReadOnlyList<Slide>> GetAllAsync(bool activeOnly, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {Columns} FROM dbo.Slides
            WHERE (@ActiveOnly = 0 OR (IsActive = 1
                   AND (StartDate IS NULL OR StartDate <= @Now)
                   AND (EndDate IS NULL OR EndDate >= @Now)))
            ORDER BY DisplayOrder, Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Slide>(Command(sql, new { ActiveOnly = activeOnly ? 1 : 0, Now = nowUtc }, cancellationToken))).ToList();
    }

    public async Task<Slide?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {Columns} FROM dbo.Slides WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Slide>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateAsync(Slide slide, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Slides (Title, Subtitle, ImagePath, LinkUrl, ButtonText, DisplayOrder, IsActive, StartDate, EndDate, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Subtitle, @ImagePath, @LinkUrl, @ButtonText, @DisplayOrder, @IsActive, @StartDate, @EndDate, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, slide, cancellationToken));
    }

    public async Task UpdateAsync(Slide slide, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Slides
            SET Title = @Title, Subtitle = @Subtitle, ImagePath = @ImagePath, LinkUrl = @LinkUrl, ButtonText = @ButtonText,
                DisplayOrder = @DisplayOrder, IsActive = @IsActive, StartDate = @StartDate, EndDate = @EndDate, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, slide, cancellationToken));
    }

    public async Task UpdateImageAsync(int id, string imagePath, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Slides SET ImagePath = @ImagePath, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id, ImagePath = imagePath }, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command("DELETE FROM dbo.Slides WHERE Id = @Id;", new { Id = id }, cancellationToken));
    }
}
