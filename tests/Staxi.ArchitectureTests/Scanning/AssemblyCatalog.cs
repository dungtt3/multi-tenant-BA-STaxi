using Mono.Cecil;
using Staxi.Platform.Tenancy;
using Staxi.TenantLeakTests.Tests;

namespace Staxi.ArchitectureTests.Scanning;

public sealed class AssemblyCatalog : IDisposable
{
    private readonly DefaultAssemblyResolver _resolver = new();
    private readonly Dictionary<string, AssemblyDefinition> _assemblies = new(StringComparer.Ordinal);

    public AssemblyCatalog()
    {
        _resolver.AddSearchDirectory(AppContext.BaseDirectory);
        var runtimePaths = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        foreach (var directory in runtimePaths.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                     .Select(Path.GetDirectoryName).OfType<string>().Distinct(StringComparer.Ordinal))
        {
            _resolver.AddSearchDirectory(directory);
        }
    }

    public AssemblyDefinition Platform => Get<TenantScope>();

    public IReadOnlyList<AssemblyDefinition> PlatformAndLeakTests => [Platform, Get<PhamViCacheTests>()];

    public AssemblyDefinition Get<T>()
    {
        var path = typeof(T).Assembly.Location;
        if (!_assemblies.TryGetValue(path, out var assembly))
        {
            assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = _resolver });
            _assemblies.Add(path, assembly);
        }

        return assembly;
    }

    public void Dispose()
    {
        foreach (var assembly in _assemblies.Values)
        {
            assembly.Dispose();
        }

        _assemblies.Clear();
        _resolver.Dispose();
    }
}
