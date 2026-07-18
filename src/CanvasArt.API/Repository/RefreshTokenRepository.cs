using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class RefreshTokenRepository : RepositoryBase, IRefreshTokenRepository
{
    public RefreshTokenRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<long> CreateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.RefreshTokens
                (UserId, Token, JwtId, CreatedAt, ExpiresAt, IsUsed, IsRevoked, RevokedAt, ReplacedByToken, CreatedByIp)
            OUTPUT INSERTED.Id
            VALUES
                (@UserId, @Token, @JwtId, @CreatedAt, @ExpiresAt, @IsUsed, @IsRevoked, @RevokedAt, @ReplacedByToken, @CreatedByIp);
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<long>(Command(sql, token, cancellationToken));
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, UserId, Token, JwtId, CreatedAt, ExpiresAt, IsUsed, IsRevoked, RevokedAt, ReplacedByToken, CreatedByIp
            FROM dbo.RefreshTokens
            WHERE Token = @Token;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<RefreshToken>(Command(sql, new { Token = token }, cancellationToken));
    }

    public async Task MarkUsedAndReplacedAsync(long id, string replacedByToken, DateTime whenUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.RefreshTokens
            SET IsUsed = 1, RevokedAt = @When, ReplacedByToken = @ReplacedByToken
            WHERE Id = @Id;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id, ReplacedByToken = replacedByToken, When = whenUtc }, cancellationToken));
    }

    public async Task RevokeAsync(long id, DateTime whenUtc, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.RefreshTokens SET IsRevoked = 1, RevokedAt = @When WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { Id = id, When = whenUtc }, cancellationToken));
    }

    public async Task RevokeAllForUserAsync(int userId, DateTime whenUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.RefreshTokens
            SET IsRevoked = 1, RevokedAt = @When
            WHERE UserId = @UserId AND IsRevoked = 0 AND IsUsed = 0;
            """;
        using var conn = await OpenAsync(cancellationToken);
        await conn.ExecuteAsync(Command(sql, new { UserId = userId, When = whenUtc }, cancellationToken));
    }
}
