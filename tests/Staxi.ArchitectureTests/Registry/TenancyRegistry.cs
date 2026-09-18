using System.Text.Json.Serialization;

namespace Staxi.ArchitectureTests.Registry;

public sealed class TenancyTableEntry
{
    [JsonPropertyName("ten")]
    public string Ten { get; init; } = string.Empty;

    [JsonPropertyName("thucThe")]
    public string ThucThe { get; init; } = string.Empty;

    [JsonPropertyName("hang")]
    public string Hang { get; init; } = string.Empty;

    [JsonPropertyName("trangThai")]
    public string TrangThai { get; init; } = string.Empty;

    [JsonPropertyName("chungCu")]
    public string ChungCu { get; init; } = string.Empty;

    [JsonPropertyName("canhBao")]
    public IReadOnlyList<string> CanhBao { get; init; } = [];

    public IEnumerable<string> CacThucThe
        => ThucThe.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed class TenancyThongKe
{
    [JsonPropertyName("tongSoBangTrongSo")]
    public int TongSoBangTrongSo { get; init; }

    [JsonPropertyName("daDuyet")]
    public int DaDuyet { get; init; }

    [JsonPropertyName("chuaDuyet")]
    public int ChuaDuyet { get; init; }

    [JsonPropertyName("theoHang")]
    public IReadOnlyDictionary<string, int> TheoHang { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    [JsonPropertyName("soMucCoCanhBao")]
    public int SoMucCoCanhBao { get; init; }
}

public sealed class TenancyRegistry
{
    [JsonPropertyName("hangHopLe")]
    public IReadOnlyList<string> HangHopLe { get; init; } = [];

    [JsonPropertyName("trangThaiHopLe")]
    public IReadOnlyList<string> TrangThaiHopLe { get; init; } = [];

    [JsonPropertyName("thongKe")]
    public TenancyThongKe ThongKe { get; init; } = new();

    [JsonPropertyName("bang")]
    public IReadOnlyList<TenancyTableEntry> Bang { get; init; } = [];
}
