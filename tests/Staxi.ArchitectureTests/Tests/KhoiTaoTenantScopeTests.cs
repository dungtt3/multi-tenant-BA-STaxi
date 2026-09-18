using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Staxi.Platform.Tenancy;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

public sealed class KhoiTaoTenantScopeTests
{
    [Fact(DisplayName = "R11: TenantScope chỉ tạo từ token đã ký hoặc công việc nền (AD-4)")]
    public void KhoiTaoTenantScope_ChiQuaHaiFactoryDuocPhep()
    {
        var violations = PublicApiScanner.Constructors(typeof(TenantScope)).Select(PublicApiScanner.Location)
            .Concat(PublicApiScanner.Factories(typeof(TenantScope))
                .Where(method => method.Name is not nameof(TenantScope.FromSignedToken) and not nameof(TenantScope.FromBackgroundJob))
                .Select(PublicApiScanner.Location));

        ArchitectureAssert.NoViolations("R11", "AD-4", "TenantScope không có constructor công khai; chỉ cho phép FromSignedToken và FromBackgroundJob.", violations);
    }
}
