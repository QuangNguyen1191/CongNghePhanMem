using BLL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

// Yeu cau 3 + 5: cach = TAI_SU_DUNG | TRUY_VAN_MOI. dinhDang = json | excel | pdf
[Route("api/thong-ke"), Authorize(Roles = VaiTro.Admin)]
public class ThongKeController : CoSoController
{
    private readonly ThongKeBLL _bll = new();

    [HttpGet("cac-nam")] public List<int> CacNam() => _bll.CacNamCoDuLieu();

    [HttpPost("lam-moi")] public IActionResult LamMoi() { ThongKeBLL.XoaBoNhoDem(); return NoContent(); }

    [HttpGet("tong-tien")]
    public IActionResult TongTien(DateTime? tuNgay, DateTime? denNgay, string cach = ThongKeBLL.TruyVanMoi, string dinhDang = "json") =>
        TraVe(_bll.TongTienKhoang(tuNgay ?? throw new LoiNghiepVu("Vui lòng chọn ngày bắt đầu (tuNgay)"),
            denNgay ?? throw new LoiNghiepVu("Vui lòng chọn ngày kết thúc (denNgay)"), cach), dinhDang);

    [HttpGet("thu-chi")]
    public IActionResult ThuChi(string kieu = "THANG", int? nam = null, string cach = ThongKeBLL.TruyVanMoi, string dinhDang = "json") =>
        TraVe(_bll.ThuChiLoiNhuan(kieu, nam, cach), dinhDang);

    [HttpGet("nhan-vien")]
    public IActionResult NhanVien(string cach = ThongKeBLL.TruyVanMoi, string dinhDang = "json") =>
        TraVe(_bll.TheoNhanVienNam(cach), dinhDang);

    [HttpGet("khach-hang")]
    public IActionResult KhachHang(int? nam = null, string cach = ThongKeBLL.TruyVanMoi, string dinhDang = "json") =>
        TraVe(_bll.TheoKhachHang(nam, cach), dinhDang);

    [HttpGet("san-pham")]
    public IActionResult SanPham(int? nam, string cach = ThongKeBLL.TruyVanMoi, string dinhDang = "json") =>
        TraVe(_bll.TheoSanPhamQuy(nam ?? throw new LoiNghiepVu("Vui lòng chọn năm (nam)"), cach), dinhDang);

    private IActionResult TraVe(BangThongKeDTO bang, string dinhDang) => dinhDang.ToLower() switch
    {
        "excel" => Excel(new ExcelBLL().XuatBangThongKe(bang), "ThongKe"),
        "pdf" => Pdf(new PdfBLL().BaoCao(bang), $"ThongKe_{DateTime.Now:yyyyMMdd_HHmm}"),
        _ => Ok(bang)
    };
}

