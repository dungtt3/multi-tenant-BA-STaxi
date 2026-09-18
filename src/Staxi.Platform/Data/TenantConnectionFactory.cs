using System.Data.Common;
using Microsoft.Extensions.Logging;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Data;

public sealed class TenantConnectionFactory(
    ITenantScopeAccessor scopeAccessor,
    ITenantRegistry registry,
    IDbConnectionSource connectionSource,
    IConnectionScopeVerifier verifier,
    ILogger<TenantConnectionFactory> logger) : ITenantConnectionFactory
{
    public async Task<DbConnection> OpenAsync(CancellationToken ct = default)
    {
        var scope = scopeAccessor.Required(nameof(ITenantConnectionFactory) + "." + nameof(OpenAsync));

        var entry = await registry.GetAsync(scope.TenantCode, ct).ConfigureAwait(false);

        var connection = connectionSource.Create(entry);

        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await verifier.VerifyAsync(connection, entry, scope, ct).ConfigureAwait(false);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        logger.LogDebug("Mở kết nối cho phạm vi {Scope} tới database {Database}.", scope.Prefix, entry.DatabaseName);

        return connection;
    }
}
