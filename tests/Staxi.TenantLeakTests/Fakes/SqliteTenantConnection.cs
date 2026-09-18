using System.Data.Common;
using Microsoft.Data.Sqlite;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;

namespace Staxi.TenantLeakTests.Fakes;

public sealed class SqliteConnectionSource : IDbConnectionSource
{
    public DbConnection Create(TenantRegistryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new SqliteConnection(entry.ConnectionString);
    }
}

public sealed class SqliteConnectionScopeVerifier : IConnectionScopeVerifier
{
    public async Task VerifyAsync(DbConnection connection, TenantRegistryEntry entry, string nhanPhamVi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(nhanPhamVi);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT file FROM pragma_database_list WHERE name = 'main';";

        var duongDan = (await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) as string ?? string.Empty;
        var tenFile = Path.GetFileName(duongDan);

        if (!string.Equals(tenFile, entry.DatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new TenantConnectionMismatchException(nhanPhamVi, entry.DatabaseName, tenFile);
        }
    }
}