// Yeu cau 6: tim nang cao. Body: { dieuKiens: [{truong, toanTu, giaTri}], khopTatCa: true(AND)/false(OR) }
[Route("api/tim-kiem"), Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class TimKiemController : CoSoController
{
    private readonly TimKiemBLL _bll = new();

    [HttpGet("truong")]
    public object CacTruong() => new
    {
        hoaDon = TimKiemBLL.CacTruongHoaDon, khachHang = TimKiemBLL.CacTruongKhachHang,
        sanPham = TimKiemBLL.CacTruongSanPham, toanTu = TimKiemBLL.CacToanTu
    };

    [HttpPost("hoa-don")] public List<HoaDonDTO> HoaDon(TimKiemNangCaoDTO dk) => _bll.TimHoaDon(dk);
    [HttpPost("khach-hang")] public List<KhachHangDTO> KhachHang(TimKiemNangCaoDTO dk) => _bll.TimKhachHang(dk);
    [HttpPost("san-pham")] public List<SanPhamDTO> SanPham(TimKiemNangCaoDTO dk) => _bll.TimSanPham(dk);
}

// Yeu cau 7: xuat / doc Excel
[Route("api/excel"), Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class ExcelController : CoSoController
{
    private readonly ExcelBLL _bll = new();

    [HttpGet("san-pham")] public FileContentResult SanPham() => Excel(_bll.XuatSanPham(new SanPhamBLL().LayTatCa()), "SanPham");
    [HttpGet("khach-hang")] public FileContentResult KhachHang() => Excel(_bll.XuatKhachHang(new KhachHangBLL().LayTatCa()), "KhachHang");
    [HttpGet("nha-cung-cap")] public FileContentResult NhaCungCap() => Excel(_bll.XuatNhaCungCap(new NhaCungCapBLL().LayTatCa()), "NhaCungCap");

    [HttpGet("hoa-don")]
    public FileContentResult HoaDon(DateTime? tuNgay, DateTime? denNgay, string? trangThai) =>
        Excel(_bll.XuatHoaDon(new HoaDonBLL().LayDanhSach(tuNgay, denNgay, trangThai)), "HoaDon");

    [HttpGet("phieu-nhap")]
    public FileContentResult PhieuNhap(DateTime? tuNgay, DateTime? denNgay) =>
        Excel(_bll.XuatPhieuNhap(new PhieuNhapBLL().LayDanhSach(tuNgay, denNgay)), "PhieuNhap");

    [Authorize(Roles = VaiTro.Admin), HttpGet("nhan-vien")]
    public FileContentResult NhanVien() => Excel(_bll.XuatNhanVien(new NhanVienBLL().LayTatCa()), "NhanVien");

    // File mau de nhap
    [HttpGet("mau/{loai}")]
    public FileContentResult Mau(string loai) => loai switch
    {
        "san-pham" => Excel(_bll.MauSanPham(), "Mau_SanPham"),
        "khach-hang" => Excel(_bll.MauKhachHang(), "Mau_KhachHang"),
        "nha-cung-cap" => Excel(_bll.MauNhaCungCap(), "Mau_NhaCungCap"),
        "chi-tiet-phieu-nhap" => Excel(_bll.MauChiTietPhieuNhap(), "Mau_ChiTietPhieuNhap"),
        _ => throw new KhongTimThay("Không có file mẫu này")
    };

    // Buoc 1: tai file len, nhan ve dong hop le + loi de xem truoc (chua ghi CSDL)
    [HttpPost("doc/{loai}")]
    public object Doc(string loai, IFormFile file)
    {
        using var s = file.OpenReadStream();
        return loai switch
        {
            "san-pham" => _bll.DocSanPham(s),
            "khach-hang" => _bll.DocKhachHang(s),
            "nha-cung-cap" => _bll.DocNhaCungCap(s),
            "chi-tiet-phieu-nhap" => _bll.DocChiTietPhieuNhap(s),
            _ => throw new KhongTimThay("Không đọc được loại dữ liệu này")
        };
    }

    // Buoc 2: xac nhan ghi cac dong hop le. Chi tiet phieu nhap thi goi POST /api/phieu-nhap
    [Authorize(Roles = VaiTro.Admin), HttpPost("nhap/san-pham")]
    public List<SanPhamDTO> NhapSanPham(List<SanPhamDTO> ds) => _bll.NhapSanPham(ds);

    [HttpPost("nhap/khach-hang")]
    public object NhapKhachHang(List<KhachHangDTO> ds)
    {
        var (so, lois) = _bll.NhapKhachHang(ds);
        return new { soDongDaThem = so, lois };
    }

    [Authorize(Roles = VaiTro.Admin), HttpPost("nhap/nha-cung-cap")]
    public object NhapNhaCungCap(List<NhaCungCapDTO> ds)
    {
        var (so, lois) = _bll.NhapNhaCungCap(ds);
        return new { soDongDaThem = so, lois };
    }
}

// Yeu cau 1: xem cac cau SQL vua chay, kem file + dong code trong DAL
[Route("api/nhat-ky-sql"), Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class NhatKySqlController : CoSoController
{
    [HttpGet] public List<NhatKySqlDTO> LayGanNhat(int soLuong = 100) => HeThongBLL.NhatKySqlGanNhat(soLuong);

    [Authorize(Roles = VaiTro.Admin), HttpDelete]
    public IActionResult Xoa() { HeThongBLL.XoaNhatKySql(); return NoContent(); }
}
