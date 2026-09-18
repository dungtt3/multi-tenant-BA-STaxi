using Microsoft.Extensions.Caching.Memory;

namespace Staxi.Platform.Caching;

public sealed class MemoryTenantCache(IMemoryCache cache) : ITenantCache
{
    public ValueTask<T?> GetAsync<T>(CacheKey khoa, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(khoa);
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult(cache.TryGetValue(khoa.GiaTri, out var giaTri) ? (T?)giaTri : default);
    }

    public ValueTask SetAsync<T>(CacheKey khoa, T giaTri, TimeSpan thoiHan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(khoa);
        ct.ThrowIfCancellationRequested();

        cache.Set(khoa.GiaTri, giaTri, thoiHan);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(CacheKey khoa, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(khoa);
        ct.ThrowIfCancellationRequested();

        cache.Remove(khoa.GiaTri);
        return ValueTask.CompletedTask;
    }
}
