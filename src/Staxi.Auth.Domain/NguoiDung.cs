namespace Staxi.Auth.Domain;

public sealed class NguoiDung
{
    public Guid Id { get; set; }

    public string TenDangNhap { get; set; } = string.Empty;

    public string MatKhauBam { get; set; } = string.Empty;

    public int CongTySoHuu { get; set; }

    public bool BiKhoa { get; set; }

    public bool DaXoa { get; set; }

    public bool DungDuoc => !BiKhoa && !DaXoa;
}
