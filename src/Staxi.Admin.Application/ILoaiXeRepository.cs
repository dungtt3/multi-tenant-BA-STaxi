using Staxi.Admin.Domain;

namespace Staxi.Admin.Application;

public interface ILoaiXeRepository
{
    public Task<IReadOnlyList<LoaiXe>> DanhSachAsync(CancellationToken ct = default);
}
