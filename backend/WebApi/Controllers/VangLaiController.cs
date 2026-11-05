using BLL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

// v3: Khach vang lai - mua hang va tra cuu don khong can tai khoan.
//   1. POST otp       : gui ma 6 so toi SDT (gia lap: in ra console, che do demo tra luon ma)
//   2. POST xac-thuc  : nhap dung ma -> nhan token phien VANG_LAI (30 phut)
//   3. Cac API con lai gui token do trong header Authorization: Bearer ...
[Route("api/vang-lai")]
public class VangLaiController : CoSoController
{
    private readonly XacThucBLL _xacThuc = new();
    private readonly DonDatHangBLL _don = new();
    private readonly PdfBLL _pdf = new();
    private readonly CauHinhJwt _jwt;

    public VangLaiController(CauHinhJwt jwt) => _jwt = jwt;

    [HttpPost("otp")]
    public KetQuaGuiOtpDTO GuiOtp(GuiOtpDTO yc) => _xacThuc.GuiOtp(yc, HttpContext.Connection.RemoteIpAddress?.ToString());

    [HttpPost("xac-thuc")]
    public PhienVangLaiDTO XacThuc(XacThucOtpDTO yc) => KemToken(_xacThuc.XacThuc(yc));

    // Lay lai thong tin phien (dien san form) khi tai lai trang
    [Authorize(Roles = VaiTro.VangLai), HttpGet("phien")]
    public PhienVangLaiDTO Phien() => _xacThuc.ThongTinPhien(SdtVangLai);

    [Authorize(Roles = VaiTro.VangLai), HttpPost("don-hang")]
    public DonDatHangDTO DatHang(DatHangVangLaiDTO yc) => _don.DatHangVangLai(SdtVangLai, yc);

    [Authorize(Roles = VaiTro.VangLai), HttpGet("don-hang")]
    public List<DonDatHangDTO> DonCuaToi() => _don.LayCuaSdt(SdtVangLai);

    [Authorize(Roles = VaiTro.VangLai), HttpGet("don-hang/{ma}")]
    public DonDatHangDTO LayDon(string ma) => _don.LayCuaSdt(SdtVangLai, ma);

    [Authorize(Roles = VaiTro.VangLai), HttpPost("don-hang/{ma}/huy")]
    public IActionResult Huy(string ma) { _don.VangLaiHuy(SdtVangLai, ma); return NoContent(); }

    [Authorize(Roles = VaiTro.VangLai), HttpPost("don-hang/{ma}/yeu-cau-huy")]
    public DonDatHangDTO YeuCauHuy(string ma, LyDoDTO yc) => _don.VangLaiYeuCauHuy(SdtVangLai, ma, yc.LyDo);

    [Authorize(Roles = VaiTro.VangLai), HttpGet("don-hang/{ma}/pdf")]
    public FileContentResult Pdf(string ma) => Pdf(_pdf.DonDatHang(_don.LayCuaSdt(SdtVangLai, ma)), ma);

    private PhienVangLaiDTO KemToken(PhienVangLaiDTO p)
    {
        p.Token = TaoToken.TaoVangLai(p, _jwt);
        return p;
    }
}
