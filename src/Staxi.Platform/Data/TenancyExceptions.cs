using Microsoft.Extensions.Logging;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Data;

public sealed class ThieuBoLocPhamViException(TenancyClass hang, string sql) : InvalidOperationException(
    $"Truy vấn bảng hạng {hang} nhưng SQL không có tham số @CompanyId nên không lọc theo công ty (AD-5). SQL: {sql}")
{
    public TenancyClass Hang { get; } = hang;
}

public sealed class SaiHangTenantException(TenancyClass hang, string sql) : InvalidOperationException(
    $"Bảng hạng {hang} không chia theo công ty — database đã là biên, lọc thêm @CompanyId là sai hạng (AD-5). SQL: {sql}")
{
    public TenancyClass Hang { get; } = hang;
}

public sealed record AcrossTenantAccess(TenantScope Scope, string Repository, string LyDo);

public interface IAcrossTenantAuditSink
{
    public Task GhiVetAsync(AcrossTenantAccess truyCap, CancellationToken ct = default);
}

public sealed class LoggingAcrossTenantAuditSink(ILogger<LoggingAcrossTenantAuditSink> logger) : IAcrossTenantAuditSink
{
    public Task GhiVetAsync(AcrossTenantAccess truyCap, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(truyCap);

        logger.LogWarning(
            "VẾT vượt phạm vi: scope={Scope} repository={Repository} lyDo={LyDo}",
            truyCap.Scope.Prefix,
            truyCap.Repository,
            truyCap.LyDo);

        return Task.CompletedTask;
    }
}
