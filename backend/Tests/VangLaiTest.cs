using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BLL;
using DTO;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

// v3: khach vang lai mua hang khong can tai khoan (xac thuc SDT bang OTP) + uu dai thanh vien moi
public class VangLaiTest : TestCoSo, IDisposable
{
    public VangLaiTest() => XacThucBLL.HienMaDemo = true;
    public void Dispose() => XacThucBLL.BayGio = () => DateTime.Now;

    private static string SdtMoi() => "09" + Random.Shared.Next(10_000_000, 99_999_999);

    // Gui ma + nhap dung ma -> SDT da xac thuc
    private static PhienVangLaiDTO XacThuc(string sdt, string mucDich = "MUA_HANG")
    {
        var ma = new XacThucBLL().GuiOtp(new GuiOtpDTO { Sdt = sdt, MucDich = mucDich }, null).MaDemo!;
        return new XacThucBLL().XacThuc(new XacThucOtpDTO { Sdt = sdt, Ma = ma });
    }

    private static DatHangVangLaiDTO Don(string maSp, int soLuong = 1, string pt = "COD") => new()
    {
        HoTen = "Khách Thử Nghiệm", DiaChiGiao = "1 Nguyễn Huệ, Quận 1, TP Hồ Chí Minh", PhuongThucTt = pt,
        DongHangs = { new DongHangDTO { MaSp = maSp, SoLuong = soLuong } }
    };

    [Fact]
    public void Otp_SaiMa_HetLuot_DungLai_HetHan()
    {
        var sdt = SdtMoi();
        var bll = new XacThucBLL();
        var kq = bll.GuiOtp(new GuiOtpDTO { Sdt = sdt }, null);
        Assert.Equal(6, kq.MaDemo!.Length);
        var sai = kq.MaDemo == "000000" ? "111111" : "000000";

        var loi = Assert.Throws<LoiNghiepVu>(() => bll.XacThuc(new XacThucOtpDTO { Sdt = sdt, Ma = sai }));
        Assert.Contains("còn 4 lần", loi.Message);
        // gui lai ngay -> phai doi 60 giay
        Assert.Throws<LoiNghiepVu>(() => bll.GuiOtp(new GuiOtpDTO { Sdt = sdt }, null));

        Assert.Equal(sdt, bll.XacThuc(new XacThucOtpDTO { Sdt = sdt, Ma = kq.MaDemo }).Sdt);
        // ma chi dung 1 lan
        Assert.Throws<LoiNghiepVu>(() => bll.XacThuc(new XacThucOtpDTO { Sdt = sdt, Ma = kq.MaDemo }));

        // ma moi, 6 phut sau moi nhap -> het han
        XacThucBLL.BayGio = () => DateTime.Now.AddMinutes(2);
        var ma2 = bll.GuiOtp(new GuiOtpDTO { Sdt = sdt }, null).MaDemo!;
        XacThucBLL.BayGio = () => DateTime.Now.AddMinutes(8);
        Assert.Contains("hết hạn", Assert.Throws<LoiNghiepVu>(() => bll.XacThuc(new XacThucOtpDTO { Sdt = sdt, Ma = ma2 })).Message);
    }

    [Fact]
    public void Otp_Sai5Lan_PhaiGuiMaMoi()
    {
        var sdt = SdtMoi();
        var bll = new XacThucBLL();
        var ma = bll.GuiOtp(new GuiOtpDTO { Sdt = sdt }, null).MaDemo!;
        var sai = ma == "000000" ? "111111" : "000000";
        for (var i = 0; i < 5; i++) Assert.Throws<LoiNghiepVu>(() => bll.XacThuc(new XacThucOtpDTO { Sdt = sdt, Ma = sai }));
        // nhap dung cung khong duoc nua
        Assert.Contains("sai 5 lần", Assert.Throws<LoiNghiepVu>(() => bll.XacThuc(new XacThucOtpDTO { Sdt = sdt, Ma = ma })).Message);
    }

    [Theory]
    [InlineData("0123456789")]   // khong phai dau so di dong
    [InlineData("091234567")]    // thieu so
    public void Otp_SdtKhongHopLe(string sdt) =>
        Assert.Throws<LoiNghiepVu>(() => new XacThucBLL().GuiOtp(new GuiOtpDTO { Sdt = sdt }, null));

    [Fact]
    public void Otp_ChuanHoaSdt_84()
    {
        var sdt = SdtMoi();
        var kq = new XacThucBLL().GuiOtp(new GuiOtpDTO { Sdt = "+84 " + sdt[1..4] + "." + sdt[4..] }, null);
        Assert.Equal(sdt, kq.Sdt);
    }

