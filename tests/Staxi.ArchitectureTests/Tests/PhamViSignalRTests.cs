using Microsoft.AspNetCore.SignalR;
using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Staxi.Platform.Tenancy;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class PhamViSignalRTests(AssemblyCatalog assemblies)
{
    [Fact(DisplayName = "R2: Cấm phát SignalR vượt phạm vi, kể cả trong leak test (AD-7)")]
    public void GoiSignalR_KhongDuocPhatVuotPhamVi()
    {
        var violations = assemblies.SanPhamVaLeakTests.SelectMany(MethodCallScanner.Read)
            .Where(call => call.Target.DeclaringType.Namespace == "Microsoft.AspNetCore.SignalR"
                && call.Target.Name is "get_All" or "get_Others" or "AllExcept" or "OthersInGroup")
            .Select(call => $"{call.Location} → {call.Target.FullName}");

        ArchitectureAssert.NoViolations("R2", "AD-7", "Cấm Clients.All, Others, AllExcept và OthersInGroup.", violations);
    }

    [Fact(DisplayName = "R3: Hub method không nhận tham số phạm vi, kể cả trong leak test (AD-7)")]
    public void HubMethod_KhongNhanThamSoPhamVi()
    {
        string[] forbiddenNames = ["tenant", "tenantcode", "tenantid", "company", "companyid", "companycode", "mahang", "congty", "xncode"];
        var violations = assemblies.SanPhamVaLeakTests.SelectMany(TypeScanner.Read)
            .Where(type => TypeScanner.InheritsFrom(type, typeof(Hub).FullName!))
            .SelectMany(type => type.Methods).Where(method => method.IsPublic && !method.IsConstructor)
            .SelectMany(method => method.Parameters
                .Where(parameter => forbiddenNames.Contains(
                    parameter.Name.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal),
                    StringComparer.OrdinalIgnoreCase)
                    || TypeReferenceScanner.Contains(parameter.ParameterType, typeof(TenantScope).FullName!))
                .Select(parameter => $"{method.DeclaringType.FullName}.{method.Name} (tham số {parameter.Name}: {parameter.ParameterType.FullName})"));

        ArchitectureAssert.NoViolations("R3", "AD-7", "Hub method phải lấy phạm vi từ token, không từ tham số.", violations);
    }
}
