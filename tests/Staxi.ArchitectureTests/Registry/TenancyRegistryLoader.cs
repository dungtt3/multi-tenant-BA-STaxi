using System.Text.Json;

namespace Staxi.ArchitectureTests.Registry;

internal static class TenancyRegistryLoader
{
    private static readonly JsonSerializerOptions TuyChon = new() { PropertyNameCaseInsensitive = true };

    public static string DuongDan => Path.Combine(AppContext.BaseDirectory, "db", "tenancy-classes.json");

    public static TenancyRegistry Load()
    {
        if (!File.Exists(DuongDan))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy sổ hạng tenant tại '{DuongDan}'. AD-5: bảng chưa phân hạng là build đỏ, "
                + "nên sổ này phải tồn tại và phải được chép sang thư mục output của bộ test.");
        }

        var registry = JsonSerializer.Deserialize<TenancyRegistry>(File.ReadAllText(DuongDan), TuyChon)
                       ?? throw new InvalidOperationException($"Sổ hạng tenant tại '{DuongDan}' rỗng hoặc không phân giải được.");

        return registry;
    }
}
