using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Staxi.Platform.Tenancy;

public sealed class TenantScope : IEquatable<TenantScope>
{
    private static readonly Regex MauMaHang = new("^[a-z0-9][a-z0-9_-]{1,31}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private TenantScope(string tenantCode, int companyId, TenantScopeSource source, string? lyDo)
    {
        TenantCode = tenantCode;
        CompanyId = companyId;
        Source = source;
        LyDo = lyDo;
    }

    public string TenantCode { get; }

    public int CompanyId { get; }

    public TenantScopeSource Source { get; }

    public string? LyDo { get; }

    public static TenantScope FromSignedToken(string tenantCode, int companyId)
        => new(ChuanHoaMaHang(tenantCode), KiemTraCompanyId(companyId), TenantScopeSource.SignedToken, lyDo: null);

    public static TenantScope FromBackgroundJob(string tenantCode, int companyId, string lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo))
        {
            throw new ArgumentException("Công việc chạy nền phải khai báo lý do tự đặt TenantScope (AD-15).", nameof(lyDo));
        }

        return new TenantScope(ChuanHoaMaHang(tenantCode), KiemTraCompanyId(companyId), TenantScopeSource.BackgroundJob, lyDo.Trim());
    }

    public string Prefix => $"{TenantCode}:{CompanyId}";

    public override string ToString() => Prefix;

    public bool Equals([NotNullWhen(true)] TenantScope? other)
        => other is not null
           && string.Equals(TenantCode, other.TenantCode, StringComparison.Ordinal)
           && CompanyId == other.CompanyId;

    public override bool Equals(object? obj) => Equals(obj as TenantScope);

    public override int GetHashCode() => HashCode.Combine(TenantCode, CompanyId);

    public static bool operator ==(TenantScope? trai, TenantScope? phai)
        => trai is null ? phai is null : trai.Equals(phai);

    public static bool operator !=(TenantScope? trai, TenantScope? phai) => !(trai == phai);

    private static string ChuanHoaMaHang(string tenantCode)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            throw new ArgumentException("Mã hãng rỗng.", nameof(tenantCode));
        }

        var chuanHoa = tenantCode.Trim().ToLowerInvariant();

        if (!MauMaHang.IsMatch(chuanHoa))
        {
            throw new ArgumentException(
                $"Mã hãng '{tenantCode}' không hợp lệ: chỉ cho phép chữ thường, số, '-' và '_', dài 2..32 ký tự.",
                nameof(tenantCode));
        }

        return chuanHoa;
    }

    private static int KiemTraCompanyId(int companyId)
        => companyId > 0
            ? companyId
            : throw new ArgumentOutOfRangeException(
                nameof(companyId),
                companyId,
                "companyId phải > 0. Token tạm thời (chưa chọn công ty) không dựng ra TenantScope — gọi select-company trước (AD-3).");
}
