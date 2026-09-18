using System.Security.Cryptography;
using System.Text;
using Staxi.Auth.Application;
using Staxi.Auth.Domain;

namespace Staxi.Auth.Infrastructure;

public sealed class SecurityStampTuDuLieu : ISecurityStampFactory
{
    public string Tao(NguoiDung nguoiDung)
    {
        ArgumentNullException.ThrowIfNull(nguoiDung);

        var nguyenLieu = $"{nguoiDung.Id:n}|{nguoiDung.MatKhauBam}|{nguoiDung.BiKhoa}|{nguoiDung.DaXoa}";

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nguyenLieu)))[..32];
    }
}
