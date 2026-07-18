using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Contact;
using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class ContactMessageRepository : RepositoryBase, IContactMessageRepository
{
    private const string EntityColumns = "Id, Name, Email, Subject, Message, IsRead, CreatedAt";

    public ContactMessageRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<int> CreateAsync(ContactMessage message, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ContactMessages (Name, Email, Subject, Message, IsRead, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Email, @Subject, @Message, @IsRead, @CreatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, message, cancellationToken));
    }

    public async Task<PagedResult<ContactMessage>> QueryAsync(ContactMessageQuery query, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {EntityColumns}
            FROM dbo.ContactMessages
            WHERE (@IsRead IS NULL OR IsRead = @IsRead)
              AND (@Search IS NULL OR Name LIKE @Like OR Email LIKE @Like OR Subject LIKE @Like)
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.ContactMessages
            WHERE (@IsRead IS NULL OR IsRead = @IsRead)
              AND (@Search IS NULL OR Name LIKE @Like OR Email LIKE @Like OR Subject LIKE @Like);
            """;

        var parameters = new
        {
            query.IsRead,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<ContactMessage>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<ContactMessage>(items, total, query.Page, query.PageSize);
    }

    public async Task<ContactMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {EntityColumns} FROM dbo.ContactMessages WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<ContactMessage>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.ContactMessages SET IsRead = 1 WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id }, cancellationToken));
    }
}
