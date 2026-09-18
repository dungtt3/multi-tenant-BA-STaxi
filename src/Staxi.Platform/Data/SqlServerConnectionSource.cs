using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Staxi.Platform.Data;

public sealed class SqlServerConnectionSource : IDbConnectionSource
{
    public DbConnection Create(TenantRegistryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new SqlConnection(entry.ConnectionString);
    }
}

public sealed class SqlServerConnectionScopeVerifier : IConnectionScopeVerifier
{
    public async Task VerifyAsync(DbConnection connection, TenantRegistryEntry entry, string nhanPhamVi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(nhanPhamVi);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_NAME();";

        var databaseThucTe = (await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) as string ?? string.Empty;

        if (!string.Equals(databaseThucTe, entry.DatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new Tenancy.TenantConnectionMismatchException(nhanPhamVi, entry.DatabaseName, databaseThucTe);
        }
    }
}