    [Fact]
    public void DatHangVangLai_TaoKhachTheoSdt_GomDon_GioiHanDonCho()
    {
        var sdt = SdtMoi();
        XacThuc(sdt);
        var sp = SanPhamConHang(5, 5);
        var tonTruoc = Ton(sp.MaSp);
        var bll = new DonDatHangBLL();

        var d1 = bll.DatHangVangLai(sdt, Don(sp.MaSp));
        Assert.Equal(("CHO_XAC_NHAN", "VANG_LAI", sdt), (d1.TrangThai, d1.Nguon, d1.SdtNhan));
        Assert.Equal(tonTruoc, Ton(sp.MaSp));                       // chua tru kho
        var kh = new KhachHangBLL().TimTheoSdt(sdt)!;
        Assert.Null(kh.MaTk);
        Assert.True(kh.SdtDaXacThuc);

        var d2 = bll.DatHangVangLai(sdt, Don(sp.MaSp, 2, "CHUYEN_KHOAN"));
        Assert.Equal(kh.MaKh, d2.MaKh);                              // cung SDT -> cung khach
        Assert.Equal(2, bll.LayCuaSdt(sdt).Count);
        // da co 2 don cho xac nhan -> khong cho dat them
        Assert.Contains("chờ cửa hàng xác nhận", Assert.Throws<LoiNghiepVu>(() => bll.DatHangVangLai(sdt, Don(sp.MaSp))).Message);

        // huy 1 don thi dat lai duoc; khong xem / huy duoc don cua SDT khac
        bll.VangLaiHuy(sdt, d1.MaDon);
        Assert.Equal("DA_HUY", bll.LayTheoMa(d1.MaDon).TrangThai);
        Assert.Throws<KhongTimThay>(() => bll.LayCuaSdt(SdtMoi(), d2.MaDon));
        bll.DatHangVangLai(sdt, Don(sp.MaSp));
    }

    [Fact]
    public void DatHangVangLai_GioiHanSoLuong_VaCod()
    {
        var sdt = SdtMoi();
        XacThuc(sdt);
        var bll = new DonDatHangBLL();
        var sp = SanPhamConHang(10, 6);
        Assert.Contains("tối đa 5 cái", Assert.Throws<LoiNghiepVu>(() => bll.DatHangVangLai(sdt, Don(sp.MaSp, 6))).Message);

        // tong > 5 trieu: COD bi chan, chuyen khoan thi duoc
        var dat = new SanPhamBLL().LayTatCa(true).Where(s => s.SoLuong >= 5).OrderByDescending(s => s.DonGia).Take(5).ToList();
        var yc = Don(dat[0].MaSp);
        yc.DongHangs = dat.Select(s => new DongHangDTO { MaSp = s.MaSp, SoLuong = 5 }).ToList();
        Assert.Contains("COD", Assert.Throws<LoiNghiepVu>(() => bll.DatHangVangLai(sdt, yc)).Message);
        yc.PhuongThucTt = "ONLINE";
        Assert.True(bll.DatHangVangLai(sdt, yc).TongTien > QuyTac.VangLaiCodToiDa);
    }

    [Fact]
    public void SdtBomHang_KhongDuocCod_RuiRoCao()
    {
        // du lieu mau: KH022 (0987000022) co 2 don da xac nhan nhung bi huy
        var rr = new KhachHangBLL().RuiRo("0987000022");
        Assert.Equal(("CAO", false), (rr.MucRuiRo, rr.DuocDatCod));
        Assert.Equal(2, rr.SoLanBomHang);

        // dat COD bi chan (kiem truoc so don cho de ra dung thong bao: dung SDT moi co 2 lan bom)
        var sdt = SdtMoi();
        XacThuc(sdt);
        var sp = SanPhamConHang(5, 7);
        var nv = DangNhap("banhangonline", NoiDangNhap.WebStaff).MaNv!;
        var bll = new DonDatHangBLL();
        for (var i = 0; i < 2; i++)
        {
            var d = bll.DatHangVangLai(sdt, Don(sp.MaSp));
            bll.XacNhan(d.MaDon, nv);
            bll.GiaoHang(d.MaDon, nv);
            bll.NhanVienHuy(d.MaDon, nv);                 // khach khong nhan hang
        }
        Assert.Contains("không nhận hàng", Assert.Throws<LoiNghiepVu>(() => bll.DatHangVangLai(sdt, Don(sp.MaSp))).Message);
        Assert.Equal("CHUYEN_KHOAN", bll.DatHangVangLai(sdt, Don(sp.MaSp, 1, "CHUYEN_KHOAN")).PhuongThucTt);
        Assert.Equal("CAO", new KhachHangBLL().RuiRo(sdt).MucRuiRo);
    }

