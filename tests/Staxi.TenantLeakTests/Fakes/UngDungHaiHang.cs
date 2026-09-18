using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Staxi.Admin.Application;
using Staxi.Admin.Infrastructure;
using Staxi.Auth.Application;
using Staxi.Auth.Infrastructure;
using Staxi.Platform.Authorization;
using Staxi.Platform.Caching;
using Staxi.Platform.Data;
using Staxi.Platform.DependencyInjection;
using Staxi.Platform.Realtime;
using Staxi.Platform.Tenancy;

namespace Staxi.TenantLeakTests.Fakes;

public sealed class UngDungHaiHang : IAsyncDisposable
{
    private readonly IHost _host;

    private UngDungHaiHang(IHost host, HaiHangGia haiHang)
    {
        _host = host;
        HaiHang = haiHang;
    }

    public HaiHangGia HaiHang { get; }

    public TestServer Server => _host.GetTestServer();

    public IServiceProvider Services => _host.Services;

    public static async Task<UngDungHaiHang> KhoiDongAsync()
    {
        var haiHang = new HaiHangGia();

        var host = await new HostBuilder()
            .UseStaxiServiceProviderValidation()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services => DangKyDichVu(services, haiHang));
                web.Configure(RapPipeline);
            })
            .StartAsync();

        return new UngDungHaiHang(host, haiHang);
    }

    private static void DangKyDichVu(IServiceCollection services, HaiHangGia haiHang)
    {
        services.AddSingleton<IDbConnectionSource, SqliteConnectionSource>();
        services.AddSingleton<IConnectionScopeVerifier, SqliteConnectionScopeVerifier>();
        services.AddSingleton<ITenantRegistryStore>(haiHang.Store);

        services.AddSingleton<ITenantRegistry>(sp => new CachingTenantRegistry(
            sp.GetRequiredService<ITenantRegistryStore>(),
            sp.GetRequiredService<Staxi.Platform.Time.IClock>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachingTenantRegistry>>(),
            TimeSpan.FromMilliseconds(200)));

        services.AddStaxiPlatform();
        services.AddScoped<LoaiXeRepository>();
        services.AddScoped<ILoaiXeRepository, Staxi.Admin.Infrastructure.LoaiXeRepository>();
        services.AddScoped<LayDanhSachLoaiXe>();
        services.AddScoped<INguoiDungXacThucRepository, NguoiDungXacThucRepository>();
        services.AddSingleton<IKiemMatKhau, MatKhauKeThuaMd5>();
        services.AddSingleton<ISecurityStampFactory, SecurityStampTuDuLieu>();
        services.AddSingleton<ITokenFactory>(sp => new JwtTokenFactory(
            new CauHinhToken(HaiHangGia.KhoaKyTest, "staxi-test", "staxi-test", TimeSpan.FromMinutes(10)),
            sp.GetRequiredService<Staxi.Platform.Time.IClock>()));
        services.AddScoped<DangNhap>();
        services.AddSingleton<IPermissionCatalog, Staxi.Admin.Application.QuyenDanhMuc>();
        services.AddScoped<IPermissionSource, QuyenTuDatabase>();
        services.AddSignalR();
        services.AddSingleton<ITenantNotifier, SignalRTenantNotifier<ThongBaoHub>>();
        services.AddAuthorization();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "staxi-test",
                    ValidAudience = "staxi-test",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(HaiHangGia.KhoaKyTest)),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });
    }

    private static void RapPipeline(IApplicationBuilder app)
    {
        app.UseStaxiPlatformErrors();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaxiTenantScope();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<ThongBaoHub>("/hubs/thong-bao");

            endpoints.MapGet("/api/pham-vi", (ITenantScopeAccessor accessor)
                => Results.Ok(new { phamVi = accessor.Required("endpoint").Prefix })).RequireAuthorization();

            endpoints.MapGet("/api/loai-xe", async (LoaiXeRepository repo, CancellationToken ct)
                => Results.Ok(await repo.DanhSachAsync(ct))).RequireAuthorization();

            endpoints.MapGet("/api/loai-xe/quen-loc", async (LoaiXeRepository repo, CancellationToken ct)
                => Results.Ok(await repo.DanhSachQuenLocAsync(ct))).RequireAuthorization();

            endpoints.MapPost("/api/loai-xe/theo-cong-ty", async (YeuCauTuChonCongTy yeuCau, LoaiXeRepository repo, CancellationToken ct)
                => Results.Ok(await repo.DanhSachVoiCompanyIdTuChonAsync(yeuCau.CongTy, ct))).RequireAuthorization();

            endpoints.MapPut("/api/cache/{muc}", GhiVaoCacheAsync).RequireAuthorization();
            endpoints.MapGet("/api/cache/{muc}", DocCacheAsync).RequireAuthorization();
            endpoints.MapPost("/api/thong-bao", PhatTinAsync).RequireAuthorization();
        });
    }

    private static async Task<IResult> GhiVaoCacheAsync(
        string muc, GhiCache noiDung, ITenantCache cache, ITenantScopeAccessor accessor, CancellationToken ct)
    {
        var khoa = CacheKey.For(accessor.Required("cache"), "danh-muc", muc);
        await cache.SetAsync(khoa, noiDung.GiaTri, TimeSpan.FromMinutes(5), ct);

        return Results.Ok(new { khoa = khoa.GiaTri });
    }

    private static async Task<IResult> DocCacheAsync(
        string muc, ITenantCache cache, ITenantScopeAccessor accessor, CancellationToken ct)
    {
        var khoa = CacheKey.For(accessor.Required("cache"), "danh-muc", muc);
        var giaTri = await cache.GetAsync<string>(khoa, ct);

        return giaTri is null ? Results.NotFound() : Results.Ok(new { giaTri, khoa = khoa.GiaTri });
    }

    private static async Task<IResult> PhatTinAsync(
        PhatTin phatTin, ITenantNotifier notifier, ITenantScopeAccessor accessor, CancellationToken ct)
    {
        await notifier.SendAsync(accessor.Required("thong-bao"), phatTin.ChuDe, "NhanTin", phatTin.NoiDung, ct);

        return Results.Accepted();
    }

    public HttpClient TaoClient(string tenantCode, int companyId, string? khoaKy = null)
    {
        var client = Server.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HaiHangGia.CapToken(tenantCode, companyId, khoaKy));

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        HaiHang.Dispose();
    }
}

public sealed record YeuCauTuChonCongTy(int CongTy);

public sealed record GhiCache(string GiaTri);

public sealed record PhatTin(string ChuDe, string NoiDung);
