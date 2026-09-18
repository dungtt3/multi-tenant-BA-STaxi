using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Staxi.Auth.Application;

namespace Staxi.Auth.Infrastructure;

public sealed class MatKhauKeThuaMd5 : IKiemMatKhau
{
    public string BamGia { get; } = Bam(Guid.NewGuid().ToString("n"));

    public bool Khop(string matKhauNhap, string bamLuuTru)
    {
        ArgumentNullException.ThrowIfNull(matKhauNhap);
        ArgumentNullException.ThrowIfNull(bamLuuTru);

        var bamNhap = Encoding.ASCII.GetBytes(Bam(matKhauNhap));
        var bamLuu = Encoding.ASCII.GetBytes(bamLuuTru.Trim().ToUpperInvariant());

        return CryptographicOperations.FixedTimeEquals(bamNhap, bamLuu);
    }

    [SuppressMessage(
        "Security",
        "CA5351:Do Not Use Broken Cryptographic Algorithms",
        Justification =
            "MD5 khong salt la dinh dang mat khau san co cua BA.STaxi.Web tren 18 database dung chung. " +
            "AD-10 cam doi nghia gia tri cot dang chay, va web cu van ghi cot Password nay, nen WEB2 BUOC " +
            "phai doi chieu bang MD5 de hai he dung chung duoc. Day la no ky thuat da ghi trong " +
            "docs/.architecture-memlog.md va phai duoc va o CA HAI he, khong phai chi o WEB2.")]
    private static string Bam(string chuoi)
        => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(chuoi)));
}
