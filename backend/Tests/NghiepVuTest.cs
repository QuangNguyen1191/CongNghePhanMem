using BLL;
using DTO;

namespace Tests;

public class DangNhapTest : TestCoSo
{
    [Theory]
    [InlineData("khach01", NoiDangNhap.WebBanHang, true)]
    [InlineData("khach01", NoiDangNhap.WebStaff, false)]
    [InlineData("khach01", NoiDangNhap.AppAdmin, false)]
    [InlineData("thungan1", NoiDangNhap.WebStaff, true)]
    [InlineData("thungan1", NoiDangNhap.WebBanHang, false)]
    [InlineData("thungan1", NoiDangNhap.AppAdmin, false)]
    [InlineData("giamdoc", NoiDangNhap.AppAdmin, true)]
    [InlineData("giamdoc", NoiDangNhap.WebStaff, true)]
    [InlineData("khach15", NoiDangNhap.WebBanHang, false)]   // tai khoan bi khoa
    public void DangNhap_DungNoi(string ten, NoiDangNhap noi, bool duocVao)
    {
        if (duocVao)
        {
            var p = DangNhap(ten, noi);
            Assert.Equal(ten, p.TenDangNhap);
            Assert.True(p.MaNv != null || p.MaKh != null);
        }
        else Assert.Throws<LoiNghiepVu>(() => DangNhap(ten, noi));
    }

    [Fact]
    public void DangNhap_SaiMatKhau() =>
        Assert.Throws<LoiNghiepVu>(() => new TaiKhoanBLL().DangNhap("khach01", "sai-mat-khau", NoiDangNhap.WebBanHang));
}

public class BanHangTest : TestCoSo
{
    [Fact]
    public void BanTaiQuay_TruTon_RoiHuy_TraLaiTon()
    {
        var sp = SanPhamConHang();
        var tonTruoc = Ton(sp.MaSp);
        var nv = DangNhap("thungan1", NoiDangNhap.WebStaff);
        var hd = new HoaDonBLL().BanTaiQuay(new BanTaiQuayDTO
        {
            DongHangs = { new DongHangDTO { MaSp = sp.MaSp, SoLuong = 2 } }, PhuongThucTt = "TIEN_MAT"
        }, nv.MaNv!);

        Assert.Equal("DA_THANH_TOAN", hd.TrangThai);
        Assert.Equal(tonTruoc - 2, Ton(sp.MaSp));
        Assert.Equal(hd.ChiTiets.Sum(c => c.ThanhTien) - hd.TienGiam, hd.TongTien);

        new HoaDonBLL().Huy(hd.MaHoaDon);
        Assert.Equal(tonTruoc, Ton(sp.MaSp));
        Assert.Equal("DA_HUY", new HoaDonBLL().LayTheoMa(hd.MaHoaDon).TrangThai);
    }

    [Fact]
    public void BanTaiQuay_VuotTon_BaoLoi_KhongDoiTon()
    {
        var sp = SanPhamConHang(1);
        var tonTruoc = Ton(sp.MaSp);
        var nv = DangNhap("thungan1", NoiDangNhap.WebStaff);
        Assert.Throws<LoiNghiepVu>(() => new HoaDonBLL().BanTaiQuay(new BanTaiQuayDTO
        {
            DongHangs = { new DongHangDTO { MaSp = sp.MaSp, SoLuong = tonTruoc + 1 } }
        }, nv.MaNv!));
        Assert.Equal(tonTruoc, Ton(sp.MaSp));
    }

    [Fact]
    public void DatHang_XacNhan_Giao_HoanTat()
    {
        var kh = DangNhap("khach02", NoiDangNhap.WebBanHang);
        var nv = DangNhap("banhangonline", NoiDangNhap.WebStaff);
        var sp = SanPhamConHang(5, 1);
        var tonTruoc = Ton(sp.MaSp);
        var gio = new GioHangBLL();
        gio.XoaHet(kh.MaKh!);
        gio.Them(kh.MaKh!, sp.MaSp, 3);

        var don = new DonDatHangBLL().DatHang(kh.MaKh!, new DatHangDTO
        {
            DiaChiGiao = "12 Lê Lợi, Quận 1, TP Hồ Chí Minh", SdtNhan = "0900000000", PhuongThucTt = "COD"
        });
        Assert.Equal("CHO_XAC_NHAN", don.TrangThai);
        Assert.Equal(tonTruoc, Ton(sp.MaSp));                    // chua tru ton khi moi dat
        Assert.Empty(gio.Xem(kh.MaKh!).ChiTiets);

        var hd = new DonDatHangBLL().XacNhan(don.MaDon, nv.MaNv!);
        Assert.Equal(tonTruoc - 3, Ton(sp.MaSp));
        Assert.Equal("CHO_THANH_TOAN", hd.TrangThai);           // COD: thu tien khi giao xong
        Assert.Equal(don.TongTien, hd.TongTien);

        new DonDatHangBLL().GiaoHang(don.MaDon, nv.MaNv!);
        new DonDatHangBLL().HoanTat(don.MaDon, nv.MaNv!);
        Assert.Equal("HOAN_TAT", new DonDatHangBLL().LayTheoMa(don.MaDon).TrangThai);
        Assert.Equal("DA_THANH_TOAN", new HoaDonBLL().LayTheoMa(hd.MaHoaDon).TrangThai);
    }