    [Fact]
    public void DangKy_PhaiXacThucSdt_GanDonCu_UuDaiThanhVienMoi()
    {
        var sdt = SdtMoi();
        var tk = new TaiKhoanBLL();
        var ten = "tv" + sdt[2..];
        var kh = new KhachHangDTO { HoTen = "Thành Viên Mới", Sdt = sdt };
        // chua xac thuc / xac thuc SDT khac -> khong cho dang ky
        Assert.Throws<LoiNghiepVu>(() => tk.DangKy(ten, "123456", kh, null));
        Assert.Throws<LoiNghiepVu>(() => tk.DangKy(ten, "123456", kh, SdtMoi()));

        // mua vang lai 1 don truoc
        XacThuc(sdt);
        var sp = SanPhamConHang(5, 8);
        var donCu = new DonDatHangBLL().DatHangVangLai(sdt, Don(sp.MaSp));

        // dang ky nhanh: chi ten dang nhap + mat khau, ho so lay tu khach cu
        var p = tk.DangKy(ten, "123456", new KhachHangDTO { Sdt = sdt }, sdt);
        Assert.Equal(donCu.MaKh, p.MaKh);
        Assert.Equal(1, p.SoDonDaGan);
        Assert.Equal("Khách Thử Nghiệm", p.HoTen);
        Assert.Contains(new DonDatHangBLL().LayCuaKhach(p.MaKh!), d => d.MaDon == donCu.MaDon);
        // SDT da co tai khoan -> khong dang ky lan nua
        Assert.Throws<LoiNghiepVu>(() => tk.DangKy(ten + "x", "123456", new KhachHangDTO { Sdt = sdt }, sdt));

        // uu dai thanh vien moi: 10% toi da 100.000d cho 3 don dau dat bang tai khoan (don vang lai cu khong tinh)
        var km = new KhuyenMaiBLL();
        var ud = km.LayUuDaiThanhVienMoi(p.MaKh)!;
        Assert.Equal((3, 0, 3), (ud.SoDonDau, ud.DaDung!.Value, ud.ConLai!.Value));

        var re = new SanPhamBLL().LayTatCa(true).Where(s => s.SoLuong >= 3 && s.DonGia < 500_000).OrderBy(s => s.DonGia).First();
        var bang = km.TinhTien(new() { new DongHangDTO { MaSp = re.MaSp, SoLuong = 1 } }, DateTime.Today, null, maKhThanhVien: p.MaKh);
        Assert.Equal("THANH_VIEN_MOI", bang.LoaiGiam);
        Assert.Equal(Math.Round(bang.TamTinh * 0.1m, 0), bang.TienGiam);

        var gio = new GioHangBLL();
        var donBll = new DonDatHangBLL();
        for (var i = 1; i <= 3; i++)
        {
            gio.Them(p.MaKh!, re.MaSp, 1);
            var d = donBll.DatHang(p.MaKh!, new DatHangDTO { DiaChiGiao = "1 Lê Lợi, Quận 1, TP Hồ Chí Minh", SdtNhan = sdt });
            Assert.Equal(("TAI_KHOAN", "KM06"), (d.Nguon, d.MaKm));
            Assert.True(d.TienGiam > 0);
            if (i == 1) donBll.KhachHuy(p.MaKh!, d.MaDon);     // huy thi duoc tra lai luot
        }
        Assert.Equal(1, km.LayUuDaiThanhVienMoi(p.MaKh)!.ConLai);
        gio.Them(p.MaKh!, re.MaSp, 1);
        donBll.DatHang(p.MaKh!, new DatHangDTO { DiaChiGiao = "1 Lê Lợi, Quận 1, TP Hồ Chí Minh", SdtNhan = sdt });
        Assert.Equal(0, km.LayUuDaiThanhVienMoi(p.MaKh)!.ConLai);
        // het luot -> khong con giam
        Assert.NotEqual("THANH_VIEN_MOI",
            km.TinhTien(new() { new DongHangDTO { MaSp = re.MaSp, SoLuong = 1 } }, DateTime.Today, null, maKhThanhVien: p.MaKh).LoaiGiam);
    }

    [Fact]
    public void UuDaiThanhVienMoi_GiamToiDa100k_VaThangVoucherNhoHon()
    {
        var sdt = SdtMoi();
        XacThuc(sdt, "DANG_KY");
        var p = new TaiKhoanBLL().DangKy("tv" + sdt[2..], "123456", new KhachHangDTO { HoTen = "Khách Đơn Lớn", Sdt = sdt }, sdt);
        Assert.Equal(0, p.SoDonDaGan);
        var dat = new SanPhamBLL().LayTatCa(true).Where(s => s.SoLuong >= 2).OrderByDescending(s => s.DonGia).First();
        var dong = new List<DongHangDTO> { new() { MaSp = dat.MaSp, SoLuong = 2 } };   // > 1 trieu
        var bang = new KhuyenMaiBLL().TinhTien(dong, DateTime.Today, "FREESHIP5", maKhThanhVien: p.MaKh);
        Assert.Equal(("THANH_VIEN_MOI", 100_000m, null), (bang.LoaiGiam, bang.TienGiam, bang.MaVoucher));
        Assert.Contains("FREESHIP5", bang.GhiChuKm);
        // khach chua dang nhap: van dung voucher nhu cu
        Assert.Equal("VOUCHER", new KhuyenMaiBLL().TinhTien(dong, DateTime.Today, "FREESHIP5").LoaiGiam);
    }
}

