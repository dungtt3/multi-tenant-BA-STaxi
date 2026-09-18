using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace Staxi.Platform.Data;

public interface IAuthenticationConnectionFactory
{
    public Task<DbConnection> OpenForAuthenticationAsync(string tenantCode, string lyDo, CancellationToken ct = default);
}

public sealed class AuthenticationConnectionFactory(
    ITenantRegistry registry,
    IDbConnectionSource connectionSource,
    IConnectionScopeVerifier verifier,
    ILogger<AuthenticationConnectionFactory> logger) : IAuthenticationConnectionFactory
{
    public async Task<DbConnection> OpenForAuthenticationAsync(string tenantCode, string lyDo, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);

        if (string.IsNullOrWhiteSpace(lyDo))
        {
            throw new ArgumentException(
                "Mở kết nối ngoài phạm vi phải khai lý do. Đường này chỉ dành cho xác thực (AD-3).",
                nameof(lyDo));
        }

        var entry = await registry.GetAsync(tenantCode, ct).ConfigureAwait(false);
        var connection = connectionSource.Create(entry);

        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            await verifier.VerifyAsync(connection, entry, $"xác thực:{entry.TenantCode}", ct).ConfigureAwait(false);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        logger.LogInformation(
            "Mở kết nối xác thực cho hãng {TenantCode}: {LyDo}",
            entry.TenantCode,
            lyDo);

        return connection;
    }
}
