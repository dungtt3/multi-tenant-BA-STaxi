using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Caching;

public sealed class CacheKey : IEquatable<CacheKey>
{
    private static readonly SearchValues<char> KyTuCam = SearchValues.Create([':', '#', ' ', '\t', '\r', '\n']);

    private CacheKey(string giaTri, TenantScope? scope, SharedReferenceItem? mucDungChung)
    {
        GiaTri = giaTri;
        Scope = scope;
        MucDungChung = mucDungChung;
    }

    public string GiaTri { get; }

    public TenantScope? Scope { get; }

    public SharedReferenceItem? MucDungChung { get; }

    public bool CoPhamVi => Scope is not null;

    public static CacheKey For(TenantScope scope, string vung, string khoa)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new CacheKey($"{scope.Prefix}:{KiemTraDoan(vung, nameof(vung))}:{KiemTraDoan(khoa, nameof(khoa))}", scope, mucDungChung: null);
    }

    public static CacheKey ForPermissionScoped(TenantScope scope, string vung, string khoa, int permissionVersion)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentOutOfRangeException.ThrowIfNegative(permissionVersion);

        return new CacheKey(
            $"{scope.Prefix}:{KiemTraDoan(vung, nameof(vung))}:{KiemTraDoan(khoa, nameof(khoa))}#p{permissionVersion}",
            scope,
            mucDungChung: null);
    }

    public static CacheKey Shared(SharedReferenceItem muc, string khoa)
    {
        ArgumentNullException.ThrowIfNull(muc);

        var daDangKy = SharedReferenceData.Require(muc.Ten);

        return new CacheKey($"shared:{daDangKy.Ten}:{KiemTraDoan(khoa, nameof(khoa))}", scope: null, mucDungChung: daDangKy);
    }

    public override string ToString() => GiaTri;

    public bool Equals([NotNullWhen(true)] CacheKey? other)
        => other is not null && string.Equals(GiaTri, other.GiaTri, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as CacheKey);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(GiaTri);

    private static string KiemTraDoan(string doan, string tenThamSo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(doan, tenThamSo);

        if (doan.AsSpan().IndexOfAny(KyTuCam) >= 0)
        {
            throw new ArgumentException(
                $"'{doan}' chứa ký tự phân đoạn (':', '#', khoảng trắng) nên có thể giả được khoá của hãng khác.",
                tenThamSo);
        }

        return doan;
    }
}
