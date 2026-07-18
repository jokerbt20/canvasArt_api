using CanvasArt.API.Models.Entities;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class RoleRepository : RepositoryBase, IRoleRepository
{
    private const string Columns = "Id, Name, NormalizedName, Description, CreatedAt";

    public RoleRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {Columns} FROM dbo.Roles WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Role>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {Columns} FROM dbo.Roles WHERE NormalizedName = @NormalizedName;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Role>(Command(sql, new { NormalizedName = normalizedName }, cancellationToken));
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {Columns} FROM dbo.Roles ORDER BY Name;";
        using var conn = await OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Role>(Command(sql, null, cancellationToken))).ToList();
    }
}
