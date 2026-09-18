using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Staxi.Platform.Authorization;

public sealed partial class Quyen : IEquatable<Quyen>
{
    private Quyen(string ma, string moTa, IReadOnlyList<int> maCu)
    {
        Ma = ma;
        MoTa = moTa;
        MaCu = maCu;
    }

    public string Ma { get; }

    public string MoTa { get; }

    public IReadOnlyList<int> MaCu { get; }

    public static Quyen Khai(string ma, string moTa, params int[] maCu)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ma);
        ArgumentException.ThrowIfNullOrWhiteSpace(moTa);

        if (!MauMaQuyen().IsMatch(ma))
        {
            throw new ArgumentException(
                $"Mã quyền '{ma}' sai khuôn. Phải là {{TàiNguyên}}.{{HànhĐộng}}, ví dụ 'DanhMucLoaiXe.Xem' (AD-17).",
                nameof(ma));
        }

        return new Quyen(ma, moTa, maCu ?? []);
    }

    public override string ToString() => Ma;

    public bool Equals([NotNullWhen(true)] Quyen? other) => other is not null && string.Equals(Ma, other.Ma, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Quyen);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Ma);

    [GeneratedRegex("^[A-Z][A-Za-z0-9]{1,49}\\.[A-Z][A-Za-z0-9]{1,29}$", RegexOptions.CultureInvariant)]
    private static partial Regex MauMaQuyen();
}

public interface IPermissionCatalog
{
    public string TenModule { get; }

    public IReadOnlyList<Quyen> CacQuyen { get; }
}

public sealed class SoDangKyQuyen
{
    private readonly Dictionary<string, Quyen> _theoMa;

    public SoDangKyQuyen(IEnumerable<IPermissionCatalog> cacCatalog)
    {
        ArgumentNullException.ThrowIfNull(cacCatalog);

        _theoMa = new Dictionary<string, Quyen>(StringComparer.Ordinal);
        var nguon = new Dictionary<string, string>(StringComparer.Ordinal);
        var maCuDaDung = new Dictionary<int, string>();

        foreach (var catalog in cacCatalog)
        {
            foreach (var quyen in catalog.CacQuyen)
            {
                if (nguon.TryGetValue(quyen.Ma, out var moduleTruoc))
                {
                    throw new InvalidOperationException(
                        $"Quyền '{quyen.Ma}' được khai ở cả '{moduleTruoc}' và '{catalog.TenModule}'. "
                        + "Hai nơi khai cùng một quyền là hai cách hiểu về cùng một quyền (AD-17).");
                }

                foreach (var ma in quyen.MaCu)
                {
                    if (maCuDaDung.TryGetValue(ma, out var quyenTruoc))
                    {
                        throw new InvalidOperationException(
                            $"Mã quyền cũ {ma} được ánh xạ cho cả '{quyenTruoc}' và '{quyen.Ma}'. "
                            + "Một mã cũ chỉ được thuộc về một quyền mới (AD-17).");
                    }

                    maCuDaDung[ma] = quyen.Ma;
                }

                nguon[quyen.Ma] = catalog.TenModule;
                _theoMa[quyen.Ma] = quyen;
            }
        }

        TheoMaCu = maCuDaDung.ToDictionary(cap => cap.Key, cap => _theoMa[cap.Value]);
    }

    public IReadOnlyDictionary<int, Quyen> TheoMaCu { get; }

    public IReadOnlyCollection<Quyen> TatCa => _theoMa.Values;

    public Quyen Require(string ma)
        => _theoMa.TryGetValue(ma, out var quyen)
            ? quyen
            : throw new InvalidOperationException($"Quyền '{ma}' chưa được khai trong sổ đăng ký quyền (AD-17).");
}
