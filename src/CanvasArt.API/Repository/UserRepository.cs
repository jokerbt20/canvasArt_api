using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class UserRepository : RepositoryBase, IUserRepository
{
    private const string SelectColumns = """
        u.Id, u.RoleId, u.Email, u.NormalizedEmail, u.PasswordHash, u.FirstName, u.LastName,
        u.PhoneNumber, u.IsActive, u.CreatedAt, u.UpdatedAt, u.LastLoginAt, r.Name AS RoleName
        """;

    public UserRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON r.Id = u.RoleId
            WHERE u.Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<User>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON r.Id = u.RoleId
            WHERE u.NormalizedEmail = @NormalizedEmail;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<User>(Command(sql, new { NormalizedEmail = normalizedEmail }, cancellationToken));
    }

    public async Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE NormalizedEmail = @NormalizedEmail) THEN 1 ELSE 0 END;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(Command(sql, new { NormalizedEmail = normalizedEmail }, cancellationToken));
    }

    public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Users
                (RoleId, Email, NormalizedEmail, PasswordHash, FirstName, LastName, PhoneNumber, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@RoleId, @Email, @NormalizedEmail, @PasswordHash, @FirstName, @LastName, @PhoneNumber, @IsActive, @CreatedAt, @UpdatedAt);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(Command(sql, user, cancellationToken));
    }

    public async Task UpdateLastLoginAsync(int userId, DateTime whenUtc, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Users SET LastLoginAt = @When, UpdatedAt = @When WHERE Id = @UserId;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { UserId = userId, When = whenUtc }, cancellationToken));
    }

    public async Task UpdatePasswordAsync(int userId, string passwordHash, DateTime whenUtc, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Users SET PasswordHash = @PasswordHash, UpdatedAt = @When WHERE Id = @UserId;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { UserId = userId, PasswordHash = passwordHash, When = whenUtc }, cancellationToken));
    }

    public async Task<PagedResult<User>> QueryAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var sortColumn = query.SortBy?.ToLowerInvariant() switch
        {
            "email" => "u.Email",
            "name" => "u.FirstName",
            "lastlogin" => "u.LastLoginAt",
            _ => "u.CreatedAt"
        };
        var direction = query.IsDescending ? "DESC" : "ASC";

        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON r.Id = u.RoleId
            WHERE (@Search IS NULL
                   OR u.Email LIKE @Like
                   OR u.FirstName LIKE @Like
                   OR u.LastName LIKE @Like)
            ORDER BY {sortColumn} {direction}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.Users u
            WHERE (@Search IS NULL
                   OR u.Email LIKE @Like
                   OR u.FirstName LIKE @Like
                   OR u.LastName LIKE @Like);
            """;

        var parameters = new
        {
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<User>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<User>(items, total, query.Page, query.PageSize);
    }
}
