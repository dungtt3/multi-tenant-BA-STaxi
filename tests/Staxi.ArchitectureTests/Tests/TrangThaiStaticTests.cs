using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class TrangThaiStaticTests(AssemblyCatalog assemblies)
{
    [Fact(DisplayName = "R6: Mọi static field phải là const hoặc readonly (AD-15)")]
    public void TruongStatic_PhaiLaConstHoacReadonly()
    {
        var violations = FieldScanner.Read(assemblies.Platform)
            .Where(field => field.IsStatic && !field.IsLiteral && !field.IsInitOnly)
            .Select(field => $"{field.DeclaringType.FullName}.{field.Name} (trường static có thể thay đổi)");

        ArchitectureAssert.NoViolations("R6", "AD-15", "Cấm trạng thái static thay đổi được.", violations);
    }
}
