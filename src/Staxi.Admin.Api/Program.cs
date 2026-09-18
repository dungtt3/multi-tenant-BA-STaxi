using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Staxi.Admin.Api;
using Staxi.Admin.Application;
using Staxi.Admin.Infrastructure;
using Staxi.Contracts;
using Staxi.Platform.Authorization;
using Staxi.Platform.Data;
using Staxi.Platform.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseStaxiServiceProviderValidation();

builder.Services.AddStaxiPlatform();
builder.Services.AddSingleton<ITenantRegistryStore>(_ => SoDangKyHangCauHinh.Doc(builder.Configuration));
builder.Services.AddScoped<ILoaiXeRepository, LoaiXeRepository>();
builder.Services.AddScoped<LayDanhSachLoaiXe>();
builder.Services.AddSingleton<IPermissionCatalog, QuyenDanhMuc>();
builder.Services.AddAuthorization();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var khoaKy = builder.Configuration["Jwt:KhoaKy"]
                     ?? throw new InvalidOperationException(
                         "Thiếu khoá ký JWT. Đặt biến môi trường Jwt__KhoaKy — khoá không được nằm trong mã nguồn hay git (AD-22).");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(khoaKy)),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };
    });

var app = builder.Build();

app.UseStaxiPlatformErrors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaxiTenantScope();

app.MapGet("/api/danh-muc/loai-xe", async (LayDanhSachLoaiXe useCase, CancellationToken ct) =>
{
    IReadOnlyList<LoaiXeDto> ketQua = await useCase.ThucThiAsync(ct);

    return Results.Ok(ketQua);
}).RequireAuthorization().RequirePermission(QuyenDanhMuc.XemLoaiXe);

await app.RunAsync();
