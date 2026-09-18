namespace Staxi.Platform.Data;

public sealed record TenantRegistryEntry(
    string TenantCode,
    string ConnectionString,
    string DatabaseName,
    bool DangHoatDong = true);

public interface ITenantRegistry
{
    public ValueTask<TenantRegistryEntry> GetAsync(string tenantCode, CancellationToken ct = default);
}
