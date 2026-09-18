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

    public const string MatKhauHangA = "matkhau-hang-a";

    public const string MatKhauHangB = "matkhau-hang-b";

    public const string MatKhauBiKhoa = "matkhau-bi-khoa";

    public const string BamMatKhauHangA = "DA7EC2614771E257986E269EDDB23337";

    public const string BamMatKhauHangB = "94497C6B6867FD6A9252A40AF517AB25";

    public const string BamMatKhauBiKhoa = "9F62790D3FFEF3A5B5659BDD9B3D06E7";

    public static Guid IdQuanTri(string tenantCode)
        => tenantCode == HangA
            ? new Guid("aaaaaaaa-0000-0000-0000-000000000001")
            : new Guid("bbbbbbbb-0000-0000-0000-000000000001");

    private readonly string _thuMuc;
    private readonly List<SqliteConnection> _giuKetNoi = [];

    public HaiHangGia()
    {
        GuidChoSqlite.DangKyMotLan();

        _thuMuc = Path.Combine(Path.GetTempPath(), "staxi-leak-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_thuMuc);

        DungDatabase(HangA, "hang_a.db", "HANG-A", BamMatKhauHangA, congTy1: "Xe 4 cho HANG-A", congTy2: "Xe 7 cho HANG-A cong ty 2");
        DungDatabase(HangB, "hang_b.db", "HANG-B", BamMatKhauHangB, congTy1: "Xe 4 cho HANG-B", congTy2: "Xe 7 cho HANG-B cong ty 2");

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

    private void DungDatabase(string tenantCode, string tenFile, string nhan, string bamMatKhau, string congTy1, string congTy2)
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

            CREATE TABLE [Admin.Users] (
              AdminId TEXT PRIMARY KEY, UserName TEXT NULL, Password TEXT NULL,
              FK_CompanyID INTEGER NOT NULL, IsLock INTEGER NULL, IsDeleted INTEGER NULL);
            INSERT INTO [Admin.Users] (AdminId, UserName, Password, FK_CompanyID, IsLock, IsDeleted)
            VALUES ($idQuanTri, 'quantri', $bamQuanTri, 1, 0, 0),
                   ($idBiKhoa, 'bikhoa', $bamBiKhoa, 1, 1, 0),
                   ($idDaXoa, 'daxoa', $bamQuanTri, 1, 0, 1);

            CREATE TABLE [Admin.UserPermissions] (
              PK_UserPermissionID INTEGER PRIMARY KEY, FK_AdminId TEXT NOT NULL, PermissionValue TEXT NOT NULL);
            CREATE TABLE [Admin.UserRoles] (FK_AdminId TEXT NOT NULL, FK_RoleID INTEGER NOT NULL);
            CREATE TABLE [Admin.RolePermissions] (
              PK_RolePermissionID INTEGER PRIMARY KEY, FK_RoleID INTEGER NOT NULL, PermissionValue TEXT NOT NULL);
            INSERT INTO [Admin.UserPermissions] (PK_UserPermissionID, FK_AdminId, PermissionValue)
            VALUES (1, $idQuanTri, $quyenRieng);
            INSERT INTO [Admin.UserRoles] (FK_AdminId, FK_RoleID) VALUES ($idQuanTri, 7);
            INSERT INTO [Admin.RolePermissions] (PK_RolePermissionID, FK_RoleID, PermissionValue)
            VALUES (1, 7, $quyenVaiTro);
            """;
        command.Parameters.AddWithValue("$congTy1", congTy1);
        command.Parameters.AddWithValue("$congTy2", congTy2);
        command.Parameters.AddWithValue("$xeC1", $"Xe 4 cho {nhan} C1");
        command.Parameters.AddWithValue("$xeC2", $"Xe 7 cho {nhan} C2");
        command.Parameters.AddWithValue("$xeKhongCongTy", $"Xe khong cong ty {nhan}");
        command.Parameters.AddWithValue("$xeDaXoa", $"Xe da xoa {nhan} C1");
        command.Parameters.AddWithValue("$idQuanTri", IdQuanTri(tenantCode));
        command.Parameters.AddWithValue("$idBiKhoa", Guid.NewGuid());
        command.Parameters.AddWithValue("$idDaXoa", Guid.NewGuid());
        command.Parameters.AddWithValue("$bamQuanTri", bamMatKhau);
        command.Parameters.AddWithValue("$bamBiKhoa", BamMatKhauBiKhoa);
        command.Parameters.AddWithValue("$quyenRieng", tenantCode == HangA ? "301,302,9999" : "304");
        command.Parameters.AddWithValue("$quyenVaiTro", tenantCode == HangA ? "303" : "");
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
