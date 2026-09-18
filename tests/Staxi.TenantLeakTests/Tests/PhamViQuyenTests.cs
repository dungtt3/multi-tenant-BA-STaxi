using Microsoft.Extensions.DependencyInjection;
using Staxi.Admin.Application;
using Staxi.Platform.Authorization;
using Staxi.Platform.Tenancy;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViQuyenTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Quyền của hãng A không rò sang hãng B, dù cùng vai trò và cùng tên đăng nhập (AD-17)")]
    public async Task QuyenHaiHang_KhongRoSangNhau()
    {
        var cuaA = await LayQuyenAsync(HaiHangGia.HangA);
        var cuaB = await LayQuyenAsync(HaiHangGia.HangB);

        Assert.True(cuaA.Co(QuyenDanhMuc.XemLoaiXe));
        Assert.True(cuaA.Co(QuyenDanhMuc.ThemLoaiXe));
        Assert.False(cuaA.Co(QuyenDanhMuc.XoaLoaiXe));

        Assert.True(cuaB.Co(QuyenDanhMuc.XoaLoaiXe));
        Assert.False(cuaB.Co(QuyenDanhMuc.XemLoaiXe));
    }

    [Fact(DisplayName = "Quyền gộp từ cả vai trò lẫn quyền riêng (AD-17)")]
    public async Task QuyenGopTuVaiTroVaQuyenRieng()
    {
        var cuaA = await LayQuyenAsync(HaiHangGia.HangA);

        Assert.True(cuaA.Co(QuyenDanhMuc.ThemLoaiXe));
        Assert.True(cuaA.Co(QuyenDanhMuc.SuaLoaiXe));
    }

    [Fact(DisplayName = "Mã quyền cũ không có trong sổ đăng ký thì bị bỏ qua, không làm hỏng cả tập (AD-17)")]
    public async Task MaQuyenCuLa_BiBoQua()
    {
        var cuaA = await LayQuyenAsync(HaiHangGia.HangA);

        Assert.Equal(3, cuaA.SoQuyen);
    }

    [Fact(DisplayName = "permissionVersion khác nhau giữa hai hãng vì tập quyền khác nhau (AD-17)")]
    public async Task PermissionVersion_KhacNhauGiuaHaiHang()
    {
        var cuaA = await LayQuyenAsync(HaiHangGia.HangA);
        var cuaB = await LayQuyenAsync(HaiHangGia.HangB);

        Assert.NotEqual(cuaA.PermissionVersion, cuaB.PermissionVersion);
        Assert.NotEqual(0, cuaA.PermissionVersion);
    }

    [Fact(DisplayName = "Tập quyền rỗng là mặc định an toàn, không phải toàn quyền (AD-17)")]
    public void TapQuyenRong_LaMacDinhAnToan()
        => Assert.False(PermissionSet.Rong.Co(QuyenDanhMuc.XemLoaiXe));

    private async Task<PermissionSet> LayQuyenAsync(string tenantCode)
    {
        using var diScope = _ungDung.Services.CreateScope();
        var nguon = diScope.ServiceProvider.GetRequiredService<IPermissionSource>();

        return await nguon.LayAsync(
            TenantScope.FromSignedToken(tenantCode, 1),
            HaiHangGia.IdQuanTri(tenantCode));
    }
}
