using Mono.Cecil;

namespace Staxi.ArchitectureTests.Scanning;

internal sealed record MethodCall(MethodDefinition Caller, MethodReference Target)
{
    public string Location => $"{Caller.DeclaringType.FullName}.{Caller.Name}";
}
