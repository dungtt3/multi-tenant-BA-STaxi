using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Staxi.Platform.Authorization;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;

namespace Staxi.Auth.Infrastructure;

public sealed class QuyenTuDatabase(IAuthenticationConnectionFactory connectionFactory, SoDangKyQuyen soDangKy) : IPermissionSource
{
    private const string CauTruyVan = """
        SELECT up.PermissionValue
        FROM [Admin.UserPermissions] up
        WHERE up.FK_AdminId = @AdminId
        UNION ALL
        SELECT rp.PermissionValue
        FROM [Admin.RolePermissions] rp
        JOIN [Admin.UserRoles] ur ON ur.FK_RoleID = rp.FK_RoleID
        WHERE ur.FK_AdminId = @AdminId
        """;

    public async Task<PermissionSet> LayAsync(TenantScope scope, Guid nguoiDungId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        await using var connection = await connectionFactory
            .OpenForAuthenticationAsync(scope.TenantCode, "nạp quyền hiệu lực của người dùng", ct)
            .ConfigureAwait(false);

        var cacChuoi = await connection
            .QueryAsync<string?>(new CommandDefinition(CauTruyVan, new { AdminId = nguoiDungId }, cancellationToken: ct))
            .ConfigureAwait(false);

        var maCu = TachMaCu(cacChuoi);
        var quyen = maCu
            .Select(ma => soDangKy.TheoMaCu.TryGetValue(ma, out var q) ? q : null)
            .OfType<Quyen>()
            .Distinct();

        return PermissionSet.Tu(quyen, TinhPermissionVersion(maCu));
    }

    private static SortedSet<int> TachMaCu(IEnumerable<string?> cacChuoi)
    {
        var ketQua = new SortedSet<int>();

        foreach (var chuoi in cacChuoi)
        {
            if (string.IsNullOrWhiteSpace(chuoi))
            {
                continue;
            }

            foreach (var phan in chuoi.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(phan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ma))
                {
                    ketQua.Add(ma);
                }
            }
        }

        return ketQua;
    }

    private static int TinhPermissionVersion(SortedSet<int> maCu)
    {
        if (maCu.Count == 0)
        {
            return 0;
        }

        var nguyenLieu = string.Join(',', maCu);
        var bam = SHA256.HashData(Encoding.UTF8.GetBytes(nguyenLieu));

        return BitConverter.ToInt32(bam, 0) & 0x7FFFFFFF;
    }
}