    [Fact]
    public void KhachKhongHuyDuocDonDaXacNhan()
    {
        var kh = DangNhap("khach03", NoiDangNhap.WebBanHang);
        var nv = DangNhap("banhangonline", NoiDangNhap.WebStaff);
        var sp = SanPhamConHang(5, 2);
        var gio = new GioHangBLL();
        gio.XoaHet(kh.MaKh!);
        gio.Them(kh.MaKh!, sp.MaSp, 1);
        var don = new DonDatHangBLL().DatHang(kh.MaKh!, new DatHangDTO { DiaChiGiao = "Hà Nội", SdtNhan = "0911111111" });
        new DonDatHangBLL().XacNhan(don.MaDon, nv.MaNv!);
        Assert.Throws<LoiNghiepVu>(() => new DonDatHangBLL().KhachHuy(kh.MaKh!, don.MaDon));
        new DonDatHangBLL().NhanVienHuy(don.MaDon, nv.MaNv!);   // nhan vien huy: tra ton
    }
}

public class KhoTest : TestCoSo
{
    [Fact]
    public void PhieuNhap_CongTon()
    {
        var sp = SanPhamConHang(0, 3);
        var tonTruoc = Ton(sp.MaSp);
        var nv = DangNhap("thukho", NoiDangNhap.WebStaff);
        var ncc = new NhaCungCapBLL().LayTatCa().First(n => n.TrangThai == "HOP_TAC");
        var p = new PhieuNhapBLL().Lap(ncc.MaNcc, nv.MaNv!, new List<ChiTietPhieuNhapDTO>
        {
            new() { MaSp = sp.MaSp, SoLuong = 10, DonGiaNhap = 100_000 }
        });
        Assert.Equal(1_000_000, p.TongTien);
        Assert.Equal(tonTruoc + 10, Ton(sp.MaSp));
    }

    [Fact]
    public void TraHang_HoanTien_CongLaiTon()
    {
        var sp = SanPhamConHang(5, 4);
        var nv = DangNhap("thungan2", NoiDangNhap.WebStaff);
        var hd = new HoaDonBLL().BanTaiQuay(new BanTaiQuayDTO
        {
            DongHangs = { new DongHangDTO { MaSp = sp.MaSp, SoLuong = 2 } }
        }, nv.MaNv!);
        var tonSauBan = Ton(sp.MaSp);

        var bll = new DoiTraBLL();
        var dt = bll.GuiYeuCau(hd.MaHoaDon, "TRA", "Không vừa", new List<ChiTietDoiTraDTO> { new() { MaSp = sp.MaSp, SoLuong = 1 } }, null);
        Assert.Equal("CHO_DUYET", dt.TrangThai);
        // khong tra qua so luong da mua
        Assert.Throws<LoiNghiepVu>(() => bll.GuiYeuCau(hd.MaHoaDon, "TRA", null,
            new List<ChiTietDoiTraDTO> { new() { MaSp = sp.MaSp, SoLuong = 2 } }, null));

        bll.Duyet(dt.MaDoiTra, nv.MaNv!);
        var xong = bll.HoanTat(dt.MaDoiTra, nv.MaNv!);
        Assert.Equal("HOAN_TAT", xong.TrangThai);
        Assert.Equal(Math.Round(hd.TongTien / 2, 0), xong.TienHoan);
        Assert.Equal(tonSauBan + 1, Ton(sp.MaSp));
        // da co doi tra thi khong huy hoa don duoc nua
        Assert.Throws<LoiNghiepVu>(() => new HoaDonBLL().Huy(hd.MaHoaDon));
    }
}
