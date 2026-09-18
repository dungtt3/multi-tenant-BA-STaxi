using Mono.Cecil;

namespace Staxi.ArchitectureTests.Scanning;

internal static class TypeScanner
{
    public static IEnumerable<TypeDefinition> Read(AssemblyDefinition assembly)
        => assembly.Modules.SelectMany(module => module.Types).SelectMany(Read);

    public static bool IsCompilerGenerated(ICustomAttributeProvider member)
        => member.CustomAttributes.Any(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    public static string NamespaceOf(TypeDefinition type)
        => type.DeclaringType is null ? type.Namespace : NamespaceOf(type.DeclaringType);

    public static bool InheritsFrom(TypeDefinition type, string baseTypeName)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.Resolve().BaseType)
        {
            if (baseType.GetElementType().FullName == baseTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<TypeDefinition> Read(TypeDefinition type)
    {
        if (IsCompilerGenerated(type) || type.Name.StartsWith('<'))
        {
            yield break;
        }

        yield return type;
        foreach (var nested in type.NestedTypes.SelectMany(Read))
        {
            yield return nested;
        }
    }
}
