using Microsoft.AspNetCore.Http;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.AspNetCore;

public sealed class HttpContextTenantScopeMiddleware(RequestDelegate next, ITenantScopeBinder binder)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var scope = TenantScopeClaims.Read(context.User);

        if (scope is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        using var _ = binder.Bind(scope);
        await next(context).ConfigureAwait(false);
    }
}
