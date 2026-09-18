using Mono.Cecil;

namespace Staxi.ArchitectureTests.Scanning;

internal static class TypeReferenceScanner
{
    public static bool Contains(TypeReference type, string fullName)
        => type.FullName == fullName
           || type is GenericInstanceType generic && generic.GenericArguments.Any(argument => Contains(argument, fullName))
           || type is TypeSpecification specification && Contains(specification.ElementType, fullName);

    public static bool References(MethodReference method, string fullName)
        => Contains(method.DeclaringType, fullName)
           || Contains(method.ReturnType, fullName)
           || method.Parameters.Any(parameter => Contains(parameter.ParameterType, fullName))
           || method is GenericInstanceMethod generic && generic.GenericArguments.Any(argument => Contains(argument, fullName));
}
