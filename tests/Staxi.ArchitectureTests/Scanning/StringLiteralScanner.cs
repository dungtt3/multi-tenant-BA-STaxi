using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Staxi.ArchitectureTests.Scanning;

internal readonly record struct StringLiteral(MethodDefinition Method, string Value);

internal static class StringLiteralScanner
{
    public static IEnumerable<StringLiteral> Read(AssemblyDefinition assembly)
        => TypeScanner.Read(assembly).SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions
                .Where(instruction => instruction.OpCode.Code == Code.Ldstr)
                .Select(instruction => new StringLiteral(method, (string)instruction.Operand)));
}
