using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class HuongPhuThuocTests(AssemblyCatalog assemblies)
{
    private static readonly string[] CamTrongDomain =
    [
        "Staxi.Admin.Infrastructure",
        "Staxi.Admin.Api",
        "Staxi.Contracts",
        "Dapper",
        "Microsoft.Data.SqlClient",
    ];

    [Fact(DisplayName = "R15: Tầng Domain không tham chiếu Infrastructure, Api hay ASP.NET Core")]
    public void TangDomain_KhongThamChieuRaNgoai()
    {
        var domain = assemblies.AdminDomain;
        var violations = domain.MainModule.AssemblyReferences
            .Where(reference => CamTrongDomain.Contains(reference.Name, StringComparer.Ordinal)
                || reference.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .Select(reference => $"{domain.Name.Name} tham chiếu {reference.Name} — phụ thuộc chỉ được đi vào trong");

        ArchitectureAssert.NoViolations(
            "R15",
            "Clean Architecture",
            "Domain là tầng trong cùng: nó không biết gì về cách dữ liệu được lưu hay được đưa lên mạng.",
            violations);
    }
}
