using Staxi.Admin.Application;
using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Staxi.Platform.Authorization;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class SoDangKyQuyenTests(AssemblyCatalog assemblies)
{
    private const string GiaoDienCatalog = "Staxi.Platform.Authorization.IPermissionCatalog";

    [Fact(DisplayName = "R17: Mọi quyền phải khai qua IPermissionCatalog, không trùng mã, không trùng mã cũ (AD-17)")]
    public void SoDangKyQuyen_KhongTrungVaDungKhuon()
    {
        var soDangKy = new SoDangKyQuyen([new QuyenDanhMuc()]);

        Assert.NotEmpty(soDangKy.TatCa);
        Assert.All(soDangKy.TatCa, quyen => Assert.Contains(".", quyen.Ma, StringComparison.Ordinal));
        Assert.All(soDangKy.TatCa, quyen => Assert.False(string.IsNullOrWhiteSpace(quyen.MoTa)));
    }

    [Fact(DisplayName = "R17: Hai module khai cùng một quyền là lỗi khởi động (AD-17)")]
    public void HaiModuleKhaiTrungQuyen_ThiNem()
    {
        var loi = Assert.Throws<InvalidOperationException>(
            () => new SoDangKyQuyen([new QuyenDanhMuc(), new CatalogTrungLap()]));

        Assert.Contains("DanhMucLoaiXe.Xem", loi.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "R17: Một mã quyền cũ chỉ được thuộc về một quyền mới (AD-17)")]
    public void MotMaCuThuocHaiQuyenMoi_ThiNem()
        => Assert.Throws<InvalidOperationException>(() => new SoDangKyQuyen([new CatalogTrungMaCu()]));

    [Fact(DisplayName = "R17: Mã quyền sai khuôn {TàiNguyên}.{HànhĐộng} bị từ chối (AD-17)")]
    public void MaQuyenSaiKhuon_BiTuChoi()
    {
        Assert.Throws<ArgumentException>(() => Quyen.Khai("khongcodau", "mô tả"));
        Assert.Throws<ArgumentException>(() => Quyen.Khai("co.hai.dau.cham", "mô tả"));
        Assert.Throws<ArgumentException>(() => Quyen.Khai("thuong.Xem", "mô tả"));
    }

    [Fact(DisplayName = "R18: Mọi IPermissionCatalog phải công khai và dựng được không tham số (AD-17)")]
    public void MoiCatalog_PhaiCongKhaiVaDungDuoc()
    {
        var cacCatalog = assemblies.SanPham
            .SelectMany(TypeScanner.Read)
            .Where(type => type.Interfaces.Any(giaoDien => giaoDien.InterfaceType.FullName == GiaoDienCatalog))
            .ToArray();

        var violations = cacCatalog
            .Where(type => !type.IsPublic || !type.Methods.Any(method => method.IsConstructor && method.Parameters.Count == 0))
            .Select(type => $"{type.FullName} phải công khai và có constructor không tham số để đăng ký được vào DI");

        ArchitectureAssert.NoViolations("R18", "AD-17", "Mỗi module khai quyền của mình qua IPermissionCatalog.", violations);
        Assert.NotEmpty(cacCatalog);
    }

    private sealed class CatalogTrungLap : IPermissionCatalog
    {
        public string TenModule => "Module.Khac";

        public IReadOnlyList<Quyen> CacQuyen => [Quyen.Khai("DanhMucLoaiXe.Xem", "khai trùng")];
    }

    private sealed class CatalogTrungMaCu : IPermissionCatalog
    {
        public string TenModule => "Module.TrungMaCu";

        public IReadOnlyList<Quyen> CacQuyen =>
        [
            Quyen.Khai("MotThu.Xem", "quyền thứ nhất", 999),
            Quyen.Khai("ThuKhac.Xem", "quyền thứ hai", 999),
        ];
    }
}
