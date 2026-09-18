using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class DongHoTests(AssemblyCatalog assemblies)
{
    [Fact(DisplayName = "R4: Chỉ SystemClock được đọc giờ hệ thống trực tiếp (AD-16)")]
    public void DocGioHeThong_ChiDuocTrongSystemClock()
    {
        var violations = assemblies.SanPham.SelectMany(MethodCallScanner.Read)
            .Where(call => call.Target.DeclaringType.FullName is "System.DateTime" or "System.DateTimeOffset"
                && call.Target.Name is "get_Now" or "get_UtcNow" or "get_Today"
                && call.Caller.DeclaringType.FullName != "Staxi.Platform.Time.SystemClock")
            .Select(call => $"{call.Location} → {call.Target.FullName}");

        ArchitectureAssert.NoViolations("R4", "AD-16", "Đọc giờ hệ thống qua IClock; chỉ SystemClock được gọi trực tiếp.", violations);
    }
}
