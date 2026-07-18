using System.Data;
using Dapper;

namespace CanvasArt.API.Repository;

/// <summary>Shared plumbing for Dapper repositories.</summary>
public abstract class RepositoryBase
{
    protected readonly IDbConnectionFactory ConnectionFactory;

    protected RepositoryBase(IDbConnectionFactory connectionFactory) => ConnectionFactory = connectionFactory;

    protected Task<IDbConnection> OpenAsync(CancellationToken ct) => ConnectionFactory.CreateOpenConnectionAsync(ct);

    protected static CommandDefinition Command(
        string sql, object? param, CancellationToken ct, IDbTransaction? transaction = null) =>
        new(sql, param, transaction, commandTimeout: 30, cancellationToken: ct);
}
