using System.Data;

namespace CanvasArt.API.Repository;

/// <summary>Creates open ADO.NET connections for Dapper. The only DB access abstraction.</summary>
public interface IDbConnectionFactory
{
    /// <summary>Creates and opens a new connection to the configured database.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
