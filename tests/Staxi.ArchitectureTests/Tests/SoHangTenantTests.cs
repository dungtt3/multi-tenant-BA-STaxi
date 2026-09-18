using Mono.Cecil;
using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Registry;
using Staxi.ArchitectureTests.Scanning;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class SoHangTenantTests(AssemblyCatalog assemblies)
{
    private const string TenantOwned = "Staxi.Platform.Data.ITenantOwned";

    [Fact(DisplayName = "R12: Sổ hạng tenant phải hợp lệ và thống kê phải khớp (AD-5)")]
    public void SoHangTenant_PhaiHopLe()
    {
        var registry = TenancyRegistryLoader.Load();

        ArchitectureAssert.NoViolations(
            "R12",
            "AD-5",
            "Sổ db/tenancy-classes.json phải hợp lệ: mỗi bảng đúng một hạng, hạng và trạng thái nằm trong danh sách cho phép, thống kê khớp nội dung.",
            TenancyRegistryValidator.Validate(registry));
    }

    [Fact(DisplayName = "R13: Mọi thực thể ITenantOwned phải có trong sổ với hạng PerCompany (AD-5)")]
    public void ThucTheTenantOwned_PhaiCoTrongSoVaLaPerCompany()
    {
        var registry = TenancyRegistryLoader.Load();
        var theoThucThe = registry.Bang
            .SelectMany(muc => muc.CacThucThe.Select(ten => (Ten: ten, Muc: muc)))
            .GroupBy(cap => cap.Ten, cap => cap.Muc, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(nhom => nhom.Key, nhom => nhom.ToArray(), StringComparer.OrdinalIgnoreCase);

        var violations = TypeScanner.Read(assemblies.Platform)
            .Where(type => type.Interfaces.Any(giaoDien => giaoDien.InterfaceType.FullName == TenantOwned))
            .Select(type => KiemTraThucThe(type, theoThucThe))
            .OfType<string>();

        ArchitectureAssert.NoViolations(
            "R13",
            "AD-5",
            "Thực thể implement ITenantOwned là thực thể thuộc về một công ty, nên phải có mục trong sổ hạng tenant với hạng PerCompany.",
            violations);
    }

    [Fact(DisplayName = "R14: Bảng mà code sản phẩm chạm tới phải đã được duyệt hạng (AD-5)")]
    public void BangTrongSqlCuaCodeSanPham_PhaiDaDuyet()
    {
        var registry = TenancyRegistryLoader.Load();
        var theoTen = registry.Bang
            .GroupBy(muc => SqlTableNameReader.ChuanHoa(muc.Ten), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(nhom => nhom.Key, nhom => nhom.First(), StringComparer.OrdinalIgnoreCase);

        var violations = StringLiteralScanner.Read(assemblies.Platform)
            .SelectMany(hang => SqlTableNameReader.Read(hang.Value)
                .Select(ten => KiemTraBang(hang.Method, ten, theoTen)))
            .OfType<string>();

        ArchitectureAssert.NoViolations(
            "R14",
            "AD-5",
            "Mọi bảng xuất hiện trong câu SQL của code sản phẩm phải có trong sổ hạng tenant và phải ở trạng thái daDuyet.",
            violations);
    }

    private static string? KiemTraThucThe(TypeDefinition type, Dictionary<string, TenancyTableEntry[]> theoThucThe)
    {
        if (!theoThucThe.TryGetValue(type.Name, out var cacMuc))
        {
            return $"{type.FullName} implement ITenantOwned nhưng không có mục nào trong sổ mang tên thực thể '{type.Name}' "
                   + "(thêm mục cho bảng của nó, hoặc bổ sung tên thực thể vào mục sẵn có)";
        }

        return cacMuc.Any(muc => muc.Hang == "PerCompany")
            ? null
            : $"{type.FullName} implement ITenantOwned nhưng mục trong sổ có hạng "
              + $"{string.Join(", ", cacMuc.Select(muc => $"'{muc.Hang}' cho bảng {muc.Ten}"))} thay vì PerCompany";
    }

    private static string? KiemTraBang(MethodDefinition method, string ten, Dictionary<string, TenancyTableEntry> theoTen)
    {
        var noiGoi = $"{method.DeclaringType.FullName}.{method.Name}";

        if (!theoTen.TryGetValue(ten, out var muc))
        {
            return $"{noiGoi}: bảng '{ten}' chưa có trong sổ hạng tenant — thêm mục cho nó vào db/tenancy-classes.json "
                   + "rồi duyệt, theo quy trình ở db/README.md";
        }

        return muc.TrangThai == "daDuyet"
            ? null
            : $"{noiGoi}: bảng '{ten}' đang ở trạng thái '{muc.TrangThai}' — hạng {muc.Hang} mới là phỏng đoán, chưa có người duyệt. "
              + "Duyệt theo nhu cầu: duyệt bảng này trong cùng thay đổi đang làm, theo quy trình ở db/README.md";
    }
}
