using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class TagRepository : RepositoryBase, ITagRepository
{
    public TagRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, Name, Slug, CreatedAt FROM dbo.Tags ORDER BY Name;";
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Tag>(Command(sql, null, cancellationToken))).ToList();
    }

    public async Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, Name, Slug, CreatedAt FROM dbo.Tags WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Tag>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Tags WHERE Slug = @Slug AND (@ExcludeId IS NULL OR Id <> @ExcludeId)) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Slug = slug, ExcludeId = excludeId }, cancellationToken));
    }

    public async Task<int> CreateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Tags (Name, Slug, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Slug, @CreatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, tag, cancellationToken));
    }

    public async Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Tags SET Name = @Name, Slug = @Slug WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, tag, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Tags WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Tags WHERE Id = @Id) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { Id = id }, cancellationToken));
    }
}
