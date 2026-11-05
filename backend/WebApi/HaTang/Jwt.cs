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
    public const string Sdt = "sdt";            // v3: phien khach vang lai
}

public static class VaiTro
{
    public const string Admin = "ADMIN";
    public const string NhanVien = "NHAN_VIEN";
    public const string KhachHang = "KHACH_HANG";
    public const string NhanVienHoacAdmin = "NHAN_VIEN,ADMIN";
    public const string VangLai = "VANG_LAI";   // v3: khach chua co tai khoan, da xac thuc SDT bang OTP (token 30 phut)
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

    // v3: phien khach vang lai - chi chua SDT da xac thuc, het han sau 30 phut
    public static string TaoVangLai(PhienVangLaiDTO p, CauHinhJwt cfg)
    {
        var claims = new List<Claim> { new(TenClaim.VaiTro, VaiTro.VangLai), new(TenClaim.Sdt, p.Sdt) };
        var khoa = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(cfg.Issuer, cfg.Issuer, claims, expires: p.HetHan.ToUniversalTime(), signingCredentials: khoa);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Doc SDT tu token phien vang lai gui trong body (vd luc dang ky). Token sai / het han -> null.
    public static string? DocSdtVangLai(string? token, CauHinhJwt cfg)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var p = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, ThamSoKiemTra(cfg), out _);
            return p.FindFirst(TenClaim.VaiTro)?.Value == VaiTro.VangLai ? p.FindFirst(TenClaim.Sdt)?.Value : null;
        }
        catch (Exception e) when (e is SecurityTokenException or ArgumentException) { return null; }
    }

    public static TokenValidationParameters ThamSoKiemTra(CauHinhJwt cfg) => new()
    {
        ValidIssuer = cfg.Issuer, ValidAudience = cfg.Issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.Key)),
        RoleClaimType = TenClaim.VaiTro, NameClaimType = TenClaim.TenDangNhap
    };
}
