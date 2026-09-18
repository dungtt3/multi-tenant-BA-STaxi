using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Realtime;

public static class TenantGroups
{
    public static string For(TenantScope scope, string chuDe)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(chuDe);

        if (chuDe.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException($"Chủ đề '{chuDe}' chứa dấu hai chấm nên có thể giả được group của hãng khác.", nameof(chuDe));
        }

        return $"{scope.Prefix}:{chuDe}";
    }
}

public interface ITenantNotifier
{
    public Task SendAsync(TenantScope scope, string chuDe, string phuongThuc, object? noiDung, CancellationToken ct = default);
}
