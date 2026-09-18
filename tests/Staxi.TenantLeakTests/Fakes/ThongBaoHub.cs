using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Staxi.Platform.Realtime;

namespace Staxi.TenantLeakTests.Fakes;

[Authorize]
public sealed class ThongBaoHub : TenantHub
{
    public Task ThamGiaChuDe(string chuDe) => VaoChuDeAsync(chuDe);

    public Task PhatTinTrongPhamVi(string chuDe, string noiDung)
        => Clients.Group(TenantGroups.For(Scope, chuDe)).SendAsync("NhanTin", noiDung);
}
