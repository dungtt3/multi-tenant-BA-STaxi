using Staxi.ArchitectureTests.Assertions;
using Staxi.ArchitectureTests.Scanning;
using Staxi.Platform.Caching;
using Staxi.Platform.Tenancy;
using Xunit;

namespace Staxi.ArchitectureTests.Tests;

public sealed class KhoiTaoCacheTests
{
    [Fact(DisplayName = "R8: CacheKey chỉ tạo qua phạm vi hoặc mục dùng chung đã đăng ký (AD-6)")]
    public void KhoiTaoCacheKey_PhaiCoPhamViHoacMucDungChung()
    {
        var factories = PublicApiScanner.Factories(typeof(CacheKey)).ToArray();
        var sharedFactories = factories.Where(method => method.Name == nameof(CacheKey.Shared)).ToArray();
        var violations = PublicApiScanner.Constructors(typeof(CacheKey)).Select(PublicApiScanner.Location)
            .Concat(factories.Where(method => method.Name != nameof(CacheKey.Shared)
                && !method.GetParameters().Any(parameter => parameter.ParameterType == typeof(TenantScope)))
                .Select(PublicApiScanner.Location))
            .Concat(sharedFactories.Where(method => sharedFactories.Length != 1
                || method.GetParameters().FirstOrDefault()?.ParameterType != typeof(SharedReferenceItem))
                .Select(PublicApiScanner.Location));

        ArchitectureAssert.NoViolations("R8", "AD-6", "CacheKey không có constructor công khai; factory cần TenantScope, ngoại lệ duy nhất Shared nhận SharedReferenceItem đầu tiên.", violations);
    }

    [Fact(DisplayName = "R9: SharedReferenceItem không có constructor công khai (AD-6)")]
    public void KhoiTaoSharedReferenceItem_KhongCoConstructorCongKhai()
    {
        ArchitectureAssert.NoViolations("R9", "AD-6", "Mục dùng chung chỉ được lấy qua SharedReferenceData.",
            PublicApiScanner.Constructors(typeof(SharedReferenceItem)).Select(PublicApiScanner.Location));
    }

    [Fact(DisplayName = "R10: Sổ dùng chung khai đủ nguồn thẩm quyền, lý do và tên khớp khoá (AD-6)")]
    public void MucTrongSoDungChung_PhaiDuNguonLyDoVaKhopTen()
    {
        var violations = SharedReferenceData.Catalog
            .Where(entry => entry.Value is null
                || string.IsNullOrWhiteSpace(entry.Value.NguonThamQuyen)
                || string.IsNullOrWhiteSpace(entry.Value.LyDo)
                || !string.Equals(entry.Key, entry.Value.Ten, StringComparison.Ordinal))
            .Select(entry => $"{typeof(SharedReferenceData).FullName}.get_Catalog (khoá '{entry.Key}': thiếu nguồn thẩm quyền, lý do hoặc tên không khớp)");

        ArchitectureAssert.NoViolations("R10", "AD-6", "Mỗi mục phải khai NguonThamQuyen, LyDo không rỗng và Ten khớp khoá của sổ.", violations);
    }
}
