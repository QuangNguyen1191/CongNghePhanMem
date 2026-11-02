using BLL;
using Microsoft.AspNetCore.Mvc;
using WebApi.HaTang;

namespace WebApi.Controllers;

// Controller chi nhan request, lay nguoi dung tu token, goi BLL va tra ket qua. Khong co SQL, khong co quy tac nghiep vu.
[ApiController]
[Produces("application/json")]
public abstract class CoSoController : ControllerBase
{
    protected string MaTk => User.FindFirst(TenClaim.MaTk)?.Value ?? throw new LoiNghiepVu("Chưa đăng nhập");
    protected string MaNv => User.FindFirst(TenClaim.MaNv)?.Value ?? throw new LoiNghiepVu("Chỉ nhân viên mới làm được thao tác này");
    protected string MaKh => User.FindFirst(TenClaim.MaKh)?.Value ?? throw new LoiNghiepVu("Chỉ khách hàng mới làm được thao tác này");
    protected bool LaKhach => User.IsInRole(VaiTro.KhachHang);

    // Khach chi duoc xem chung tu cua chinh minh
    protected void ChiCuaMinh(string? maKhCuaChungTu)
    {
        if (LaKhach && maKhCuaChungTu != MaKh) throw new KhongTimThay("Không tìm thấy");
    }

    protected FileContentResult Excel(byte[] noiDung, string ten) =>
        File(noiDung, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{ten}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

    protected FileContentResult Pdf(byte[] noiDung, string ten) => File(noiDung, "application/pdf", $"{ten}.pdf");
}
