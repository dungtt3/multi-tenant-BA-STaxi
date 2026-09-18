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
    private IReadOnlyList<AssemblyDefinition>? _sanPham;

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

    public IReadOnlyList<AssemblyDefinition> SanPham => _sanPham ??= DocSanPham();

    public IReadOnlyList<AssemblyDefinition> SanPhamVaLeakTests => [.. SanPham, Get<PhamViCacheTests>()];

    public AssemblyDefinition AdminDomain => Get<LoaiXe>();

    private IReadOnlyList<AssemblyDefinition> DocSanPham()
    {
        var cacFile = Directory.EnumerateFiles(AppContext.BaseDirectory, "Staxi.*.dll")
            .Where(duongDan => !Path.GetFileNameWithoutExtension(duongDan).EndsWith("Tests", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (cacFile.Length == 0)
        {
            throw new InvalidOperationException(
                $"Không tìm thấy assembly sản phẩm nào trong '{AppContext.BaseDirectory}'. "
                + "Bài kiểm kiến trúc sẽ xanh một cách giả tạo nếu danh sách này rỗng.");
        }

        return [.. cacFile.Select(Doc)];
    }

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
