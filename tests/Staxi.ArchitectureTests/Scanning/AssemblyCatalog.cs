using Mono.Cecil;
using Staxi.Admin.Application;
using Staxi.Admin.Domain;
using Staxi.Admin.Infrastructure;
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

    public IReadOnlyList<AssemblyDefinition> SanPham =>
    [
        Platform,
        Get<LoaiXe>(),
        Get<ILoaiXeRepository>(),
        Get<LoaiXeRepository>(),
        GetTheoTen("Staxi.Admin.Api"),
    ];

    public IReadOnlyList<AssemblyDefinition> SanPhamVaLeakTests => [.. SanPham, Get<PhamViCacheTests>()];

    public AssemblyDefinition AdminDomain => Get<LoaiXe>();

    public AssemblyDefinition GetTheoTen(string tenAssembly)
    {
        var path = Path.Combine(AppContext.BaseDirectory, tenAssembly + ".dll");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy assembly '{path}'. Bài kiểm kiến trúc phải quét được assembly sản phẩm này, "
                + "nên project test phải tham chiếu tới nó.");
        }

        return Doc(path);
    }

    public AssemblyDefinition Get<T>() => Doc(typeof(T).Assembly.Location);

    private AssemblyDefinition Doc(string path)
    {
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
