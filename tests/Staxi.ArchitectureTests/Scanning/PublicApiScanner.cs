using System.Reflection;

namespace Staxi.ArchitectureTests.Scanning;

internal static class PublicApiScanner
{
    public static IEnumerable<ConstructorInfo> Constructors(Type type)
        => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

    public static IEnumerable<MethodInfo> Factories(Type type)
        => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.ReturnType == type);

    public static string Location(MemberInfo member)
        => $"{member.DeclaringType?.FullName}.{member.Name} ({member})";
}