public class VangLaiApiTest : TestCoSo, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _app;

    public VangLaiApiTest(WebApplicationFactory<Program> app)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__QLBanThoiTrang", MoiTruong.ChuoiKetNoi);
        Environment.SetEnvironmentVariable("Otp__HienMaDemo", "true");
        _app = app;
    }

    [Fact]
    public async Task Otp_DatHang_TraCuu_TokenChiDungChoVangLai()
    {
        var c = _app.CreateClient();
        var sdt = "09" + Random.Shared.Next(10_000_000, 99_999_999);
        var gui = await (await c.PostAsJsonAsync("/api/vang-lai/otp", new { sdt })).Content.ReadFromJsonAsync<KetQuaGuiOtpDTO>();
        var r = await c.PostAsJsonAsync("/api/vang-lai/xac-thuc", new { sdt, ma = gui!.MaDemo });
        r.EnsureSuccessStatusCode();
        var phien = await r.Content.ReadFromJsonAsync<PhienVangLaiDTO>();

        // chua co token -> 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/vang-lai/don-hang")).StatusCode);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", phien!.Token);

        var sp = SanPhamConHang(5, 9);
        var dat = await c.PostAsJsonAsync("/api/vang-lai/don-hang", new
        {
            hoTen = "Khách API", diaChiGiao = "5 Lý Tự Trọng, Quận 1, TP Hồ Chí Minh", phuongThucTt = "COD",
            dongHangs = new[] { new { maSp = sp.MaSp, soLuong = 1 } }
        });
        dat.EnsureSuccessStatusCode();
        var don = await dat.Content.ReadFromJsonAsync<DonDatHangDTO>();
        Assert.Equal("VANG_LAI", don!.Nguon);
        Assert.Single((await c.GetFromJsonAsync<List<DonDatHangDTO>>("/api/vang-lai/don-hang"))!);
        Assert.Equal("application/pdf", (await c.GetAsync($"/api/vang-lai/don-hang/{don.MaDon}/pdf")).Content.Headers.ContentType!.MediaType);

        // token vang lai khong dung duoc o API khac (vd xem don theo ma, gio hang)
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync($"/api/don-hang/{don.MaDon}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/gio-hang")).StatusCode);

        // tao tai khoan nhanh bang phien vang lai
        var dk = await _app.CreateClient().PostAsJsonAsync("/api/tai-khoan/dang-ky", new
        {
            tenDangNhap = "api" + sdt[2..], matKhau = "123456", khachHang = new { sdt }, phienXacThuc = phien.Token
        });
        dk.EnsureSuccessStatusCode();
        Assert.Equal(1, (await dk.Content.ReadFromJsonAsync<PhienDangNhapDTO>())!.SoDonDaGan);
    }

    [Fact]
    public async Task DangKy_KhongCoPhienXacThuc_Tra400()
    {
        var r = await _app.CreateClient().PostAsJsonAsync("/api/tai-khoan/dang-ky", new
        {
            tenDangNhap = "khongotp" + Random.Shared.Next(1000, 9999), matKhau = "123456",
            khachHang = new { hoTen = "A B", sdt = "0912345678" }, phienXacThuc = "token-gia"
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("OTP", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NhanVien_LocDonVangLai_XemRuiRo()
    {
        var c = _app.CreateClient();
        var p = await (await c.PostAsJsonAsync("/api/tai-khoan/dang-nhap/web-staff", new { tenDangNhap = "banhangonline", matKhau = "123456" }))
            .Content.ReadFromJsonAsync<PhienDangNhapDTO>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", p!.Token);
        var ds = await c.GetFromJsonAsync<List<DonDatHangDTO>>("/api/don-hang?nguon=VANG_LAI");
        Assert.NotEmpty(ds!);
        Assert.All(ds!, d => Assert.Equal("VANG_LAI", d.Nguon));
        var rr = await c.GetFromJsonAsync<RuiRoKhachDTO>("/api/khach-hang/rui-ro/0987000021");
        Assert.Equal(("THAP", 1), (rr!.MucRuiRo, rr.SoDonHoanTat));
    }
}
