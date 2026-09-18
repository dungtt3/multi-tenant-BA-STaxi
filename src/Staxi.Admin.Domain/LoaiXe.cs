using Staxi.Platform.Data;

namespace Staxi.Admin.Domain;

public sealed class LoaiXe : ITenantOwned
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Ten { get; set; } = string.Empty;

    public string? TenTiengAnh { get; set; }

    public int SoCho { get; set; }

    public int TaiTrong { get; set; }

    public bool? DaXoa { get; set; }
}
