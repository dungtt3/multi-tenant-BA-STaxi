using Mono.Cecil;

namespace Staxi.ArchitectureTests.Scanning;

internal static class FieldScanner
{
    public static IEnumerable<FieldDefinition> Read(AssemblyDefinition assembly)
        => TypeScanner.Read(assembly).SelectMany(type => type.Fields)
            .Where(field => !TypeScanner.IsCompilerGenerated(field));
}
