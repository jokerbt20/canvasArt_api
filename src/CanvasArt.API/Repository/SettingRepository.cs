using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class SettingRepository : RepositoryBase, ISettingRepository
{
    private const string SelectColumns = "Id, [Key] AS [Key], Value, [Group] AS [Group], Description, UpdatedAt";

    private const string MergeSql = """
        MERGE dbo.Settings AS target
        USING (SELECT @Key AS [Key]) AS source
        ON target.[Key] = source.[Key]
        WHEN MATCHED THEN
            UPDATE SET Value = @Value, [Group] = @Group, Description = @Description, UpdatedAt = @UpdatedAt
        WHEN NOT MATCHED THEN
            INSERT ([Key], Value, [Group], Description, UpdatedAt)
            VALUES (@Key, @Value, @Group, @Description, @UpdatedAt);
        """;

    public SettingRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<IReadOnlyList<Setting>> GetAllAsync(string? group, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns} FROM dbo.Settings
            WHERE (@Group IS NULL OR [Group] = @Group)
            ORDER BY [Group], [Key];
            """;
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Setting>(Command(sql, new { Group = group }, cancellationToken))).ToList();
    }

    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {SelectColumns} FROM dbo.Settings WHERE [Key] = @Key;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Setting>(Command(sql, new { Key = key }, cancellationToken));
    }

    public async Task UpsertAsync(Setting setting, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(MergeSql, setting, cancellationToken));
    }

    public async Task UpsertManyAsync(IReadOnlyList<Setting> settings, CancellationToken cancellationToken = default)
    {
        if (settings.Count == 0)
            return;

        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var setting in settings)
                await conn.ExecuteAsync(Command(MergeSql, setting, cancellationToken, tx));
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
