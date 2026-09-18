using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Staxi.Platform.Authorization;
using Staxi.Platform.Errors;

namespace Staxi.Platform.AspNetCore;

public sealed class PermissionEndpointMiddleware(RequestDelegate next, IPermissionSetAccessor permissionSetAccessor)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var yeuCau = context.GetEndpoint()?.Metadata.GetMetadata<QuyenBatBuoc>();

        if (yeuCau is not null && !permissionSetAccessor.Current.Co(yeuCau.Quyen))
        {
            await TuChoiAsync(context, yeuCau.Quyen).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static async Task TuChoiAsync(HttpContext context, Quyen quyen)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Không có quyền thực hiện thao tác này.",
            Type = "https://staxi.vn/loi/403",
            Instance = context.Request.Path,
        };

        problem.Extensions["quyenCanCo"] = quyen.Ma;
        problem.Extensions["corrId"] = context.Response.Headers[PlatformHeaders.CorrelationId].ToString();

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response
            .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json")
            .ConfigureAwait(false);
    }
}
