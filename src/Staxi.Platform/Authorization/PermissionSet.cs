using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Authorization;

public sealed class PermissionSet
{
    private readonly HashSet<string> _ma;

    private PermissionSet(HashSet<string> ma, int permissionVersion)
    {
        _ma = ma;
        PermissionVersion = permissionVersion;
    }

    public static PermissionSet Rong { get; } = new([], 0);

    public int PermissionVersion { get; }

    public int SoQuyen => _ma.Count;

    public static PermissionSet Tu(IEnumerable<Quyen> cacQuyen, int permissionVersion)
    {
        ArgumentNullException.ThrowIfNull(cacQuyen);

        return new PermissionSet([.. cacQuyen.Select(quyen => quyen.Ma)], permissionVersion);
    }

    public bool Co(Quyen quyen)
    {
        ArgumentNullException.ThrowIfNull(quyen);

        return _ma.Contains(quyen.Ma);
    }
}

public interface IPermissionSetAccessor
{
    public PermissionSet Current { get; }
}

public interface IPermissionSetBinder
{
    public IDisposable Bind(PermissionSet permissionSet);
}

public sealed class AsyncLocalPermissionSetAccessor : IPermissionSetAccessor, IPermissionSetBinder
{
    private static readonly AsyncLocal<PermissionSet?> HienTai = new();

    public PermissionSet Current => HienTai.Value ?? PermissionSet.Rong;

    public IDisposable Bind(PermissionSet permissionSet)
    {
        ArgumentNullException.ThrowIfNull(permissionSet);

        var truoc = HienTai.Value;
        HienTai.Value = permissionSet;

        return new KhoiPhuc(truoc);
    }

    private sealed class KhoiPhuc(PermissionSet? truoc) : IDisposable
    {
        private bool _daBo;

        public void Dispose()
        {
            if (_daBo)
            {
                return;
            }

            _daBo = true;
            HienTai.Value = truoc;
        }
    }
}

public interface IPermissionSource
{
    public Task<PermissionSet> LayAsync(TenantScope scope, Guid nguoiDungId, CancellationToken ct = default);
}
