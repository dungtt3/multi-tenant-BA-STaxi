using Staxi.Auth.Domain;

namespace Staxi.Auth.Application;

public interface INguoiDungXacThucRepository
{
    public Task<NguoiDung?> TimTheoTenDangNhapAsync(string tenantCode, string tenDangNhap, CancellationToken ct = default);
}

public interface IKiemMatKhau
{
    public bool Khop(string matKhauNhap, string bamLuuTru);

    public string BamGia { get; }
}

public interface ITokenFactory
{
    public string Tao(string tenantCode, int companyId, Guid nguoiDungId, int permissionVersion, string securityStamp);
}

public interface ISecurityStampFactory
{
    public string Tao(NguoiDung nguoiDung);
}

public sealed record YeuCauDangNhap(string MaHang, string TenDangNhap, string MatKhau);

public sealed record KetQuaDangNhap(bool ThanhCong, string? Token)
{
    public static KetQuaDangNhap ThatBai { get; } = new(false, null);
}

public sealed class DangNhap(
    INguoiDungXacThucRepository repository,
    IKiemMatKhau kiemMatKhau,
    ITokenFactory tokenFactory,
    ISecurityStampFactory securityStampFactory)
{
    private const int PermissionVersionTamThoi = 1;

    public async Task<KetQuaDangNhap> ThucThiAsync(YeuCauDangNhap yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);

        var nguoiDung = await TimNguoiDungAsync(yeuCau, ct).ConfigureAwait(false);
        var bamDoiChieu = nguoiDung?.MatKhauBam ?? kiemMatKhau.BamGia;
        var matKhauDung = kiemMatKhau.Khop(yeuCau.MatKhau, bamDoiChieu);

        if (nguoiDung is null || !matKhauDung || !nguoiDung.DungDuoc)
        {
            return KetQuaDangNhap.ThatBai;
        }

        var token = tokenFactory.Tao(
            yeuCau.MaHang,
            nguoiDung.CongTySoHuu,
            nguoiDung.Id,
            PermissionVersionTamThoi,
            securityStampFactory.Tao(nguoiDung));

        return new KetQuaDangNhap(true, token);
    }

    private async Task<NguoiDung?> TimNguoiDungAsync(YeuCauDangNhap yeuCau, CancellationToken ct)
    {
        try
        {
            return await repository.TimTheoTenDangNhapAsync(yeuCau.MaHang, yeuCau.TenDangNhap, ct).ConfigureAwait(false);
        }
        catch (Platform.Tenancy.TenantKhongTonTaiException)
        {
            return null;
        }
    }
}
