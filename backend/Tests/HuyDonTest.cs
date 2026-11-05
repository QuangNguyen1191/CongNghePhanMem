using BLL;
using DTO;

namespace Tests;

// v3: khach huy don / xin huy don da xac nhan, nhan vien duyet - tu choi, hoan tien don da tra truoc
public class HuyDonTest : TestCoSo
{
    private static (string maKh, string maNv) KhachVaNv()
    {
        var kh = DangNhap("khach04", NoiDangNhap.WebBanHang);
        var nv = DangNhap("banhangonline", NoiDangNhap.WebStaff);
        return (kh.MaKh!, nv.MaNv!);
    }

    private static DonDatHangDTO DatDon(string maKh, string maSp, string pt = "COD")
    {
        var gio = new GioHangBLL();
        gio.XoaHet(maKh);
        gio.Them(maKh, maSp, 1);
        return new DonDatHangBLL().DatHang(maKh, new DatHangDTO { DiaChiGiao = "12 Lý Thường Kiệt, Hà Nội", SdtNhan = "0911111111", PhuongThucTt = pt });
    }

    [Fact]
    public void ChoXacNhan_HuyNgay_GhiLyDoVaNguoiHuy()
    {
        var (maKh, _) = KhachVaNv();
        var bll = new DonDatHangBLL();
        var don = DatDon(maKh, SanPhamConHang(5, 3).MaSp);
        Assert.Throws<LoiNghiepVu>(() => bll.YeuCauHuy(maKh, don.MaDon, "  "));        // bat buoc ly do
        var sau = bll.YeuCauHuy(maKh, don.MaDon, "Đặt nhầm size");
        Assert.Equal(("DA_HUY", "KHACH", "Đặt nhầm size", (string?)null), (sau.TrangThai, sau.NguoiHuy, sau.LyDoHuy, sau.YeuCauHuy));
        Assert.NotNull(sau.NgayHuy);
    }

    [Fact]
    public void DaXacNhan_GuiYeuCau_ChanGiao_DuyetThiTraKhoHoanTien_KhongTinhBom()
    {
        var (maKh, maNv) = KhachVaNv();
        var bll = new DonDatHangBLL();
        var sp = SanPhamConHang(5, 4);
        var don = DatDon(maKh, sp.MaSp, "CHUYEN_KHOAN");
        var hd = bll.XacNhan(don.MaDon, maNv);
        Assert.Equal("DA_THANH_TOAN", hd.TrangThai);
        var tonSauXacNhan = Ton(sp.MaSp);
        var bomTruoc = new DonDatHangDAL_().Bom(maKh);

        var yc = bll.YeuCauHuy(maKh, don.MaDon, "Đổi ý, không mua nữa");
        Assert.Equal(("DA_XAC_NHAN", "CHO_DUYET"), (yc.TrangThai, yc.YeuCauHuy));
        Assert.Contains("đang xem xét", Assert.Throws<LoiNghiepVu>(() => bll.YeuCauHuy(maKh, don.MaDon, "x")).Message);
        Assert.Contains("xin hủy", Assert.Throws<LoiNghiepVu>(() => bll.GiaoHang(don.MaDon, maNv)).Message);
        Assert.Contains(bll.LayDanhSach(yeuCauHuy: "CHO_DUYET"), d => d.MaDon == don.MaDon);

        bll.DuyetHuy(don.MaDon, maNv);
        var sau = bll.LayTheoMa(don.MaDon);
        Assert.Equal(("DA_HUY", "DA_DUYET", "KHACH"), (sau.TrangThai, sau.YeuCauHuy, sau.NguoiHuy));
        Assert.Equal(tonSauXacNhan + 1, Ton(sp.MaSp));
        Assert.Equal("DA_HUY", new HoaDonBLL().LayTheoMa(hd.MaHoaDon).TrangThai);
        Assert.Equal(("CHO_HOAN", hd.TongTien), (sau.TrangThaiHoanTien, sau.SoTienHoan));
        Assert.Equal(bomTruoc, new DonDatHangDAL_().Bom(maKh));                       // khach bao truoc: khong tinh bom hang

        // nhan vien xac nhan da chuyen tien lai
        var ht = new HoanTienBLL();
        var phieu = ht.LayDanhSach("CHO_HOAN").Single(h => h.MaDon == don.MaDon);
        Assert.Equal("DA_HOAN", ht.DaHoan(phieu.MaHoanTien, maNv, "Đã chuyển khoản lại").TrangThai);
        Assert.Throws<LoiNghiepVu>(() => ht.DaHoan(phieu.MaHoanTien, maNv, null));
        Assert.Throws<LoiNghiepVu>(() => bll.DuyetHuy(don.MaDon, maNv));
    }

    [Fact]
    public void TuChoiYeuCau_GiaoBinhThuong_KhongXinLaiDuoc_DangGiaoKhongHuyDuoc()
    {
        var (maKh, maNv) = KhachVaNv();
        var bll = new DonDatHangBLL();
        var don = DatDon(maKh, SanPhamConHang(5, 5).MaSp);
        bll.XacNhan(don.MaDon, maNv);
        bll.YeuCauHuy(maKh, don.MaDon, "Muốn đổi địa chỉ");
        Assert.Throws<LoiNghiepVu>(() => bll.TuChoiHuy(don.MaDon, maNv, ""));          // phai co ly do
        bll.TuChoiHuy(don.MaDon, maNv, "Hàng đã giao cho đơn vị vận chuyển");
        var d = bll.LayTheoMa(don.MaDon);
        Assert.Equal(("DA_XAC_NHAN", "TU_CHOI", "Hàng đã giao cho đơn vị vận chuyển"), (d.TrangThai, d.YeuCauHuy, d.PhanHoiHuy));
        Assert.Contains("từ chối", Assert.Throws<LoiNghiepVu>(() => bll.YeuCauHuy(maKh, don.MaDon, "xin lần nữa")).Message);

        bll.GiaoHang(don.MaDon, maNv);
        Assert.Contains("đang được giao", Assert.Throws<LoiNghiepVu>(() => bll.YeuCauHuy(maKh, don.MaDon, "x")).Message);
        bll.NhanVienHuy(don.MaDon, maNv, "Khách không nhận hàng");                       // bom hang: COD chua tra -> khong hoan
        d = bll.LayTheoMa(don.MaDon);
        Assert.Equal(("DA_HUY", "NHAN_VIEN", (string?)null), (d.TrangThai, d.NguoiHuy, d.TrangThaiHoanTien));
    }

    [Fact]
    public void KhachKhacKhongXinHuyDuoc()
    {
        var (maKh, _) = KhachVaNv();
        var don = DatDon(maKh, SanPhamConHang(5, 6).MaSp);
        var khac = DangNhap("khach05", NoiDangNhap.WebBanHang).MaKh!;
        Assert.Throws<LoiNghiepVu>(() => new DonDatHangBLL().YeuCauHuy(khac, don.MaDon, "x"));
        new DonDatHangBLL().KhachHuy(maKh, don.MaDon);
    }
}

// doc so lan bom hang qua DAL (RuiRo cua BLL can SDT)
file class DonDatHangDAL_
{
    public int Bom(string maKh) => new DAL.DonDatHangDAL().DemLichSu(maKh).SoLanBomHang;
}
