using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Staxi.TenantLeakTests.Fakes;
using Xunit;

namespace Staxi.TenantLeakTests.Tests;

public sealed class PhamViHttpTests : IAsyncLifetime
{
    private UngDungHaiHang _ungDung = null!;

    public async Task InitializeAsync() => _ungDung = await UngDungHaiHang.KhoiDongAsync();

    public async Task DisposeAsync() => await _ungDung.DisposeAsync();

    [Fact(DisplayName = "Token hãng A chỉ thấy dữ liệu hãng A (AD-2, AD-4)")]
    public async Task TokenHangA_ChiThayDuLieuHangA()
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);

        var phanHoi = await clientA.GetAsync(new Uri("/api/loai-xe", UriKind.Relative));
        var noiDung = await phanHoi.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.Contains("HANG-A", noiDung, StringComparison.Ordinal);
        Assert.DoesNotContain("HANG-B", noiDung, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "Tham số query xin hãng khác bị chặn 400, không im lặng bỏ qua (AD-4)")]
    [InlineData("tenantCode=hang-b")]
    [InlineData("tenant_code=hang-b")]
    [InlineData("companyId=2")]
    [InlineData("company-id=2")]
    [InlineData("maHang=hang-b")]
    public async Task ThamSoQueryXinHangKhac_BiChan(string query)
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);

        var phanHoi = await clientA.GetAsync(new Uri($"/api/loai-xe?{query}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
        Assert.DoesNotContain("HANG-B", await phanHoi.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory(DisplayName = "Header phạm vi tự đặt bị chặn — gateway đã xoá, còn tới đây là đi vòng (AD-3, AD-4)")]
    [InlineData("X-Tenant-Code")]
    [InlineData("X-Company-Id")]
    [InlineData("x-staxi-tenant")]
    public async Task HeaderPhamVi_BiChan(string header)
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);
        clientA.DefaultRequestHeaders.Add(header, "hang-b");

        var phanHoi = await clientA.GetAsync(new Uri("/api/loai-xe", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact(DisplayName = "Body mang công ty khác cũng không đổi được phạm vi (AD-4)")]
    public async Task BodyMangCongTyKhac_KhongDoiDuocPhamVi()
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);

        var phanHoi = await clientA.PostAsJsonAsync("/api/loai-xe/theo-cong-ty", new YeuCauTuChonCongTy(2));
        var noiDung = await phanHoi.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.DoesNotContain("cong ty 2", noiDung, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HANG-B", noiDung, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Không token thì 401, không phải 200 kèm dữ liệu (AD-8)")]
    public async Task KhongToken_Thi401()
    {
        using var client = _ungDung.Server.CreateClient();

        var phanHoi = await client.GetAsync(new Uri("/api/loai-xe", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact(DisplayName = "Token ký bằng khoá lạ bị từ chối — claim tự đặt không có giá trị (AD-4, AD-22)")]
    public async Task TokenKyBangKhoaLa_BiTuChoi()
    {
        using var giaMao = _ungDung.TaoClient(HaiHangGia.HangB, 1, HaiHangGia.KhoaKyGiaMao);

        var phanHoi = await giaMao.GetAsync(new Uri("/api/loai-xe", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact(DisplayName = "Lỗi trả ProblemDetails + corrId, tuyệt đối không 200 (AD-8)")]
    public async Task Loi_TraProblemDetailsKemCorrId()
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);

        var phanHoi = await clientA.GetAsync(new Uri("/api/loai-xe/quen-loc", UriKind.Relative));

        Assert.Equal(HttpStatusCode.InternalServerError, phanHoi.StatusCode);
        Assert.Equal("application/problem+json", phanHoi.Content.Headers.ContentType?.MediaType);

        using var tai = JsonDocument.Parse(await phanHoi.Content.ReadAsStringAsync());
        var corrId = tai.RootElement.GetProperty("corrId").GetString();

        Assert.False(string.IsNullOrWhiteSpace(corrId));
        Assert.Equal(corrId, phanHoi.Headers.GetValues("corrId").Single());

        var than = await phanHoi.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SELECT", than, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hang_a.db", than, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Phản hồi thành công cũng mang corrId (AD-8)")]
    public async Task PhanHoiThanhCong_CungMangCorrId()
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);

        var phanHoi = await clientA.GetAsync(new Uri("/api/pham-vi", UriKind.Relative));

        Assert.True(phanHoi.Headers.Contains("corrId"));
        Assert.Contains("hang-a:1", await phanHoi.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Hãng bị vô hiệu hoá thì cắt cả phiên đang chạy, không chỉ chặn đăng nhập mới (AD-14)")]
    public async Task HangBiVoHieuHoa_ThiCatCaPhienDangChay()
    {
        using var clientA = _ungDung.TaoClient(HaiHangGia.HangA, 1);

        (await clientA.GetAsync(new Uri("/api/loai-xe", UriKind.Relative))).EnsureSuccessStatusCode();

        _ungDung.HaiHang.Store.DatTrangThai(HaiHangGia.HangA, dangHoatDong: false);
        await Task.Delay(TimeSpan.FromMilliseconds(400));

        var sauKhiTat = await clientA.GetAsync(new Uri("/api/loai-xe", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Forbidden, sauKhiTat.StatusCode);
    }
}
