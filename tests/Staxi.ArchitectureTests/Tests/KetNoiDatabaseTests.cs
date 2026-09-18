using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Staxi.Platform.Data;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class KetNoiDatabaseTests(AssemblyCatalog assemblies)
{
    [Fact(DisplayName = "R1: Chỉ SqlServerConnectionSource được tạo SqlConnection, kể cả trong leak test (AD-2)")]
    public void TaoSqlConnection_ChiDuocTrongConnectionSource()
    {
        var violations = assemblies.PlatformAndLeakTests.SelectMany(MethodCallScanner.Read)
            .Where(call => call.Target.Name == ".ctor" && call.Target.DeclaringType.Name == "SqlConnection"
                && call.Caller.DeclaringType.FullName != "Staxi.Platform.Data.SqlServerConnectionSource")
            .Select(call => $"{call.Location} → {call.Target.FullName}");

        ArchitectureAssert.NoViolations("R1", "AD-2", "SqlConnection chỉ được tạo trong SqlServerConnectionSource.", violations);
    }

    [Fact(DisplayName = "R7: ITenantConnectionFactory chỉ nhận CancellationToken (AD-2)")]
    public void ThamSoConnectionFactory_ChiNhanCancellationToken()
    {
        var contract = typeof(ITenantConnectionFactory);
        var violations = contract.GetInterfaces().Append(contract).SelectMany(type => type.GetMethods())
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType != typeof(CancellationToken)
                || (parameter.Name?.Contains("tenant", StringComparison.OrdinalIgnoreCase) ?? false)
                || (parameter.Name?.Contains("company", StringComparison.OrdinalIgnoreCase) ?? false)))
            .Select(PublicApiScanner.Location);

        ArchitectureAssert.NoViolations("R7", "AD-2", "Factory không nhận phạm vi; chỉ nhận CancellationToken.", violations);
    }
}
