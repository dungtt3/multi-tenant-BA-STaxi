namespace Staxi.Platform.Tenancy;

public interface ITenantScopeAccessor
{
    public TenantScope? Current { get; }

    public TenantScope Required(string thaoTac);
}

public interface ITenantScopeBinder
{
    public IDisposable Bind(TenantScope scope);

    public IDisposable Detach();
}
