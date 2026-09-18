using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Staxi.Auth.Application;
using Staxi.Platform.Tenancy;
using Staxi.Platform.Time;

namespace Staxi.Auth.Infrastructure;

public sealed record CauHinhToken(string KhoaKy, string Issuer, string Audience, TimeSpan ThoiHan);

public sealed class JwtTokenFactory(CauHinhToken cauHinh, IClock clock) : ITokenFactory
{
    private readonly JsonWebTokenHandler _handler = new();

    public string Tao(string tenantCode, int companyId, Guid nguoiDungId, int permissionVersion, string securityStamp)
    {
        var khoa = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cauHinh.KhoaKy));

        var moTa = new SecurityTokenDescriptor
        {
            Issuer = cauHinh.Issuer,
            Audience = cauHinh.Audience,
            Expires = clock.UtcNow.Add(cauHinh.ThoiHan).UtcDateTime,
            SigningCredentials = new SigningCredentials(khoa, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [ClaimTypes.NameIdentifier] = nguoiDungId.ToString("n"),
                [StaxiClaimTypes.TenantCode] = tenantCode,
                [StaxiClaimTypes.CompanyId] = companyId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [StaxiClaimTypes.PermissionVersion] = permissionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [StaxiClaimTypes.SecurityStamp] = securityStamp,
            },
        };

        return _handler.CreateToken(moTa);
    }
}
