using BLL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

[Route("api/khuyen-mai")]
public class KhuyenMaiController : CoSoController
{
    private readonly KhuyenMaiBLL _bll = new();

    [HttpGet("dang-chay")] public List<KhuyenMaiDTO> DangChay() => _bll.LayDangChay();

    // Xem truoc tien phai tra (gio hang / ban tai quay) truoc khi chot
    [HttpPost("tinh-tien")]
    public BangTinhTienDTO TinhTien(BanTaiQuayDTO yc) => _bll.TinhTien(yc.DongHangs, DateTime.Today, yc.MaVoucher);

    [Authorize(Roles = VaiTro.Admin), HttpGet] public List<KhuyenMaiDTO> LayTatCa() => _bll.LayTatCa();
    [Authorize(Roles = VaiTro.Admin), HttpGet("{ma}")] public KhuyenMaiDTO LayTheoMa(string ma) => _bll.LayTheoMa(ma);
    [Authorize(Roles = VaiTro.Admin), HttpPost] public KhuyenMaiDTO Them(KhuyenMaiDTO k) => _bll.Them(k);

    [Authorize(Roles = VaiTro.Admin), HttpPut("{ma}")]
    public IActionResult Sua(string ma, KhuyenMaiDTO k) { k.MaKm = ma; _bll.Sua(k); return NoContent(); }

    [Authorize(Roles = VaiTro.Admin), HttpPost("{ma}/ket-thuc")]
    public IActionResult KetThuc(string ma) { _bll.KetThuc(ma); return NoContent(); }
}

[Route("api/gio-hang"), Authorize(Roles = VaiTro.KhachHang)]
public class GioHangController : CoSoController
{
    private readonly GioHangBLL _bll = new();

    [HttpGet] public GioHangDTO Xem() => _bll.Xem(MaKh);
    [HttpPost] public GioHangDTO Them(DongHangDTO d) => _bll.Them(MaKh, d.MaSp, d.SoLuong);
    [HttpPut] public GioHangDTO CapNhat(DongHangDTO d) => _bll.CapNhat(MaKh, d.MaSp, d.SoLuong);
    [HttpDelete("{maSp}")] public GioHangDTO XoaDong(string maSp) => _bll.XoaDong(MaKh, maSp);
    [HttpDelete] public IActionResult XoaHet() { _bll.XoaHet(MaKh); return NoContent(); }

    // Khach chon hang khi chua dang nhap (luu o trinh duyet), dang nhap xong gop vao gio
    [HttpPost("gop")] public GioHangDTO Gop(List<DongHangDTO> gioTam) => _bll.GopGioTam(MaKh, gioTam);
}

[Route("api/don-hang"), Authorize]
public class DonHangController : CoSoController
{
    private readonly DonDatHangBLL _bll = new();
    private readonly PdfBLL _pdf = new();

    // ---- khach
    [Authorize(Roles = VaiTro.KhachHang), HttpPost]
    public DonDatHangDTO DatHang(DatHangDTO yc) => _bll.DatHang(MaKh, yc);

    [Authorize(Roles = VaiTro.KhachHang), HttpGet("cua-toi")]
    public List<DonDatHangDTO> CuaToi() => _bll.LayCuaKhach(MaKh);

    [Authorize(Roles = VaiTro.KhachHang), HttpPost("{ma}/khach-huy")]
    public IActionResult KhachHuy(string ma) { _bll.KhachHuy(MaKh, ma); return NoContent(); }

    // ---- ca hai (khach chi xem don cua minh)
    [HttpGet("{ma}")]
    public DonDatHangDTO LayTheoMa(string ma)
    {
        var d = _bll.LayTheoMa(ma);
        ChiCuaMinh(d.MaKh);
        return d;
    }

    [HttpGet("{ma}/pdf")]
    public FileContentResult InPdf(string ma) => Pdf(_pdf.DonDatHang(LayTheoMa(ma)), ma);

    // ---- nhan vien xu ly don
    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet]
    public List<DonDatHangDTO> LayDanhSach(string? trangThai, DateTime? tuNgay, DateTime? denNgay) =>
        _bll.LayDanhSach(trangThai, tuNgay, denNgay);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/xac-nhan")]
    public HoaDonDTO XacNhan(string ma) => _bll.XacNhan(ma, MaNv);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/tu-choi")]
    public IActionResult TuChoi(string ma) { _bll.TuChoi(ma, MaNv); return NoContent(); }

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/giao-hang")]
    public IActionResult GiaoHang(string ma) { _bll.GiaoHang(ma, MaNv); return NoContent(); }

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/hoan-tat")]
    public IActionResult HoanTat(string ma) { _bll.HoanTat(ma, MaNv); return NoContent(); }

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/huy")]
    public IActionResult Huy(string ma) { _bll.NhanVienHuy(ma, MaNv); return NoContent(); }
}

[Route("api/hoa-don"), Authorize]
public class HoaDonController : CoSoController
{
    private readonly HoaDonBLL _bll = new();
    private readonly PdfBLL _pdf = new();

    [Authorize(Roles = VaiTro.KhachHang), HttpGet("cua-toi")]
    public List<HoaDonDTO> CuaToi() => _bll.LayCuaKhach(MaKh);

    [HttpGet("{ma}")]
    public HoaDonDTO LayTheoMa(string ma)
    {
        var h = _bll.LayTheoMa(ma);
        ChiCuaMinh(h.MaKh);
        return h;
    }

    [HttpGet("{ma}/pdf")]
    public FileContentResult InPdf(string ma) => Pdf(_pdf.HoaDon(LayTheoMa(ma)), ma);

    // ---- nhan vien: ban tai quay
    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("tinh-truoc")]
    public BangTinhTienDTO TinhTruoc(BanTaiQuayDTO yc) => _bll.TinhTruoc(yc);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("ban-tai-quay")]
    public HoaDonDTO BanTaiQuay(BanTaiQuayDTO yc) => _bll.BanTaiQuay(yc, MaNv);

    // Yeu cau 4 (main-sub): danh sach ben trai...
    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet]
    public List<HoaDonDTO> LayDanhSach(DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? maNv, string? maKh) =>
        _bll.LayDanhSach(tuNgay, denNgay, trangThai, maNv, maKh);

    // ...bam 1 dong thi chi goi cau nay de lay chi tiet ben phai
    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet("{ma}/chi-tiet")]
    public List<ChiTietHoaDonDTO> LayChiTiet(string ma) => _bll.LayChiTiet(ma);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/huy")]
    public IActionResult Huy(string ma) { _bll.Huy(ma); return NoContent(); }
}
