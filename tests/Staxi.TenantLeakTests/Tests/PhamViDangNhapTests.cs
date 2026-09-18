using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Staxi.Auth.Application;
using Staxi.Platform.Tenancy;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViDangNhapTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Đăng nhập đúng hãng thì token mang đúng phạm vi của hãng đó (AD-3, AD-4)")]
    public async Task DangNhapDungHang_TokenMangDungPhamVi()
    {
        var ketQua = await DangNhapAsync(HaiHangGia.HangA, "quantri", HaiHangGia.MatKhauHangA);

        Assert.True(ketQua.ThanhCong);
        Assert.NotNull(ketQua.Token);

        var scope = TenantScopeClaims.Read(DocToken(ketQua.Token));

        Assert.NotNull(scope);
        Assert.Equal(HaiHangGia.HangA, scope.TenantCode);
        Assert.Equal(1, scope.CompanyId);
    }

    [Fact(DisplayName = "Mật khẩu của hãng A không đăng nhập được vào hãng B, dù trùng tên đăng nhập (AD-2, AD-3)")]
    public async Task MatKhauHangA_KhongVaoDuocHangB()
    {
        var sang = await DangNhapAsync(HaiHangGia.HangB, "quantri", HaiHangGia.MatKhauHangA);

        Assert.False(sang.ThanhCong);
        Assert.Null(sang.Token);

        var dung = await DangNhapAsync(HaiHangGia.HangB, "quantri", HaiHangGia.MatKhauHangB);

        Assert.True(dung.ThanhCong);
    }

    [Fact(DisplayName = "Cùng tên đăng nhập ở hai hãng là hai người khác nhau (AD-3)")]
    public async Task CungTenDangNhap_LaHaiNguoiKhacNhau()
    {
        var cuaA = await DangNhapAsync(HaiHangGia.HangA, "quantri", HaiHangGia.MatKhauHangA);
        var cuaB = await DangNhapAsync(HaiHangGia.HangB, "quantri", HaiHangGia.MatKhauHangB);

        var scopeA = TenantScopeClaims.Read(DocToken(cuaA.Token!));
        var scopeB = TenantScopeClaims.Read(DocToken(cuaB.Token!));

        Assert.NotEqual(scopeA, scopeB);
    }

    [Fact(DisplayName = "Tài khoản bị khoá hoặc đã xoá không đăng nhập được")]
    public async Task TaiKhoanBiKhoaHoacDaXoa_KhongVaoDuoc()
    {
        Assert.False((await DangNhapAsync(HaiHangGia.HangA, "bikhoa", HaiHangGia.MatKhauBiKhoa)).ThanhCong);
        Assert.False((await DangNhapAsync(HaiHangGia.HangA, "daxoa", HaiHangGia.MatKhauHangA)).ThanhCong);
    }

    [Fact(DisplayName = "Mã hãng không tồn tại và mật khẩu sai cho cùng một kết quả (AD-3)")]
    public async Task MaHangSaiVaMatKhauSai_ChoCungKetQua()
    {
        var hangLa = await DangNhapAsync("hang-khong-co-that", "quantri", HaiHangGia.MatKhauHangA);
        var matKhauSai = await DangNhapAsync(HaiHangGia.HangA, "quantri", "sai-bet");
        var userLa = await DangNhapAsync(HaiHangGia.HangA, "khong-co-nguoi-nay", HaiHangGia.MatKhauHangA);

        Assert.False(hangLa.ThanhCong);
        Assert.False(matKhauSai.ThanhCong);
        Assert.False(userLa.ThanhCong);
        Assert.Null(hangLa.Token);
        Assert.Null(matKhauSai.Token);
        Assert.Null(userLa.Token);
    }

    [Fact(DisplayName = "Người dùng không tồn tại vẫn tốn công băm mật khẩu (AD-3, chống dò tên đăng nhập)")]
    public async Task NguoiDungKhongTonTai_VanBamMatKhau()
    {
        var coNguoi = await DoThoiGianAsync(() => DangNhapAsync(HaiHangGia.HangA, "quantri", "sai-bet"));
        var khongCoNguoi = await DoThoiGianAsync(() => DangNhapAsync(HaiHangGia.HangA, "khong-co-nguoi-nay", "sai-bet"));

        var chenhLech = Math.Abs(coNguoi - khongCoNguoi);
        var lonHon = Math.Max(coNguoi, khongCoNguoi);

        Assert.True(
            chenhLech < lonHon,
            $"Thời gian phản hồi lệch quá nhiều: có người {coNguoi}ms, không có người {khongCoNguoi}ms");
    }

    [Fact(DisplayName = "Đăng nhập KHÔNG để lại TenantScope nào (AD-4)")]
    public async Task DangNhap_KhongDeLaiPhamVi()
    {
        var accessor = _ungDung.Services.GetRequiredService<ITenantScopeAccessor>();

        await DangNhapAsync(HaiHangGia.HangA, "quantri", HaiHangGia.MatKhauHangA);

        Assert.Null(accessor.Current);
    }

    private static System.Security.Claims.ClaimsPrincipal DocToken(string token)
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        var danhTinh = new System.Security.Claims.ClaimsIdentity(jwt.Claims, "test");

        return new System.Security.Claims.ClaimsPrincipal(danhTinh);
    }

    private static async Task<double> DoThoiGianAsync(Func<Task<KetQuaDangNhap>> thaoTac)
    {
        await thaoTac();
        var dongHo = Stopwatch.StartNew();
        for (var i = 0; i < 5; i++)
        {
            await thaoTac();
        }

        return dongHo.Elapsed.TotalMilliseconds / 5;
    }

    private async Task<KetQuaDangNhap> DangNhapAsync(string maHang, string tenDangNhap, string matKhau)
    {
        using var diScope = _ungDung.Services.CreateScope();
        var useCase = diScope.ServiceProvider.GetRequiredService<DangNhap>();

        return await useCase.ThucThiAsync(new YeuCauDangNhap(maHang, tenDangNhap, matKhau));
    }
}
