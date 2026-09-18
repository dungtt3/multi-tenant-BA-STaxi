using Microsoft.Extensions.DependencyInjection;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViRoRiSangViecNenTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Gỡ phạm vi ở ranh giới việc nền thì việc nền không còn phạm vi nào (AD-15)")]
    public async Task GoPhamVi_ThiViecNenKhongConPhamVi()
    {
        var binder = _ungDung.Services.GetRequiredService<ITenantScopeBinder>();
        var accessor = _ungDung.Services.GetRequiredService<ITenantScopeAccessor>();

        using var _ = binder.Bind(HaiHangGia.ScopeA);
        Assert.Equal(HaiHangGia.ScopeA, accessor.Current);

        using (binder.Detach())
        {
            Assert.Null(accessor.Current);

            using var diScope = _ungDung.Services.CreateScope();
            var factory = diScope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

            await Assert.ThrowsAsync<TenantScopeMissingException>(() => factory.OpenAsync());
        }

        Assert.Equal(HaiHangGia.ScopeA, accessor.Current);
    }

    [Fact(DisplayName = "Việc nền phải tự đặt phạm vi kèm lý do, và lý do được giữ lại (AD-15, AD-18)")]
    public void ViecNen_PhaiKhaiLyDo()
    {
        Assert.Throws<ArgumentException>(() => TenantScope.FromBackgroundJob(HaiHangGia.HangA, 1, "  "));

        var scope = TenantScope.FromBackgroundJob(HaiHangGia.HangA, 1, "tong hop bao cao dem");

        Assert.Equal(TenantScopeSource.BackgroundJob, scope.Source);
        Assert.Equal("tong hop bao cao dem", scope.LyDo);
    }

    [Fact(DisplayName = "Không gán đè được một phạm vi khác lên request đang chạy (AD-4)")]
    public void KhongGanDeDuocPhamViKhac()
    {
        var binder = _ungDung.Services.GetRequiredService<ITenantScopeBinder>();

        using var _ = binder.Bind(HaiHangGia.ScopeA);

        var loi = Assert.Throws<TenantScopeConflictException>(() => binder.Bind(HaiHangGia.ScopeB));

        Assert.Equal(HaiHangGia.ScopeA, loi.DangCo);
        Assert.Equal(HaiHangGia.ScopeB, loi.MuonDat);
    }

    [Fact(DisplayName = "Gán lại đúng phạm vi đang có là vô hại (AD-4)")]
    public void GanLaiDungPhamVi_LaVoHai()
    {
        var binder = _ungDung.Services.GetRequiredService<ITenantScopeBinder>();
        var accessor = _ungDung.Services.GetRequiredService<ITenantScopeAccessor>();

        using var ngoai = binder.Bind(HaiHangGia.ScopeA);
        using (binder.Bind(TenantScope.FromSignedToken(HaiHangGia.HangA, 1)))
        {
            Assert.Equal(HaiHangGia.ScopeA, accessor.Current);
        }

        Assert.Equal(HaiHangGia.ScopeA, accessor.Current);
    }

    [Theory(DisplayName = "Mã hãng sai khuôn bị từ chối ngay khi dựng phạm vi (AD-6, AD-7)")]
    [InlineData("hang:b")]
    [InlineData("hang b")]
    [InlineData("a")]
    [InlineData("")]
    [InlineData("hang#b")]
    public void MaHangSaiKhuon_BiTuChoi(string maHang)
        => Assert.ThrowsAny<ArgumentException>(() => TenantScope.FromSignedToken(maHang, 1));

    [Fact(DisplayName = "Mã hãng khác hoa thường vẫn là một hãng (AD-6)")]
    public void MaHangKhacHoaThuong_VanLaMotHang()
        => Assert.Equal(TenantScope.FromSignedToken("HANG-A", 1), TenantScope.FromSignedToken("hang-a", 1));

    [Fact(DisplayName = "Token chưa chọn công ty không dựng ra phạm vi (AD-3)")]
    public void ChuaChonCongTy_ThiKhongCoPhamVi()
        => Assert.Throws<ArgumentOutOfRangeException>(() => TenantScope.FromSignedToken(HaiHangGia.HangA, 0));
}
