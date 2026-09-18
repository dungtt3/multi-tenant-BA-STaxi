using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Staxi.Platform.Caching;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViCacheTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Nạp cache ở hãng A, đọc ở hãng B là miss (AD-6)")]
    public async Task NapOHangA_DocOHangB_LaMiss()
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);
        using var clientB = _ungDung.TaoClient(HaiHangGia.HangB, 1);

        var ghi = await clientA.PutAsJsonAsync("/api/cache/loai-xe", new GhiCache("danh muc cua HANG-A"));
        ghi.EnsureSuccessStatusCode();

        var docA = await clientA.GetAsync(new Uri("/api/cache/loai-xe", UriKind.Relative));
        var docB = await clientB.GetAsync(new Uri("/api/cache/loai-xe", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, docA.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, docB.StatusCode);
        Assert.DoesNotContain("HANG-A", await docB.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Cùng vùng, cùng khoá, khác hãng ⇒ khác key (AD-6)")]
    public void CungKhoaKhacHang_ThiKhacKey()
    {
        var cuaA = CacheKey.For(HaiHangGia.ScopeA, "danh-muc", "loai-xe");
        var cuaB = CacheKey.For(HaiHangGia.ScopeB, "danh-muc", "loai-xe");

        Assert.NotEqual(cuaA, cuaB);
        Assert.Equal("hang-a:1:danh-muc:loai-xe", cuaA.GiaTri);
        Assert.Equal("hang-b:1:danh-muc:loai-xe", cuaB.GiaTri);
    }

    [Fact(DisplayName = "Hai công ty trong cùng một hãng cũng khác key (AD-5, AD-6)")]
    public void HaiCongTyTrongMotHang_CungKhacKey()
    {
        var congTy1 = CacheKey.For(Staxi.Platform.Tenancy.TenantScope.FromSignedToken(HaiHangGia.HangA, 1), "danh-muc", "loai-xe");
        var congTy2 = CacheKey.For(Staxi.Platform.Tenancy.TenantScope.FromSignedToken(HaiHangGia.HangA, 2), "danh-muc", "loai-xe");

        Assert.NotEqual(congTy1, congTy2);
    }

    [Fact(DisplayName = "Key dùng chung ngoài sổ SharedReferenceData thì không dựng được (AD-6)")]
    public void KeyDungChungNgoaiSo_ThiNem()
    {
        Assert.Throws<SharedReferenceDataChuaDangKyException>(() => SharedReferenceData.Require("tinh-thanh"));

        Assert.Empty(SharedReferenceData.Catalog);
    }

    [Fact(DisplayName = "Khoá chứa ký tự phân đoạn bị từ chối, không giả được key hãng khác (AD-6)")]
    public void KhoaChuaKyTuPhanDoan_ThiTuChoi()
    {
        Assert.Throws<ArgumentException>(() => CacheKey.For(HaiHangGia.ScopeA, "danh-muc", "loai-xe:hang-b:1"));
        Assert.Throws<ArgumentException>(() => CacheKey.For(HaiHangGia.ScopeA, "danh-muc:hang-b", "loai-xe"));
    }

    [Fact(DisplayName = "permissionVersion đổi thì key đổi, cache không phục vụ nội dung của quyền cũ (AD-6, AD-17)")]
    public void DoiPermissionVersion_ThiDoiKey()
    {
        var quyenCu = CacheKey.ForPermissionScoped(HaiHangGia.ScopeA, "man-hinh", "menu", 7);
        var quyenMoi = CacheKey.ForPermissionScoped(HaiHangGia.ScopeA, "man-hinh", "menu", 8);

        Assert.NotEqual(quyenCu, quyenMoi);
    }

    [Fact(DisplayName = "Cache dùng chung một kho, nên cách ly đến từ key chứ không từ bộ nhớ (AD-6)")]
    public async Task HaiHangDungChungMotKhoCache()
    {
        var cache = _ungDung.Services.GetRequiredService<ITenantCache>();

        await cache.SetAsync(CacheKey.For(HaiHangGia.ScopeA, "vung", "k"), "cua-A", TimeSpan.FromMinutes(1));
        await cache.SetAsync(CacheKey.For(HaiHangGia.ScopeB, "vung", "k"), "cua-B", TimeSpan.FromMinutes(1));

        Assert.Equal("cua-A", await cache.GetAsync<string>(CacheKey.For(HaiHangGia.ScopeA, "vung", "k")));
        Assert.Equal("cua-B", await cache.GetAsync<string>(CacheKey.For(HaiHangGia.ScopeB, "vung", "k")));
    }
}
