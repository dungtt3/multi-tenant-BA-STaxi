namespace Staxi.ArchitectureTests.Registry;

internal static class TenancyRegistryValidator
{
    public static IEnumerable<string> Validate(TenancyRegistry registry)
    {
        foreach (var loi in KiemTraTungMuc(registry))
        {
            yield return loi;
        }

        foreach (var loi in KiemTraTrungTen(registry))
        {
            yield return loi;
        }

        foreach (var loi in KiemTraThongKe(registry))
        {
            yield return loi;
        }
    }

    private static IEnumerable<string> KiemTraTungMuc(TenancyRegistry registry)
    {
        var hangHopLe = registry.HangHopLe.ToHashSet(StringComparer.Ordinal);
        var trangThaiHopLe = registry.TrangThaiHopLe.ToHashSet(StringComparer.Ordinal);

        foreach (var muc in registry.Bang)
        {
            var nhan = string.IsNullOrWhiteSpace(muc.Ten) ? "<mục không có tên bảng>" : muc.Ten;

            if (string.IsNullOrWhiteSpace(muc.Ten))
            {
                yield return $"{nhan}: thiếu 'ten'";
            }

            if (string.IsNullOrWhiteSpace(muc.ThucThe))
            {
                yield return $"{nhan}: thiếu 'thucThe'";
            }

            if (string.IsNullOrWhiteSpace(muc.ChungCu))
            {
                yield return $"{nhan}: thiếu 'chungCu' — mỗi phân hạng phải nói rõ dựa trên cái gì";
            }

            if (!hangHopLe.Contains(muc.Hang))
            {
                yield return $"{nhan}: hạng '{muc.Hang}' không nằm trong hangHopLe ({string.Join(", ", registry.HangHopLe)})";
            }

            if (!trangThaiHopLe.Contains(muc.TrangThai))
            {
                yield return $"{nhan}: trạng thái '{muc.TrangThai}' không nằm trong trangThaiHopLe ({string.Join(", ", registry.TrangThaiHopLe)})";
            }
        }
    }

    private static IEnumerable<string> KiemTraTrungTen(TenancyRegistry registry)
        => registry.Bang
            .GroupBy(muc => muc.Ten, StringComparer.OrdinalIgnoreCase)
            .Where(nhom => nhom.Count() > 1)
            .Select(nhom => $"{nhom.Key}: có {nhom.Count()} mục trùng tên bảng — mỗi bảng chỉ được có đúng một hạng");

    private static IEnumerable<string> KiemTraThongKe(TenancyRegistry registry)
    {
        var thongKe = registry.ThongKe;

        if (thongKe.TongSoBangTrongSo != registry.Bang.Count)
        {
            yield return $"thongKe.tongSoBangTrongSo = {thongKe.TongSoBangTrongSo} nhưng sổ có {registry.Bang.Count} mục";
        }

        var daDuyet = registry.Bang.Count(muc => muc.TrangThai == "daDuyet");
        if (thongKe.DaDuyet != daDuyet)
        {
            yield return $"thongKe.daDuyet = {thongKe.DaDuyet} nhưng đếm thật được {daDuyet}";
        }

        var chuaDuyet = registry.Bang.Count(muc => muc.TrangThai == "chuaDuyet");
        if (thongKe.ChuaDuyet != chuaDuyet)
        {
            yield return $"thongKe.chuaDuyet = {thongKe.ChuaDuyet} nhưng đếm thật được {chuaDuyet}";
        }

        var coCanhBao = registry.Bang.Count(muc => muc.CanhBao.Count > 0);
        if (thongKe.SoMucCoCanhBao != coCanhBao)
        {
            yield return $"thongKe.soMucCoCanhBao = {thongKe.SoMucCoCanhBao} nhưng đếm thật được {coCanhBao}";
        }

        foreach (var hang in registry.HangHopLe)
        {
            var dem = registry.Bang.Count(muc => muc.Hang == hang);
            var khai = thongKe.TheoHang.TryGetValue(hang, out var giaTri) ? giaTri : 0;

            if (khai != dem)
            {
                yield return $"thongKe.theoHang['{hang}'] = {khai} nhưng đếm thật được {dem}";
            }
        }
    }
}
