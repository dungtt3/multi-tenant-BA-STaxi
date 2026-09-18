using Microsoft.Extensions.DependencyInjection;
using Staxi.Admin.Application;
using Staxi.Contracts;
using Staxi.Platform.Tenancy;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViLoaiXeTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Repository thật: hãng A công ty 1 chỉ thấy loại xe của chính mình (AD-2, AD-5)")]
    public async Task HangACongTy1_ChiThayLoaiXeCuaChinhMinh()
    {
        var ketQua = await LayDanhSachAsync(HaiHangGia.HangA, 1);

        Assert.NotEmpty(ketQua);
        Assert.All(ketQua, x => Assert.Contains("HANG-A", x.Ten, StringComparison.Ordinal));
        Assert.All(ketQua, x => Assert.DoesNotContain("HANG-B", x.Ten, StringComparison.Ordinal));
        Assert.All(ketQua, x => Assert.DoesNotContain("C2", x.Ten, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Dòng CompanyId NULL không hiện cho công ty nào (phán quyết AD-5 ngày 18-09-2026)")]
    public async Task DongKhongCoCongTy_KhongHienChoAi()
    {
        var congTy1 = await LayDanhSachAsync(HaiHangGia.HangA, 1);
        var congTy2 = await LayDanhSachAsync(HaiHangGia.HangA, 2);

        Assert.All(congTy1, x => Assert.DoesNotContain("khong cong ty", x.Ten, StringComparison.OrdinalIgnoreCase));
        Assert.All(congTy2, x => Assert.DoesNotContain("khong cong ty", x.Ten, StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Dòng đã xoá mềm không hiện")]
    public async Task DongDaXoa_KhongHien()
    {
        var ketQua = await LayDanhSachAsync(HaiHangGia.HangA, 1);

        Assert.All(ketQua, x => Assert.DoesNotContain("da xoa", x.Ten, StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Hai hãng cùng companyId = 1 vẫn không thấy dữ liệu của nhau (AD-2)")]
    public async Task HaiHangCungCongTy_VanKhongThayCuaNhau()
    {
        var cuaA = await LayDanhSachAsync(HaiHangGia.HangA, 1);
        var cuaB = await LayDanhSachAsync(HaiHangGia.HangB, 1);

        Assert.NotEmpty(cuaA);
        Assert.NotEmpty(cuaB);
        Assert.All(cuaA, x => Assert.DoesNotContain("HANG-B", x.Ten, StringComparison.Ordinal));
        Assert.All(cuaB, x => Assert.DoesNotContain("HANG-A", x.Ten, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Không có TenantScope thì use case ném, không trả danh sách rỗng (AD-2)")]
    public async Task KhongCoPhamVi_ThiNem()
    {
        using var diScope = _ungDung.Services.CreateScope();
        var useCase = diScope.ServiceProvider.GetRequiredService<LayDanhSachLoaiXe>();

        await Assert.ThrowsAsync<TenantScopeMissingException>(() => useCase.ThucThiAsync());
    }

    private async Task<IReadOnlyList<LoaiXeDto>> LayDanhSachAsync(string tenantCode, int companyId)
    {
        using var diScope = _ungDung.Services.CreateScope();
        var binder = _ungDung.Services.GetRequiredService<ITenantScopeBinder>();
        var useCase = diScope.ServiceProvider.GetRequiredService<LayDanhSachLoaiXe>();

        using var _ = binder.Bind(TenantScope.FromSignedToken(tenantCode, companyId));

        return await useCase.ThucThiAsync();
    }
}
