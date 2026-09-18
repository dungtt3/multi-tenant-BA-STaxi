using System.Globalization;
using System.Security.Claims;

namespace Staxi.Platform.Tenancy;

public static class StaxiClaimTypes
{
    public const string TenantCode = "staxi_tenant";
    public const string CompanyId = "staxi_company";
    public const string PermissionVersion = "staxi_permver";
    public const string SecurityStamp = "staxi_secstamp";
}

public static class TenantScopeClaims
{
    public static TenantScope? Read(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var tenantCode = principal.FindFirstValue(StaxiClaimTypes.TenantCode);
        var companyIdTho = principal.FindFirstValue(StaxiClaimTypes.CompanyId);

        if (string.IsNullOrWhiteSpace(tenantCode) || string.IsNullOrWhiteSpace(companyIdTho))
        {
            return null;
        }

        if (!int.TryParse(companyIdTho, NumberStyles.Integer, CultureInfo.InvariantCulture, out var companyId))
        {
            return null;
        }

        return TenantScope.FromSignedToken(tenantCode, companyId);
    }

    public static int? ReadPermissionVersion(ClaimsPrincipal? principal)
    {
        var thô = principal?.FindFirstValue(StaxiClaimTypes.PermissionVersion);

        return int.TryParse(thô, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
