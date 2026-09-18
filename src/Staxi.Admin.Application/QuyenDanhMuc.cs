using Staxi.Platform.Authorization;

namespace Staxi.Admin.Application;

public sealed class QuyenDanhMuc : IPermissionCatalog
{
    public static Quyen XemLoaiXe { get; } = Quyen.Khai("DanhMucLoaiXe.Xem", "Xem danh mục loại xe", 301);

    public static Quyen ThemLoaiXe { get; } = Quyen.Khai("DanhMucLoaiXe.Them", "Thêm loại xe", 302);

    public static Quyen SuaLoaiXe { get; } = Quyen.Khai("DanhMucLoaiXe.Sua", "Sửa loại xe", 303);

    public static Quyen XoaLoaiXe { get; } = Quyen.Khai("DanhMucLoaiXe.Xoa", "Xoá loại xe", 304);

    public string TenModule => "Staxi.Admin";

    public IReadOnlyList<Quyen> CacQuyen => [XemLoaiXe, ThemLoaiXe, SuaLoaiXe, XoaLoaiXe];
}
