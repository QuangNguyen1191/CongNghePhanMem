using BLL;

namespace WebApi.HaTang;

// Yeu cau 1 tren web: moi response co header cho biet request do chay bao nhieu cau SQL
// va cau cuoi cung chay o file:dong nao trong DAL. Web staff hien o goc man hinh khi bat "che do do an".
public class DemCauSql
{
    public const string TenHeaderSoCau = "X-So-Cau-Sql";
    public const string TenHeaderViTri = "X-Sql-Vi-Tri";
    private readonly RequestDelegate _next;

    public DemCauSql(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        using var dem = HeThongBLL.BatDauDemSql();
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers[TenHeaderSoCau] = dem.SoCau.ToString();
            if (dem.CauCuoi is { } c) ctx.Response.Headers[TenHeaderViTri] = $"{c.File}:{c.Dong} {c.HamGoi}";
            return Task.CompletedTask;
        });
        await _next(ctx);
    }
}
