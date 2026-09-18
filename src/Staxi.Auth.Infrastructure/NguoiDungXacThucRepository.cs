using Dapper;
using Staxi.Auth.Application;
using Staxi.Auth.Domain;
using Staxi.Platform.Data;

namespace Staxi.Auth.Infrastructure;

public sealed class NguoiDungXacThucRepository(IAuthenticationConnectionFactory connectionFactory) : INguoiDungXacThucRepository
{
    private const string CauTruyVan = """
        SELECT AdminId AS Id, UserName AS TenDangNhap, Password AS MatKhauBam,
               FK_CompanyID AS CongTySoHuu, IsLock AS BiKhoa, IsDeleted AS DaXoa
        FROM [Admin.Users]
        WHERE UserName = @TenDangNhap
        """;

    public async Task<NguoiDung?> TimTheoTenDangNhapAsync(string tenantCode, string tenDangNhap, CancellationToken ct = default)
    {
        await using var connection = await connectionFactory
            .OpenForAuthenticationAsync(tenantCode, "tra người dùng để xác thực đăng nhập", ct)
            .ConfigureAwait(false);

        return await connection
            .QueryFirstOrDefaultAsync<NguoiDung>(new CommandDefinition(CauTruyVan, new { TenDangNhap = tenDangNhap }, cancellationToken: ct))
            .ConfigureAwait(false);
    }
}
