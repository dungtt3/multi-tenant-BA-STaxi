namespace Staxi.Platform.Tenancy;

public sealed class AsyncLocalTenantScopeAccessor : ITenantScopeAccessor, ITenantScopeBinder
{
    private static readonly AsyncLocal<TenantScope?> HienTai = new();

    public TenantScope? Current => HienTai.Value;

    public TenantScope Required(string thaoTac)
        => HienTai.Value ?? throw new TenantScopeMissingException(thaoTac);

    public IDisposable Bind(TenantScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var truoc = HienTai.Value;

        if (truoc is not null && truoc != scope)
        {
            throw new TenantScopeConflictException(truoc, scope);
        }

        HienTai.Value = scope;
        return new KhoiPhuc(truoc);
    }

    public IDisposable Detach()
    {
        var truoc = HienTai.Value;
        HienTai.Value = null;
        return new KhoiPhuc(truoc);
    }

    private sealed class KhoiPhuc(TenantScope? truoc) : IDisposable
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
