using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class DuongXacThucTests(AssemblyCatalog assemblies)
{
    private const string CongXacThuc = "Staxi.Platform.Data.IAuthenticationConnectionFactory";

    private static readonly string[] DuocPhepGoi =
    [
        "Staxi.Platform.Data.AuthenticationConnectionFactory",
    ];

    [Fact(DisplayName = "R16: Chỉ Staxi.Auth được mở kết nối ngoài phạm vi để xác thực (AD-2, AD-3)")]
    public void MoKetNoiXacThuc_ChiDuocTrongStaxiAuth()
    {
        var violations = assemblies.SanPham.SelectMany(MethodCallScanner.Read)
            .Where(call => call.Target.DeclaringType.FullName == CongXacThuc)
            .Where(call => !LaNoiDuocPhep(call.Caller.DeclaringType.FullName))
            .Select(call => $"{call.Location} → {call.Target.Name}");

        ArchitectureAssert.NoViolations(
            "R16",
            "AD-2/AD-3",
            "Xác thực là nơi DUY NHẤT được mở kết nối khi chưa có TenantScope. Mọi nơi khác phải đi qua ITenantConnectionFactory.",
            violations);
    }

    private static bool LaNoiDuocPhep(string kieuGoi)
        => kieuGoi.StartsWith("Staxi.Auth.", StringComparison.Ordinal)
           || DuocPhepGoi.Contains(kieuGoi, StringComparer.Ordinal);
}
