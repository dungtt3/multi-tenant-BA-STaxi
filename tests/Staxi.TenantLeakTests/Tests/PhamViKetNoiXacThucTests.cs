using Microsoft.Extensions.DependencyInjection;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViKetNoiXacThucTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Đường xác thực mở đúng database của hãng được yêu cầu (AD-3, AD-18)")]
    public async Task DuongXacThuc_MoDungDatabaseCuaHang()
    {
        Assert.Equal("hang_a.db", await TenDatabaseAsync(HaiHangGia.HangA));
        Assert.Equal("hang_b.db", await TenDatabaseAsync(HaiHangGia.HangB));
    }

    [Fact(DisplayName = "Đường xác thực bắt buộc khai lý do (AD-2)")]
    public async Task DuongXacThuc_BatBuocKhaiLyDo()
    {
        var factory = LayFactory();

        await Assert.ThrowsAsync<ArgumentException>(
            () => factory.OpenForAuthenticationAsync(HaiHangGia.HangA, "   "));
    }

    [Fact(DisplayName = "Đường xác thực KHÔNG để lại TenantScope nào sau khi chạy (AD-4)")]
    public async Task DuongXacThuc_KhongDeLaiPhamVi()
    {
        var accessor = _ungDung.Services.GetRequiredService<ITenantScopeAccessor>();
        var factory = LayFactory();

        await using var ketNoi = await factory.OpenForAuthenticationAsync(HaiHangGia.HangA, "đăng nhập");

        Assert.Null(accessor.Current);
    }

    [Fact(DisplayName = "Hãng không có trong sổ đăng ký thì không mở được kết nối xác thực (AD-14)")]
    public async Task HangKhongCoTrongSo_ThiKhongMoDuoc()
    {
        var factory = LayFactory();

        await Assert.ThrowsAsync<TenantKhongTonTaiException>(
            () => factory.OpenForAuthenticationAsync("hang-khong-ton-tai", "đăng nhập"));
    }

    private IAuthenticationConnectionFactory LayFactory()
        => _ungDung.Services.CreateScope().ServiceProvider.GetRequiredService<IAuthenticationConnectionFactory>();

    private async Task<string> TenDatabaseAsync(string tenantCode)
    {
        await using var ketNoi = await LayFactory().OpenForAuthenticationAsync(tenantCode, "đăng nhập");
        await using var lenh = ketNoi.CreateCommand();
        lenh.CommandText = "SELECT file FROM pragma_database_list WHERE name = 'main';";

        return Path.GetFileName((await lenh.ExecuteScalarAsync()) as string ?? string.Empty);
    }
}
