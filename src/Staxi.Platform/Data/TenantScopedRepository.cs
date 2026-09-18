using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Logging;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Data;

public abstract class TenantScopedRepository(
    ITenantConnectionFactory connectionFactory,
    ITenantScopeAccessor scopeAccessor,
    IAcrossTenantAuditSink auditSink,
    ILogger logger)
{
    protected TenantScope Scope => scopeAccessor.Required(GetType().Name);

    protected async Task<IReadOnlyList<T>> QueryPerCompanyAsync<T>(string sql, object? thamSo = null, CancellationToken ct = default)
    {
        var scope = Scope;

        if (!ChuaThamSo(sql))
        {
            throw new ThieuBoLocPhamViException(TenancyClass.PerCompany, sql);
        }

        var thamSoDayDu = GopThamSo(thamSo, scope);

        await using var connection = await connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        var ketQua = await connection.QueryAsync<T>(new CommandDefinition(sql, thamSoDayDu, cancellationToken: ct)).ConfigureAwait(false);

        return ketQua.AsList();
    }

    protected async Task<IReadOnlyList<T>> QueryPerTenantAsync<T>(string sql, object? thamSo = null, CancellationToken ct = default)
    {
        _ = Scope;

        if (ChuaThamSo(sql))
        {
            throw new SaiHangTenantException(TenancyClass.PerTenant, sql);
        }

        await using var connection = await connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        var ketQua = await connection.QueryAsync<T>(new CommandDefinition(sql, thamSo, cancellationToken: ct)).ConfigureAwait(false);

        return ketQua.AsList();
    }

    protected async Task<IReadOnlyList<T>> AcrossTenants<T>(string lyDo, string sql, object? thamSo = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(lyDo))
        {
            throw new ArgumentException("Vượt phạm vi phải khai lý do (AD-5).", nameof(lyDo));
        }

        var scope = Scope;

        await auditSink.GhiVetAsync(new AcrossTenantAccess(scope, GetType().Name, lyDo), ct).ConfigureAwait(false);
        logger.LogWarning("Truy vấn vượt phạm vi tại {Repository} cho {Scope}: {LyDo}", GetType().Name, scope.Prefix, lyDo);

        await using var connection = await connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        var ketQua = await connection.QueryAsync<T>(new CommandDefinition(sql, thamSo, cancellationToken: ct)).ConfigureAwait(false);

        return ketQua.AsList();
    }

    protected Task<DbConnection> OpenConnectionAsync(CancellationToken ct = default) => connectionFactory.OpenAsync(ct);

    private static bool ChuaThamSo(string sql)
        => sql.Contains("@CompanyId", StringComparison.OrdinalIgnoreCase);

    private static DynamicParameters GopThamSo(object? thamSo, TenantScope scope)
    {
        var gop = new DynamicParameters();

        if (thamSo is not null)
        {
            gop.AddDynamicParams(thamSo);
        }

        gop.Add("CompanyId", scope.CompanyId);

        return gop;
    }
}
