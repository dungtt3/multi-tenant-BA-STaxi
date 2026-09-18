using Microsoft.Extensions.Logging;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;

namespace Staxi.TenantLeakTests.Fakes;

public sealed class LoaiXe : ITenantOwned
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string Ten { get; set; } = string.Empty;
}

public sealed class LoaiXeRepository(
    ITenantConnectionFactory connectionFactory,
    ITenantScopeAccessor scopeAccessor,
    IAcrossTenantAuditSink auditSink,
    ILogger<LoaiXeRepository> logger)
    : TenantScopedRepository(connectionFactory, scopeAccessor, auditSink, logger)
{
    public Task<IReadOnlyList<LoaiXe>> DanhSachAsync(CancellationToken ct = default)
        => QueryPerCompanyAsync<LoaiXe>(
            "SELECT Id, CompanyId, Ten FROM LoaiXe WHERE CompanyId = @CompanyId ORDER BY Id",
            ct: ct);

    public Task<IReadOnlyList<LoaiXe>> DanhSachQuenLocAsync(CancellationToken ct = default)
        => QueryPerCompanyAsync<LoaiXe>("SELECT Id, CompanyId, Ten FROM LoaiXe ORDER BY Id", ct: ct);

    public Task<IReadOnlyList<LoaiXe>> DanhSachVoiCompanyIdTuChonAsync(int companyIdTuChon, CancellationToken ct = default)
        => QueryPerCompanyAsync<LoaiXe>(
            "SELECT Id, CompanyId, Ten FROM LoaiXe WHERE CompanyId = @CompanyId ORDER BY Id",
            new { CompanyId = companyIdTuChon },
            ct);

    public Task<IReadOnlyList<LoaiXe>> DanhSachCaHangAsync(string lyDo, CancellationToken ct = default)
        => AcrossTenants<LoaiXe>(lyDo, "SELECT Id, CompanyId, Ten FROM LoaiXe ORDER BY Id", ct: ct);
}
