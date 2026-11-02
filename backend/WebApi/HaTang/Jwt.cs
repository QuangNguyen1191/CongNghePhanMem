using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DTO;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.HaTang;

public record CauHinhJwt(string Key, string Issuer, int SoGio);

public static class TenClaim
{
    public const string MaTk = "ma_tk";
    public const string TenDangNhap = "ten_dang_nhap";
    public const string VaiTro = "vai_tro";
    public const string MaNv = "ma_nv";
    public const string MaKh = "ma_kh";
    public const string HoTen = "ho_ten";
}

public static class VaiTro
{
    public const string Admin = "ADMIN";
    public const string NhanVien = "NHAN_VIEN";
    public const string KhachHang = "KHACH_HANG";
    public const string NhanVienHoacAdmin = "NHAN_VIEN,ADMIN";
}

public static class TaoToken
{
    public static string Tao(PhienDangNhapDTO p, CauHinhJwt cfg)
    {
        var claims = new List<Claim>
        {
            new(TenClaim.MaTk, p.MaTk), new(TenClaim.TenDangNhap, p.TenDangNhap),
            new(TenClaim.VaiTro, p.VaiTro), new(TenClaim.HoTen, p.HoTen)
        };
        if (p.MaNv != null) claims.Add(new(TenClaim.MaNv, p.MaNv));
        if (p.MaKh != null) claims.Add(new(TenClaim.MaKh, p.MaKh));
        var khoa = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(cfg.Issuer, cfg.Issuer, claims, expires: DateTime.UtcNow.AddHours(cfg.SoGio),
            signingCredentials: khoa);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
