using BLL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace WebApi.HaTang;

// Loi nghiep vu -> 400 / 404 kem thong bao tieng Viet: { "thongBao": "..." }
public class BoLocLoi : IExceptionFilter
{
    private readonly ILogger<BoLocLoi> _log;
    public BoLocLoi(ILogger<BoLocLoi> log) => _log = log;

    public void OnException(ExceptionContext ctx)
    {
        (int ma, string thongBao) = ctx.Exception switch
        {
            KhongTimThay e => (404, e.Message),
            LoiNghiepVu e => (400, e.Message),
            // Loi do trigger / CHECK trong CSDL nem ra (THROW 50xxx) cung la loi nghiep vu
            SqlException { Number: >= 50000 } e => (400, e.Message),
            SqlException { Number: 547 } => (400, "Dữ liệu đang được dùng ở nơi khác hoặc vi phạm ràng buộc."),
            SqlException { Number: 2627 or 2601 } => (400, "Dữ liệu bị trùng."),
            _ => (500, "Lỗi hệ thống, vui lòng thử lại.")
        };
        if (ma == 500) _log.LogError(ctx.Exception, "Loi chua xu ly");
        ctx.Result = new ObjectResult(new { thongBao }) { StatusCode = ma };
        ctx.ExceptionHandled = true;
    }

    // Body / tham so sai kieu, thieu truong bat buoc: 400 { thongBao, loi: { truong: [..] } } thay cho ProblemDetails mac dinh
    public static IActionResult LoiDuLieuGuiLen(ActionContext ctx)
    {
        var loi = ctx.ModelState.Where(m => m.Value?.Errors.Count > 0)
            .ToDictionary(m => m.Key, m => m.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
        var cacTruong = string.Join(", ", loi.Keys.Where(k => k != "" && !k.StartsWith('$')).Select(k => k.TrimStart('$', '.')));
        var thongBao = cacTruong.Length > 0 ? $"Dữ liệu gửi lên không hợp lệ: {cacTruong}" : "Dữ liệu gửi lên không hợp lệ";
        return new BadRequestObjectResult(new { thongBao, loi });
    }
}
