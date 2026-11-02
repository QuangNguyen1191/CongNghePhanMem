using BLL;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

[Route("api/loai-sp")]
public class LoaiSpController : CoSoController
{
    private readonly LoaiSpBLL _bll = new();

    [HttpGet] public List<LoaiSpDTO> LayTatCa() => _bll.LayTatCa();

    [Authorize(Roles = VaiTro.Admin), HttpPost]
    public LoaiSpDTO Them(LoaiSpDTO l) => _bll.Them(l.TenLoaiSp);

    [Authorize(Roles = VaiTro.Admin), HttpPut("{ma}")]
    public IActionResult Sua(string ma, LoaiSpDTO l) { l.MaLoaiSp = ma; _bll.Sua(l); return NoContent(); }

    [Authorize(Roles = VaiTro.Admin), HttpDelete("{ma}")]
    public IActionResult Xoa(string ma) { _bll.Xoa(ma); return NoContent(); }
}

[Route("api/san-pham")]
public class SanPhamController : CoSoController
{
    private readonly SanPhamBLL _bll = new();

    // Web ban hang: chiDangBan=true. Yeu cau 2 - "truy van moi": goi lai voi tuKhoa
    [HttpGet]
    public List<SanPhamDTO> LayDanhSach(string? tuKhoa, string? maLoaiSp, bool chiDangBan = false) =>
        string.IsNullOrWhiteSpace(tuKhoa) && string.IsNullOrWhiteSpace(maLoaiSp)
            ? _bll.LayTatCa(chiDangBan) : _bll.TimKiem(tuKhoa ?? "", maLoaiSp, chiDangBan);

    [HttpGet("{ma}")] public SanPhamDTO LayTheoMa(string ma) => _bll.LayTheoMa(ma);

    // Cac size / mau cua cung san pham (trang chi tiet chon size)
    [HttpGet("{ma}/bien-the")] public List<SanPhamDTO> LayBienThe(string ma) => _bll.LayCacBienThe(ma);

    [HttpGet("gia-khuyen-mai")] public Dictionary<string, decimal> GiaKhuyenMai() => _bll.GiaKhuyenMaiHomNay();

    [Authorize(Roles = VaiTro.NhanVienHoacAdmin), HttpGet("ton-thap")]
    public List<SanPhamDTO> TonThap() => _bll.LayTonThap();

    [Authorize(Roles = VaiTro.Admin), HttpPost]
    public SanPhamDTO Them(SanPhamDTO s) => _bll.Them(s);

    [Authorize(Roles = VaiTro.Admin), HttpPost("them-nhieu")]
    public List<SanPhamDTO> ThemNhieu(List<SanPhamDTO> ds) => _bll.ThemNhieu(ds);

    [Authorize(Roles = VaiTro.Admin), HttpPut("{ma}")]
    public IActionResult Sua(string ma, SanPhamDTO s) { s.MaSp = ma; _bll.Sua(s); return NoContent(); }

    [Authorize(Roles = VaiTro.Admin), HttpPost("{ma}/ngung-ban")]
    public IActionResult NgungBan(string ma, bool ngung = true) { _bll.NgungBan(ma, ngung); return NoContent(); }
}

[Route("api/nha-cung-cap"), Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class NhaCungCapController : CoSoController
{
    private readonly NhaCungCapBLL _bll = new();

    [HttpGet] public List<NhaCungCapDTO> LayTatCa() => _bll.LayTatCa();

    [Authorize(Roles = VaiTro.Admin), HttpPost]
    public NhaCungCapDTO Them(NhaCungCapDTO n) => _bll.Them(n);

    [Authorize(Roles = VaiTro.Admin), HttpPut("{ma}")]
    public IActionResult Sua(string ma, NhaCungCapDTO n) { n.MaNcc = ma; _bll.Sua(n); return NoContent(); }

    [Authorize(Roles = VaiTro.Admin), HttpDelete("{ma}")]
    public IActionResult Xoa(string ma) { _bll.Xoa(ma); return NoContent(); }
}
