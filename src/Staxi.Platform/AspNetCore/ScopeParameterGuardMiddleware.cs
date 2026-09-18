using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Staxi.Platform.Errors;

namespace Staxi.Platform.AspNetCore;

public sealed class ScopeParameterGuardOptions
{
    public HashSet<string> TenBiCam { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "tenant",
        "tenantcode",
        "tenantid",
        "company",
        "companyid",
        "companycode",
        "mahang",
        "hangtaxi",
        "congty",
        "xncode",
        "dbname",
        "database",
        "connectionstring",
    };

    public List<string> TienToHeaderBiCam { get; } = ["x-tenant", "x-company", "x-staxi-tenant"];
}

public sealed class ScopeParameterGuardMiddleware(
    RequestDelegate next,
    ScopeParameterGuardOptions options,
    ILogger<ScopeParameterGuardMiddleware> logger)
{
    public Task InvokeAsync(HttpContext context)
    {
        foreach (var header in context.Request.Headers)
        {
            if (options.TienToHeaderBiCam.Exists(tienTo => header.Key.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogWarning("Chặn header phạm vi {Header} từ {RemoteIp} (AD-3, AD-4).", header.Key, context.Connection.RemoteIpAddress);
                throw new ScopeEscalationAttemptException("header", header.Key);
            }
        }

        foreach (var thamSo in context.Request.Query)
        {
            KiemTra("query", thamSo.Key);
        }

        if (context.Request.RouteValues.Count > 0)
        {
            foreach (var giaTri in context.Request.RouteValues)
            {
                KiemTra("route", giaTri.Key);
            }
        }

        if (context.Request.HasFormContentType)
        {
            foreach (var truong in context.Request.Form)
            {
                KiemTra("form", truong.Key);
            }
        }

        return next(context);

        void KiemTra(string nguon, string ten)
        {
            if (options.TenBiCam.Contains(ChuanHoa(ten)))
            {
                logger.LogWarning("Chặn tham số phạm vi {Ten} trong {Nguon} (AD-4).", ten, nguon);
                throw new ScopeEscalationAttemptException(nguon, ten);
            }
        }
    }

    private static string ChuanHoa(string ten) => ten.Replace("-", string.Empty, StringComparison.Ordinal)
        .Replace("_", string.Empty, StringComparison.Ordinal);
}
