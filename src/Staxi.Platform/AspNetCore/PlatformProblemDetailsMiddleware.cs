using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Staxi.Platform.Caching;
using Staxi.Platform.Data;
using Staxi.Platform.Errors;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.AspNetCore;

public sealed class PlatformProblemDetailsMiddleware(RequestDelegate next, ILogger<PlatformProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var corrId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("n");

        using var _ = AsyncLocalCorrelationIdAccessor.Dat(corrId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[PlatformHeaders.CorrelationId] = corrId;
            return Task.CompletedTask;
        });

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await XuLyNgoaiLeAsync(context, ex, corrId).ConfigureAwait(false);
        }
    }

    private async Task XuLyNgoaiLeAsync(HttpContext context, Exception ex, string corrId)
    {
        var (maTrangThai, tieuDe) = PhanLoai(ex);

        if (maTrangThai >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(ex, "Lỗi chưa xử lý, corrId={CorrelationId}", corrId);
        }
        else
        {
            logger.LogWarning(ex, "Request bị từ chối ({MaTrangThai}), corrId={CorrelationId}", maTrangThai, corrId);
        }

        if (context.Response.HasStarted)
        {
            logger.LogCritical(
                "Phản hồi đã bắt đầu gửi nên không đặt được ProblemDetails; corrId={CorrelationId} có nguy cơ trả 200 kèm lỗi (AD-8).",
                corrId);
            return;
        }

        var problem = new ProblemDetails
        {
            Status = maTrangThai,
            Title = tieuDe,
            Type = $"https://staxi.vn/loi/{maTrangThai}",
            Instance = context.Request.Path,
        };

        problem.Extensions["corrId"] = corrId;

        context.Response.Clear();
        context.Response.StatusCode = maTrangThai;
        await context.Response
            .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json")
            .ConfigureAwait(false);
    }

    private static (int MaTrangThai, string TieuDe) PhanLoai(Exception ex) => ex switch
    {
        ScopeEscalationAttemptException => (StatusCodes.Status400BadRequest, "Yêu cầu không hợp lệ."),

        TenantKhongTonTaiException => (StatusCodes.Status403Forbidden, "Không có quyền truy cập."),

        TenantConnectionMismatchException => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống."),

        TenantScopeMissingException
            or TenantScopeConflictException
            or ThieuBoLocPhamViException
            or SaiHangTenantException
            or SharedReferenceDataChuaDangKyException => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống."),

        OperationCanceledException => (499, "Yêu cầu bị huỷ."),

        _ => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống."),
    };
}

