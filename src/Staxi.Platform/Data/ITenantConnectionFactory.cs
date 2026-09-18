using System.Data.Common;

namespace Staxi.Platform.Data;

public interface ITenantConnectionFactory
{
    public Task<DbConnection> OpenAsync(CancellationToken ct = default);
}

public interface IDbConnectionSource
{
    public DbConnection Create(TenantRegistryEntry entry);
}

public interface IConnectionScopeVerifier
{
    public Task VerifyAsync(DbConnection connection, TenantRegistryEntry entry, string nhanPhamVi, CancellationToken ct = default);
}
