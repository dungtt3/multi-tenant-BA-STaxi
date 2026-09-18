using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Staxi.Platform.Realtime;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViSignalRTests : IAsyncLifetime
{
    private static readonly TimeSpan ChoNhanTin = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan ChoKhangDinhIm = TimeSpan.FromSeconds(2);

    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Phát tin ở hãng A: A nhận, B không nhận gì (AD-7)")]
    public async Task PhatTinOHangA_HangBKhongNhanGi()
    {
        await using var ketNoiA = await KetNoiAsync(HaiHangGia.HangA, 1);
        await using var ketNoiB = await KetNoiAsync(HaiHangGia.HangB, 1);

        var nhanCuaA = ChoMotTin(ketNoiA);
        var nhanCuaB = ChoMotTin(ketNoiB);

        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);
        var phanHoi = await clientA.PostAsJsonAsync("/api/thong-bao", new PhatTin("chung", "cuoc goi moi cua HANG-A"));
        phanHoi.EnsureSuccessStatusCode();

        Assert.Equal("cuoc goi moi cua HANG-A", await nhanCuaA.WaitAsync(ChoNhanTin));
        await KhangDinhKhongNhanAsync(nhanCuaB);
    }

    [Fact(DisplayName = "Cùng hãng, khác công ty: công ty 2 không nhận tin của công ty 1 (AD-5, AD-7)")]
    public async Task CungHangKhacCongTy_CungKhongNhan()
    {
        await using var congTy1 = await KetNoiAsync(HaiHangGia.HangA, 1);
        await using var congTy2 = await KetNoiAsync(HaiHangGia.HangA, 2);

        var nhanCuaCongTy1 = ChoMotTin(congTy1);
        var nhanCuaCongTy2 = ChoMotTin(congTy2);

        using var client = _ungDung.TaoClient(HaiHangGia.HangA, 1);
        (await client.PostAsJsonAsync("/api/thong-bao", new PhatTin("chung", "tin cua cong ty 1"))).EnsureSuccessStatusCode();

        Assert.Equal("tin cua cong ty 1", await nhanCuaCongTy1.WaitAsync(ChoNhanTin));
        await KhangDinhKhongNhanAsync(nhanCuaCongTy2);
    }

    [Fact(DisplayName = "Client tự phát tin cũng chỉ tới phạm vi của chính kết nối đó (AD-7)")]
    public async Task ClientTuPhatTin_ChiToiPhamViCuaChinhNo()
    {
        await using var ketNoiA = await KetNoiAsync(HaiHangGia.HangA, 1);
        await using var ketNoiB = await KetNoiAsync(HaiHangGia.HangB, 1);

        var nhanCuaA = ChoMotTin(ketNoiA);
        var nhanCuaB = ChoMotTin(ketNoiB);

        await ketNoiB.InvokeAsync("PhatTinTrongPhamVi", "chung", "tin do HANG-B tu phat");

        Assert.Equal("tin do HANG-B tu phat", await nhanCuaB.WaitAsync(ChoNhanTin));
        await KhangDinhKhongNhanAsync(nhanCuaA);
    }

    [Fact(DisplayName = "Kết nối không token bị đóng, không có kết nối chưa phân hạng nằm chờ (AD-7)")]
    public async Task KetNoiKhongToken_BiDong()
    {
        await using var ketNoi = TaoKetNoi(token: null);

        await Assert.ThrowsAnyAsync<Exception>(() => ketNoi.StartAsync());
    }

    [Fact(DisplayName = "Tên group sinh bởi đúng một hàm, mang đủ hãng và công ty (AD-7)")]
    public void TenGroup_MangDuPhamVi()
    {
        Assert.Equal("hang-a:1:chung", TenantGroups.For(HaiHangGia.ScopeA, "chung"));
        Assert.Equal("hang-b:1:chung", TenantGroups.For(HaiHangGia.ScopeB, "chung"));
        Assert.Throws<ArgumentException>(() => TenantGroups.For(HaiHangGia.ScopeA, "chung:hang-b:1"));
    }

    private static TaskCompletionSource<string> DangKyNhan(HubConnection ketNoi)
    {
        var hop = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        ketNoi.On<string>("NhanTin", noiDung => hop.TrySetResult(noiDung));

        return hop;
    }

    private static Task<string> ChoMotTin(HubConnection ketNoi) => DangKyNhan(ketNoi).Task;

    private static async Task KhangDinhKhongNhanAsync(Task<string> cho)
    {
        var xong = await Task.WhenAny(cho, Task.Delay(ChoKhangDinhIm));

        if (xong == cho)
        {
            Assert.Fail($"RÒ RỈ: hãng kia nhận được bản tin '{await cho}' (AD-7).");
        }
    }

    private async Task<HubConnection> KetNoiAsync(string tenantCode, int companyId)
    {
        var ketNoi = TaoKetNoi(HaiHangGia.CapToken(tenantCode, companyId));
        await ketNoi.StartAsync();

        return ketNoi;
    }

    private HubConnection TaoKetNoi(string? token)
        => new HubConnectionBuilder()
            .WithUrl(new Uri(_ungDung.Server.BaseAddress, "hubs/thong-bao"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _ungDung.Server.CreateHandler();

                if (token is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            })
            .Build();
}
