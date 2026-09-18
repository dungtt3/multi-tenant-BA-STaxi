using Microsoft.AspNetCore.SignalR;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Realtime;

public sealed class SignalRTenantNotifier<THub>(IHubContext<THub> hubContext) : ITenantNotifier
    where THub : Hub
{
    public Task SendAsync(TenantScope scope, string chuDe, string phuongThuc, object? noiDung, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(phuongThuc);

        var group = TenantGroups.For(scope, chuDe);

        return hubContext.Clients.Group(group).SendAsync(phuongThuc, noiDung, ct);
    }
}
