using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Staxi.Platform.Tenancy;
using Staxi.Platform.Time;

namespace Staxi.Platform.Data;

public interface ITenantRegistryStore
{
    public Task<TenantRegistryEntry?> LoadAsync(string tenantCode, CancellationToken ct = default);
}

public sealed class CachingTenantRegistry(
    ITenantRegistryStore store,
    IClock clock,
    ILogger<CachingTenantRegistry> logger,
    TimeSpan? thoiHanCache = null) : ITenantRegistry
{
    private readonly ConcurrentDictionary<string, MucCache> _cache = new(StringComparer.Ordinal);
    private readonly TimeSpan _thoiHanCache = thoiHanCache ?? TimeSpan.FromMinutes(1);

    public async ValueTask<TenantRegistryEntry> GetAsync(string tenantCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);

        var khoa = tenantCode.ToLowerInvariant();
        var bayGio = clock.UtcNow;

        if (_cache.TryGetValue(khoa, out var muc) && muc.HetHan > bayGio)
        {
            return muc.Entry;
        }

        TenantRegistryEntry? nap;

        try
        {
            nap = await store.LoadAsync(khoa, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (muc is not null)
        {
            logger.LogWarning(ex, "Không đọc được sổ đăng ký cho hãng {TenantCode}; dùng bản cache cũ (AD-14).", khoa);
            return muc.Entry;
        }

        if (nap is null)
        {
            _cache.TryRemove(khoa, out _);
            throw new TenantKhongTonTaiException(khoa, "không có trong sổ đăng ký hãng");
        }

        if (!nap.DangHoatDong)
        {
            _cache.TryRemove(khoa, out _);
            throw new TenantKhongTonTaiException(khoa, "đã bị vô hiệu hoá trong sổ đăng ký");
        }

        _cache[khoa] = new MucCache(nap, bayGio.Add(_thoiHanCache));
        return nap;
    }

    private sealed record MucCache(TenantRegistryEntry Entry, DateTimeOffset HetHan);
}

public sealed class InMemoryTenantRegistryStore(IEnumerable<TenantRegistryEntry> entries) : ITenantRegistryStore
{
    private readonly ConcurrentDictionary<string, TenantRegistryEntry> _entries =
        new(entries.ToDictionary(e => e.TenantCode.ToLowerInvariant(), StringComparer.Ordinal), StringComparer.Ordinal);

    public Task<TenantRegistryEntry?> LoadAsync(string tenantCode, CancellationToken ct = default)
        => Task.FromResult(_entries.TryGetValue(tenantCode.ToLowerInvariant(), out var entry) ? entry : null);

    public void DatTrangThai(string tenantCode, bool dangHoatDong)
    {
        var khoa = tenantCode.ToLowerInvariant();

        if (_entries.TryGetValue(khoa, out var entry))
        {
            _entries[khoa] = entry with { DangHoatDong = dangHoatDong };
        }
    }
}
