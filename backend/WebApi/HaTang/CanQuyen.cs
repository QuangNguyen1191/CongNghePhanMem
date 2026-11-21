using BLL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.HaTang;

// Chot chan theo CHUC VU cho API nhan vien: [CanQuyen(NhomQuyen.Kho)] = nhan vien phai co nhom "kho".
// Nhieu nhom = co 1 trong cac nhom la du. ADMIN co tat ca nhom (BLL.NhomQuyen.TheoChucVu).
// Chi ap cho nhan vien / admin: khach hang da bi chan bang [Authorize(Roles)] o API nhan vien, con o API dung chung
// (xem don / hoa don / phieu doi tra theo ma) thi khach di qua ChiCuaMinh() nhu cu.
// Thieu quyen -> 403 { thongBao } (cung kieu loi nghiep vu cua ca API).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class CanQuyenAttribute : Attribute, IAuthorizationFilter
{
    public string[] Nhom { get; }

    public CanQuyenAttribute(params string[] nhom) => Nhom = nhom;

    public void OnAuthorization(AuthorizationFilterContext ctx)
    {
        var u = ctx.HttpContext.User;
        if (!u.IsInRole(VaiTro.NhanVien) && !u.IsInRole(VaiTro.Admin)) return;
        if (Nhom.Any(n => u.HasClaim(TenClaim.NhomQuyen, n))) return;
        var ten = string.Join(" hoặc ", Nhom.Select(n => NhomQuyen.Ten.TryGetValue(n, out var t) ? t : n));
        ctx.Result = new ObjectResult(new { thongBao = $"Chức vụ của bạn không có quyền làm việc này (cần quyền {ten})." })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
