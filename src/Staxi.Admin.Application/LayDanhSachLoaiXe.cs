using Staxi.Contracts;

namespace Staxi.Admin.Application;

public sealed class LayDanhSachLoaiXe(ILoaiXeRepository repository)
{
    public async Task<IReadOnlyList<LoaiXeDto>> ThucThiAsync(CancellationToken ct = default)
    {
        var loaiXe = await repository.DanhSachAsync(ct).ConfigureAwait(false);

        return [.. loaiXe.Select(x => new LoaiXeDto(x.Id, x.Ten, x.TenTiengAnh, x.SoCho, x.TaiTrong))];
    }
}
