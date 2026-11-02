using BLL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

[Route("api/phieu-nhap"), Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class PhieuNhapController : CoSoController
{
    private readonly PhieuNhapBLL _bll = new();
    private readonly PdfBLL _pdf = new();

    public record LapPhieuNhapYeuCau(string MaNcc, List<ChiTietPhieuNhapDTO> ChiTiets);

    [HttpPost] public PhieuNhapDTO Lap(LapPhieuNhapYeuCau yc) => _bll.Lap(yc.MaNcc, MaNv, yc.ChiTiets ?? new());

    [HttpGet]
    public List<PhieuNhapDTO> LayDanhSach(DateTime? tuNgay, DateTime? denNgay, string? maNcc) => _bll.LayDanhSach(tuNgay, denNgay, maNcc);

    [HttpGet("{ma}")] public PhieuNhapDTO LayTheoMa(string ma) => _bll.LayTheoMa(ma);
    [HttpGet("{ma}/chi-tiet")] public List<ChiTietPhieuNhapDTO> LayChiTiet(string ma) => _bll.LayChiTiet(ma);
    [HttpGet("{ma}/pdf")] public FileContentResult InPdf(string ma) => Pdf(_pdf.PhieuNhap(_bll.LayTheoMa(ma)), ma);
}

[Route("api/doi-tra"), Authorize]
public class DoiTraController : CoSoController
{
    private readonly DoiTraBLL _bll = new();
    private readonly PdfBLL _pdf = new();

    public record YeuCauDoiTra(string MaHoaDon, string Loai, string? LyDo, List<ChiTietDoiTraDTO> ChiTiets);

    // Khach gui tren web (hoa don cua minh), hoac nhan vien lap tai quay
    [HttpPost]
    public DoiTraDTO GuiYeuCau(YeuCauDoiTra yc) =>
        _bll.GuiYeuCau(yc.MaHoaDon, yc.Loai, yc.LyDo, yc.ChiTiets ?? new(), LaKhach ? MaKh : null);

    [Authorize(Roles = VaiTro.KhachHang), HttpGet("cua-toi")]
    public List<DoiTraDTO> CuaToi() => _bll.LayDanhSach(null, MaKh);

    [HttpGet("{ma}")]
    public DoiTraDTO LayTheoMa(string ma)
    {
        var d = _bll.LayTheoMa(ma);
        ChiCuaMinh(d.MaKh);
        return d;
    }

    [HttpGet("{ma}/pdf")] public FileContentResult InPdf(string ma) => Pdf(_pdf.DoiTra(LayTheoMa(ma)), ma);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet]
    public List<DoiTraDTO> LayDanhSach(string? trangThai) => _bll.LayDanhSach(trangThai);

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/duyet")]
    public IActionResult Duyet(string ma) { _bll.Duyet(ma, MaNv); return NoContent(); }

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/tu-choi")]
    public IActionResult TuChoi(string ma) { _bll.TuChoi(ma, MaNv); return NoContent(); }

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpPost("{ma}/hoan-tat")]
    public DoiTraDTO HoanTat(string ma) => _bll.HoanTat(ma, MaNv);
}
