using BLL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

[Route("api/tai-khoan")]
public class TaiKhoanController : CoSoController
{
    private readonly TaiKhoanBLL _bll = new();
    private readonly CauHinhJwt _jwt;

    public TaiKhoanController(CauHinhJwt jwt) => _jwt = jwt;

    public record DangNhapYeuCau(string TenDangNhap, string MatKhau);
    public record DangKyYeuCau(string TenDangNhap, string MatKhau, KhachHangDTO KhachHang);
    public record DoiMatKhauYeuCau(string MatKhauCu, string MatKhauMoi);

    // Moi noi 1 duong dan rieng: khach chi vao duoc web-ban-hang, nhan vien web-staff, admin app-admin (+ web-staff)
    [HttpPost("dang-nhap/web-ban-hang")]
    public PhienDangNhapDTO DangNhapWebBanHang(DangNhapYeuCau yc) => DangNhap(yc, NoiDangNhap.WebBanHang);

    [HttpPost("dang-nhap/web-staff")]
    public PhienDangNhapDTO DangNhapWebStaff(DangNhapYeuCau yc) => DangNhap(yc, NoiDangNhap.WebStaff);

    [HttpPost("dang-nhap/app-admin")]
    public PhienDangNhapDTO DangNhapAppAdmin(DangNhapYeuCau yc) => DangNhap(yc, NoiDangNhap.AppAdmin);

    private PhienDangNhapDTO DangNhap(DangNhapYeuCau yc, NoiDangNhap noi)
    {
        var p = _bll.DangNhap(yc.TenDangNhap ?? "", yc.MatKhau ?? "", noi);
        p.Token = TaoToken.Tao(p, _jwt);
        return p;
    }

    [HttpPost("dang-ky")]
    public PhienDangNhapDTO DangKy(DangKyYeuCau yc)
    {
        var p = _bll.DangKy(yc.TenDangNhap ?? "", yc.MatKhau ?? "", yc.KhachHang ?? new KhachHangDTO());
        p.Token = TaoToken.Tao(p, _jwt);
        return p;
    }

    [Authorize, HttpPost("doi-mat-khau")]
    public IActionResult DoiMatKhau(DoiMatKhauYeuCau yc)
    {
        _bll.DoiMatKhau(MaTk, yc.MatKhauCu ?? "", yc.MatKhauMoi ?? "");
        return Ok(new { thongBao = "Đã đổi mật khẩu" });
    }

    [Authorize(Roles = VaiTro.Admin), HttpGet]
    public List<TaiKhoanDTO> LayTatCa() => _bll.LayTatCa();

    [Authorize(Roles = VaiTro.Admin), HttpPost("{ma}/khoa")]
    public IActionResult Khoa(string ma) { _bll.KhoaMo(ma, true); return NoContent(); }

    [Authorize(Roles = VaiTro.Admin), HttpPost("{ma}/mo")]
    public IActionResult Mo(string ma) { _bll.KhoaMo(ma, false); return NoContent(); }

    [Authorize(Roles = VaiTro.Admin), HttpPost("{ma}/dat-lai-mat-khau")]
    public object DatLaiMatKhau(string ma) => new { matKhauTam = _bll.DatLaiMatKhau(ma) };
}
