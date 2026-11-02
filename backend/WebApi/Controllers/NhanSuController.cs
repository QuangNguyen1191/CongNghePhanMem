using BLL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

[Route("api/nhan-vien"), Authorize(Roles = VaiTro.Admin)]
public class NhanVienController : CoSoController
{
    private readonly NhanVienBLL _bll = new();

    public record ThemNhanVienYeuCau(NhanVienDTO NhanVien, string TenDangNhap, string MatKhau, string VaiTro);

    [HttpGet] public List<NhanVienDTO> LayTatCa() => _bll.LayTatCa();
    [HttpGet("{ma}")] public NhanVienDTO LayTheoMa(string ma) => _bll.LayTheoMa(ma);

    [HttpPost]
    public NhanVienDTO Them(ThemNhanVienYeuCau yc) => _bll.Them(yc.NhanVien, yc.TenDangNhap, yc.MatKhau, yc.VaiTro);

    [HttpPut("{ma}")]
    public IActionResult Sua(string ma, NhanVienDTO nv) { nv.MaNv = ma; _bll.Sua(nv); return NoContent(); }

    [HttpPost("{ma}/nghi-viec")]
    public IActionResult NghiViec(string ma) { _bll.NghiViec(ma); return NoContent(); }
}

[Route("api/khach-hang"), Authorize]
public class KhachHangController : CoSoController
{
    private readonly KhachHangBLL _bll = new();

    // ---- khach tu xem / sua ho so
    [Authorize(Roles = VaiTro.KhachHang), HttpGet("toi")]
    public KhachHangDTO CuaToi() => _bll.LayTheoMa(MaKh);

    [Authorize(Roles = VaiTro.KhachHang), HttpPut("toi")]
    public IActionResult SuaCuaToi(KhachHangDTO k) { k.MaKh = MaKh; _bll.Sua(k); return NoContent(); }

    // ---- nhan vien
    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet]
    public List<KhachHangDTO> LayTatCa() => _bll.LayTatCa();

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet("{ma}")]
    public KhachHangDTO LayTheoMa(string ma) => _bll.LayTheoMa(ma);

    // Ban tai quay: go SĐT de tim khach
    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet("theo-sdt/{sdt}")]
    public ActionResult<KhachHangDTO> TheoSdt(string sdt) => _bll.TimTheoSdt(sdt) is { } k ? k : NotFound(new { thongBao = "Chưa có khách với SĐT này" });

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost]
    public KhachHangDTO ThemTaiQuay(KhachHangDTO k) => _bll.ThemTaiQuay(k);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPut("{ma}")]
    public IActionResult Sua(string ma, KhachHangDTO k) { k.MaKh = ma; _bll.Sua(k); return NoContent(); }

    [Authorize(Roles = VaiTro.Admin), HttpPut("{ma}/hang-diem")]
    public IActionResult SuaHangDiem(string ma, string hang, int diem) { _bll.SuaHangDiem(ma, hang, diem); return NoContent(); }

    // ---- yeu cau 6: HOP va GIAO
    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet("hai-tinh-hop")]
    public List<KhachHangDTO> HaiTinhHop(string tinhA = "TP Hồ Chí Minh", string tinhB = "Hà Nội") => _bll.TheoHaiTinhHop(tinhA, tinhB);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet("o-tinh-a-giao-tinh-b")]
    public List<KhachHangDTO> OTinhAGiaoTinhB(string tinhA = "TP Hồ Chí Minh", string tinhB = "Hà Nội") => _bll.OTinhACoDonGiaoTinhB(tinhA, tinhB);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet("vua-tai-quay-vua-online")]
    public List<KhachHangDTO> VuaTaiQuayVuaOnline() => _bll.VuaMuaTaiQuayVuaDatOnline();
}
