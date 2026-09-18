using Microsoft.AspNetCore.SignalR;
using Staxi.Platform.Tenancy;

namespace Staxi.Platform.Realtime;

public abstract class TenantHub : Hub
{
    private const string KhoaItems = "staxi.tenant-scope";

    protected virtual string ChuDeMacDinh => "chung";

    protected TenantScope Scope
        => Context.Items.TryGetValue(KhoaItems, out var giaTri) && giaTri is TenantScope scope
            ? scope
            : throw new TenantScopeMissingException($"{GetType().Name}.{nameof(Scope)}");

    public override async Task OnConnectedAsync()
    {
        var scope = TenantScopeClaims.Read(Context.User);

        if (scope is null)
        {
            Context.Abort();
            return;
        }

        Context.Items[KhoaItems] = scope;

        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroups.For(scope, ChuDeMacDinh)).ConfigureAwait(false);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    protected Task VaoChuDeAsync(string chuDe)
        => Groups.AddToGroupAsync(Context.ConnectionId, TenantGroups.For(Scope, chuDe));

    protected Task RoiChuDeAsync(string chuDe)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, TenantGroups.For(Scope, chuDe));
}
