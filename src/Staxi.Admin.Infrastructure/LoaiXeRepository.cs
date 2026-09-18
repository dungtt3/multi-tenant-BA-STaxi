using Microsoft.Extensions.Logging;
using Staxi.Admin.Application;
using Staxi.Admin.Domain;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;

namespace Staxi.Admin.Infrastructure;

public sealed class LoaiXeRepository(
    ITenantConnectionFactory connectionFactory,
    ITenantScopeAccessor scopeAccessor,
    IAcrossTenantAuditSink auditSink,
    ILogger<LoaiXeRepository> logger)
    : TenantScopedRepository(connectionFactory, scopeAccessor, auditSink, logger), ILoaiXeRepository
{
    private const string CauTruyVan = """
        SELECT PK_VehicleTypeID AS Id, CompanyId, Name AS Ten, NameEN AS TenTiengAnh,
               Seat AS SoCho, Payload AS TaiTrong, IsDeleted AS DaXoa
        FROM [Admin.CarTypes]
        WHERE CompanyId = @CompanyId AND (IsDeleted IS NULL OR IsDeleted = 0)
        ORDER BY Name
        """;

    public Task<IReadOnlyList<LoaiXe>> DanhSachAsync(CancellationToken ct = default)
        => QueryPerCompanyAsync<LoaiXe>(CauTruyVan, ct: ct);
}
