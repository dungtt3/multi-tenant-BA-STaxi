namespace Staxi.Platform.Data;

public enum TenancyClass
{
    Global = 1,

    PerTenant = 2,

    PerCompany = 3,

    CrossLink = 4,
}

public interface ITenantOwned
{
    public int CompanyId { get; }
}
