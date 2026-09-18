using Microsoft.Extensions.DependencyInjection;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViDatabaseTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Không có TenantScope thì factory ném, không có database mặc định (AD-2)")]
    public async Task KhongCoPhamVi_ThiNem()
    {
        using var scope = _ungDung.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        await Assert.ThrowsAsync<TenantScopeMissingException>(() => factory.OpenAsync());
    }

    [Fact(DisplayName = "Mỗi hãng mở đúng database của mình, kiểm tại kết nối vật lý (AD-2, AD-18)")]
    public async Task MoiHang_MoDungDatabaseCuaMinh()
    {
        Assert.Equal("hang_a.db", await TenDatabaseDangMoAsync(HaiHangGia.ScopeA));
        Assert.Equal("hang_b.db", await TenDatabaseDangMoAsync(HaiHangGia.ScopeB));
    }

    [Fact(DisplayName = "Token hãng A không đọc được một dòng nào của hãng B (AD-2)")]
    public async Task HangA_KhongDocDuocDuLieuHangB()
    {
        var cuaA = await TrongPhamViAsync(HaiHangGia.ScopeA, repo => repo.DanhSachAsync());
        var cuaB = await TrongPhamViAsync(HaiHangGia.ScopeB, repo => repo.DanhSachAsync());

        Assert.All(cuaA, x => Assert.DoesNotContain("HANG-B", x.Ten, StringComparison.Ordinal));
        Assert.All(cuaB, x => Assert.DoesNotContain("HANG-A", x.Ten, StringComparison.Ordinal));
        Assert.NotEmpty(cuaA);
        Assert.NotEmpty(cuaB);
    }

    [Fact(DisplayName = "CompanyId do người gọi tự truyền KHÔNG thắng được phạm vi (AD-4)")]
    public async Task CompanyIdTuTruyen_KhongThangPhamVi()
    {
        var ketQua = await TrongPhamViAsync(HaiHangGia.ScopeA, repo => repo.DanhSachVoiCompanyIdTuChonAsync(2));

        Assert.All(ketQua, x => Assert.Equal(1, x.CompanyId));
    }

    [Fact(DisplayName = "Quên lọc công ty trên bảng PerCompany thì ném, không lặng lẽ trả cả hãng (AD-5)")]
    public async Task QuenLocCongTy_ThiNem()
    {
        await Assert.ThrowsAsync<ThieuBoLocPhamViException>(
            () => TrongPhamViAsync(HaiHangGia.ScopeA, repo => repo.DanhSachQuenLocAsync()));
    }

    [Fact(DisplayName = "Vượt phạm vi phải khai lý do và để lại vết (AD-5, AD-18)")]
    public async Task VuotPhamVi_PhaiKhaiLyDo()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => TrongPhamViAsync(HaiHangGia.ScopeA, repo => repo.DanhSachCaHangAsync("   ")));

        var ketQua = await TrongPhamViAsync(HaiHangGia.ScopeA, repo => repo.DanhSachCaHangAsync("đối soát doanh thu toàn hãng"));

        Assert.Contains(ketQua, x => x.CompanyId == 2);
        Assert.All(ketQua, x => Assert.DoesNotContain("HANG-B", x.Ten, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Sổ đăng ký trỏ sai database thì kết nối bị chặn tại chỗ (AD-18)")]
    public async Task SoDangKyTroSaiDatabase_ThiChan()
    {
        var store = new InMemoryTenantRegistryStore(
        [
            new TenantRegistryEntry(HaiHangGia.HangA, _ungDung.HaiHang.DuongDanKetNoi("hang_b.db"), "hang_a.db"),
        ]);

        await using var duoi = new DichVuToiThieu(store);

        var loi = await Assert.ThrowsAsync<TenantConnectionMismatchException>(
            () => duoi.MoKetNoiAsync(HaiHangGia.ScopeA));

        Assert.Equal("hang_a.db", loi.DatabaseMongDoi);
        Assert.Equal("hang_b.db", loi.DatabaseThucTe);
    }

    private async Task<string> TenDatabaseDangMoAsync(TenantScope scope)
    {
        using var diScope = _ungDung.Services.CreateScope();
        var binder = _ungDung.Services.GetRequiredService<ITenantScopeBinder>();
        var factory = diScope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        using var _ = binder.Bind(scope);
        await using var connection = await factory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT file FROM pragma_database_list WHERE name = 'main';";

        return Path.GetFileName((await command.ExecuteScalarAsync()) as string ?? string.Empty);
    }

    private async Task<T> TrongPhamViAsync<T>(TenantScope scope, Func<LoaiXeRepository, Task<T>> thaoTac)
    {
        using var diScope = _ungDung.Services.CreateScope();
        var binder = _ungDung.Services.GetRequiredService<ITenantScopeBinder>();
        var repo = diScope.ServiceProvider.GetRequiredService<LoaiXeRepository>();

        using var _ = binder.Bind(scope);

        return await thaoTac(repo);
    }
}
