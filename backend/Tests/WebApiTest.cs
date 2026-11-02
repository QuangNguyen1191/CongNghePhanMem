using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

public class WebApiTest : TestCoSo, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _app;

    public WebApiTest(WebApplicationFactory<Program> app)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__QLBanThoiTrang", MoiTruong.ChuoiKetNoi);
        _app = app;
    }

    private async Task<HttpClient> DangNhap(string noi, string ten)
    {
        var c = _app.CreateClient();
        var r = await c.PostAsJsonAsync($"/api/tai-khoan/dang-nhap/{noi}", new { tenDangNhap = ten, matKhau = "123456" });
        r.EnsureSuccessStatusCode();
        var p = await r.Content.ReadFromJsonAsync<DTO.PhienDangNhapDTO>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", p!.Token);
        return c;
    }

    [Fact]
    public async Task SaiNoiDangNhap_Tra400_CoThongBao()
    {
        var r = await _app.CreateClient().PostAsJsonAsync("/api/tai-khoan/dang-nhap/web-staff",
            new { tenDangNhap = "khach01", matKhau = "123456" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("thongBao", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Khach_KhongVaoDuocApiNhanVien()
    {
        var c = await DangNhap("web-ban-hang", "khach01");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/hoa-don")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/hoa-don/cua-toi")).StatusCode);
        // xem hoa don cua nguoi khac -> 404
        var cuaNguoiKhac = new BLL.HoaDonBLL().LayDanhSach().First(h => h.MaKh != null && h.MaKh != "KH001" && h.TenKhachHang != null);
        var khach01 = new BLL.TaiKhoanBLL().DangNhap("khach01", "123456", DTO.NoiDangNhap.WebBanHang).MaKh;
        if (cuaNguoiKhac.MaKh != khach01)
            Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/hoa-don/{cuaNguoiKhac.MaHoaDon}")).StatusCode);
    }

    [Fact]
    public async Task NhanVien_MainSub_CoHeaderSoCauSql()
    {
        var c = await DangNhap("web-staff", "quanly");
        var ds = await c.GetFromJsonAsync<List<DTO.HoaDonDTO>>("/api/hoa-don");
        Assert.NotEmpty(ds!);
        var r = await c.GetAsync($"/api/hoa-don/{ds![0].MaHoaDon}/chi-tiet");
        Assert.Equal("1", r.Headers.GetValues("X-So-Cau-Sql").Single());
        Assert.StartsWith("HoaDonDAL.cs:", r.Headers.GetValues("X-Sql-Vi-Tri").Single());
    }

    [Fact]
    public async Task SanPham_KhongCanDangNhap_ThongKeChiAdmin()
    {
        var anDanh = _app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anDanh.GetAsync("/api/san-pham?chiDangBan=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anDanh.GetAsync("/api/thong-ke/nhan-vien")).StatusCode);
        var nv = await DangNhap("web-staff", "thungan1");
        Assert.Equal(HttpStatusCode.Forbidden, (await nv.GetAsync("/api/thong-ke/nhan-vien")).StatusCode);
        var admin = await DangNhap("app-admin", "giamdoc");
        var pdf = await admin.GetAsync("/api/thong-ke/thu-chi?kieu=QUY&nam=2025&dinhDang=pdf");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Swagger_SinhDuocTaiLieu()
    {
        var r = await _app.CreateClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("/api/hoa-don/{ma}/chi-tiet", await r.Content.ReadAsStringAsync());
    }
}
