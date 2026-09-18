namespace Staxi.Platform.Caching;

public interface ITenantCache
{
    public ValueTask<T?> GetAsync<T>(CacheKey khoa, CancellationToken ct = default);

    public ValueTask SetAsync<T>(CacheKey khoa, T giaTri, TimeSpan thoiHan, CancellationToken ct = default);

    public ValueTask RemoveAsync(CacheKey khoa, CancellationToken ct = default);
}
