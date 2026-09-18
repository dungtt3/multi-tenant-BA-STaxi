using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Staxi.Platform.Data;
using Staxi.Platform.Tenancy;

namespace Staxi.TenantLeakTests.Fakes;

public sealed class HaiHangGia : IDisposable
{
    public const string HangA = "hang-a";
    public const string HangB = "hang-b";

    public const string KhoaKyTest = "khoa-ky-jwt-chi-dung-trong-test-dai-toi-thieu-32-byte";

    public const string KhoaKyGiaMao = "khoa-gia-mao-cua-ke-tan-cong-cung-dai-32-byte-tro-len";

    private readonly string _thuMuc;
    private readonly List<SqliteConnection> _giuKetNoi = [];

    public HaiHangGia()
    {
        _thuMuc = Path.Combine(Path.GetTempPath(), "staxi-leak-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_thuMuc);

        DungDatabase(HangA, "hang_a.db", "HANG-A", congTy1: "Xe 4 cho HANG-A", congTy2: "Xe 7 cho HANG-A cong ty 2");
        DungDatabase(HangB, "hang_b.db", "HANG-B", congTy1: "Xe 4 cho HANG-B", congTy2: "Xe 7 cho HANG-B cong ty 2");

        Store = new InMemoryTenantRegistryStore(
        [
            new TenantRegistryEntry(HangA, DuongDanKetNoi("hang_a.db"), "hang_a.db"),
            new TenantRegistryEntry(HangB, DuongDanKetNoi("hang_b.db"), "hang_b.db"),
        ]);
    }

    public InMemoryTenantRegistryStore Store { get; }

    public static TenantScope ScopeA { get; } = TenantScope.FromSignedToken(HangA, 1);

    public static TenantScope ScopeB { get; } = TenantScope.FromSignedToken(HangB, 1);

    public static string CapToken(string tenantCode, int companyId, string? khoaKy = null, int permissionVersion = 1)
    {
        var khoa = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(khoaKy ?? KhoaKyTest));

        var moTa = new SecurityTokenDescriptor
        {
            Issuer = "staxi-test",
            Audience = "staxi-test",
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(khoa, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [ClaimTypes.NameIdentifier] = $"user-{tenantCode}",
                [StaxiClaimTypes.TenantCode] = tenantCode,
                [StaxiClaimTypes.CompanyId] = companyId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [StaxiClaimTypes.PermissionVersion] = permissionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [StaxiClaimTypes.SecurityStamp] = "stamp-1",
            },
        };

        return new JsonWebTokenHandler().CreateToken(moTa);
    }

    public string DuongDanKetNoi(string tenFile) => $"Data Source={Path.Combine(_thuMuc, tenFile)}";

    private void DungDatabase(string tenantCode, string tenFile, string nhan, string congTy1, string congTy2)
    {
        var connection = new SqliteConnection(DuongDanKetNoi(tenFile));
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE LoaiXe (Id INTEGER PRIMARY KEY, CompanyId INTEGER NOT NULL, Ten TEXT NOT NULL);
            INSERT INTO LoaiXe (Id, CompanyId, Ten) VALUES (1, 1, $congTy1), (2, 2, $congTy2);

            CREATE TABLE [Admin.CarTypes] (
              PK_VehicleTypeID INTEGER PRIMARY KEY, CompanyId INTEGER NULL,
              Name TEXT NOT NULL, NameEN TEXT NULL, Seat INTEGER NOT NULL,
              Payload INTEGER NOT NULL, IsDeleted INTEGER NULL);
            INSERT INTO [Admin.CarTypes] (PK_VehicleTypeID, CompanyId, Name, NameEN, Seat, Payload, IsDeleted)
            VALUES (1, 1, $xeC1, 'Car C1', 4, 500, NULL),
                   (2, 2, $xeC2, NULL, 7, 900, NULL),
                   (3, NULL, $xeKhongCongTy, NULL, 5, 600, NULL),
                   (4, 1, $xeDaXoa, NULL, 4, 500, 1);
            """;
        command.Parameters.AddWithValue("$congTy1", congTy1);
        command.Parameters.AddWithValue("$congTy2", congTy2);
        command.Parameters.AddWithValue("$xeC1", $"Xe 4 cho {nhan} C1");
        command.Parameters.AddWithValue("$xeC2", $"Xe 7 cho {nhan} C2");
        command.Parameters.AddWithValue("$xeKhongCongTy", $"Xe khong cong ty {nhan}");
        command.Parameters.AddWithValue("$xeDaXoa", $"Xe da xoa {nhan} C1");
        command.ExecuteNonQuery();

        _giuKetNoi.Add(connection);

        _ = tenantCode;
    }

    public void Dispose()
    {
        foreach (var connection in _giuKetNoi)
        {
            connection.Dispose();
        }

        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_thuMuc, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
