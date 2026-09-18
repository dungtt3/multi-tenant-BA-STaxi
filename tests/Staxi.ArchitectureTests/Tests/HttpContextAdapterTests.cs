using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

[Collection(nameof(AssemblyScanFixture))]
public sealed class HttpContextAdapterTests(AssemblyCatalog assemblies)
{
    [Fact(DisplayName = "R5: HttpContext chỉ xuất hiện trong adapter AspNetCore (AD-15)")]
    public void SuDungHttpContext_ChiDuocTrongAdapter()
    {
        const string adapterNamespace = "Staxi.Platform.AspNetCore";
        const string contextType = "Microsoft.AspNetCore.Http.HttpContext";
        var signatures = TypeScanner.Read(assemblies.Platform)
            .Where(type => TypeScanner.NamespaceOf(type) != adapterNamespace).SelectMany(type => type.Methods)
            .Where(method => TypeReferenceScanner.References(method, contextType))
            .Select(method => $"{method.DeclaringType.FullName}.{method.Name} (chữ ký: {method.FullName})");
        var fields = FieldScanner.Read(assemblies.Platform)
            .Where(field => TypeScanner.NamespaceOf(field.DeclaringType) != adapterNamespace
                && TypeReferenceScanner.Contains(field.FieldType, contextType))
            .Select(field => $"{field.DeclaringType.FullName}.{field.Name} (trường: {field.FieldType.FullName})");
        var calls = MethodCallScanner.Read(assemblies.Platform)
            .Where(call => TypeScanner.NamespaceOf(call.Caller.DeclaringType) != adapterNamespace
                && TypeReferenceScanner.References(call.Target, contextType))
            .Select(call => $"{call.Location} → {call.Target.FullName}");

        ArchitectureAssert.NoViolations("R5", "AD-15", "HttpContext chỉ thuộc namespace Staxi.Platform.AspNetCore.",
            signatures.Concat(fields).Concat(calls));
    }
}
