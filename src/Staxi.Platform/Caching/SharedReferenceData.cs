namespace Staxi.Platform.Caching;

public sealed class SharedReferenceItem
{
    internal SharedReferenceItem(string ten, string nguonThamQuyen, string lyDo)
    {
        Ten = ten;
        NguonThamQuyen = nguonThamQuyen;
        LyDo = lyDo;
    }

    public string Ten { get; }

    public string NguonThamQuyen { get; }

    public string LyDo { get; }

    public override string ToString() => Ten;
}

public static class SharedReferenceData
{
    public static IReadOnlyDictionary<string, SharedReferenceItem> Catalog { get; } =
        new Dictionary<string, SharedReferenceItem>(StringComparer.Ordinal)
        {
        };

    public static SharedReferenceItem Require(string ten)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ten);

        return Catalog.TryGetValue(ten, out var muc)
            ? muc
            : throw new SharedReferenceDataChuaDangKyException(ten);
    }
}

public sealed class SharedReferenceDataChuaDangKyException(string ten) : InvalidOperationException(
    $"'{ten}' không có trong sổ SharedReferenceData nên không được dùng key cache không mang tenant. " +
    "Mỗi hãng có database riêng ⇒ danh mục là của riêng hãng đó. Dùng CacheKey.For(scope, ...) (AD-6).")
{
    public string Ten { get; } = ten;
}
