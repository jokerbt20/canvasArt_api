using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class TestimonialRepository : RepositoryBase, ITestimonialRepository
{
    private const string Columns =
        "Id, CustomerName, Comment, Rating, ImagePath, ThumbnailPath, DisplayOrder, IsActive, CreatedAt, UpdatedAt";

    public TestimonialRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<IReadOnlyList<Testimonial>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {Columns} FROM dbo.Testimonials
            WHERE (@ActiveOnly = 0 OR IsActive = 1)
            ORDER BY DisplayOrder, Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Testimonial>(Command(sql, new { ActiveOnly = activeOnly ? 1 : 0 }, cancellationToken))).ToList();
    }

    public async Task<Testimonial?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {Columns} FROM dbo.Testimonials WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Testimonial>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateAsync(Testimonial testimonial, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Testimonials (CustomerName, Comment, Rating, ImagePath, ThumbnailPath, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@CustomerName, @Comment, @Rating, @ImagePath, @ThumbnailPath, @DisplayOrder, @IsActive, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, testimonial, cancellationToken));
    }

    public async Task UpdateAsync(Testimonial testimonial, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Testimonials
            SET CustomerName = @CustomerName, Comment = @Comment, Rating = @Rating, ImagePath = @ImagePath, ThumbnailPath = @ThumbnailPath,
                DisplayOrder = @DisplayOrder, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, testimonial, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command("DELETE FROM dbo.Testimonials WHERE Id = @Id;", new { Id = id }, cancellationToken));
    }
}
