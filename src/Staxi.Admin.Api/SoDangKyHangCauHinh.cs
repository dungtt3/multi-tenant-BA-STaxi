using Staxi.Platform.Data;

namespace Staxi.Admin.Api;

internal sealed class DongSoDangKy
{
    public string TenantCode { get; init; } = string.Empty;

    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = string.Empty;

    public bool DangHoatDong { get; init; } = true;
}

internal static class SoDangKyHangCauHinh
{
    public static ITenantRegistryStore Doc(IConfiguration cauHinh)
    {
        var cacDong = cauHinh.GetSection("SoDangKyHang").Get<DongSoDangKy[]>() ?? [];

        var entries = cacDong.Select(dong => new TenantRegistryEntry(
            dong.TenantCode,
            dong.ConnectionString,
            dong.DatabaseName,
            dong.DangHoatDong));

        return new InMemoryTenantRegistryStore(entries);
    }
}
