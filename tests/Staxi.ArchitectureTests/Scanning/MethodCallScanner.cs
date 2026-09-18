using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Staxi.ArchitectureTests.Scanning;

internal static class MethodCallScanner
{
    public static IEnumerable<MethodCall> Read(AssemblyDefinition assembly)
        => TypeScanner.Read(assembly).SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions
                .Where(instruction => instruction.OpCode.Code is Code.Call or Code.Callvirt or Code.Newobj)
                .Select(instruction => new MethodCall(method, (MethodReference)instruction.Operand)));
}
