using BLL;
using DTO;

namespace Tests;

public class ThongKeTest : TestCoSo
{
    private readonly ThongKeBLL _bll = new();

    public ThongKeTest() => ThongKeBLL.XoaBoNhoDem();

    private static void GiongNhau(BangThongKeDTO a, BangThongKeDTO b)
    {
        Assert.Equal(a.Cots, b.Cots);
        Assert.Equal(a.Dongs.Select(d => d.Nhan), b.Dongs.Select(d => d.Nhan));
        for (var i = 0; i < a.Dongs.Count; i++) Assert.Equal(a.Dongs[i].GiaTris, b.Dongs[i].GiaTris);
    }

    private void HaiCachKhop(Func<string, BangThongKeDTO> tinh)
    {
        var moi = tinh(ThongKeBLL.TruyVanMoi);
        var taiSuDung = tinh(ThongKeBLL.TaiSuDung);
        Assert.NotEmpty(moi.Dongs);
        GiongNhau(moi, taiSuDung);
        Assert.True(moi.SoCauTruyVan >= 1);
        // lan 2 cach tai su dung khong chay them cau SQL nao
        Assert.Equal(0, tinh(ThongKeBLL.TaiSuDung).SoCauTruyVan);
    }

    [Fact] public void TongTienKhoang_5a() => HaiCachKhop(c => _bll.TongTienKhoang(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), c));
    [Fact] public void ThuChiTheoThang_5b() => HaiCachKhop(c => _bll.ThuChiLoiNhuan("THANG", 2025, c));
    [Fact] public void ThuChiTheoQuy_5b() => HaiCachKhop(c => _bll.ThuChiLoiNhuan("QUY", null, c));
    [Fact] public void ThuChiTheoNam_5b() => HaiCachKhop(c => _bll.ThuChiLoiNhuan("NAM", null, c));
    [Fact] public void NhanVienNam_5c() => HaiCachKhop(_bll.TheoNhanVienNam);
    [Fact] public void KhachHangQuy_5d() => HaiCachKhop(c => _bll.TheoKhachHang(2025, c));
    [Fact] public void KhachHangNam_5d() => HaiCachKhop(c => _bll.TheoKhachHang(null, c));
    [Fact] public void SanPhamQuy_5e() => HaiCachKhop(c => _bll.TheoSanPhamQuy(2025, c));

    [Fact]
    public void DongTongCong_BangTongCacDong()
    {
        var b = _bll.TheoNhanVienNam(ThongKeBLL.TruyVanMoi);
        var tong = b.Dongs.Last();
        Assert.Equal(ThongKeBLL.TongCong, tong.Nhan);
        Assert.Equal(b.Dongs.SkipLast(1).Sum(d => d.GiaTris[^1]), tong.GiaTris[^1]);
        // tong ca bang = tong tat ca hoa don da thanh toan
        var tatCa = new HoaDonBLL().LayDanhSach(trangThai: "DA_THANH_TOAN").Sum(h => h.TongTien);
        Assert.Equal(tatCa, tong.GiaTris[^1]);
    }

    [Fact]
    public void ThuChi_LoiNhuanBangThuTruChi()
    {
        foreach (var d in _bll.ThuChiLoiNhuan("QUY", 2025, ThongKeBLL.TruyVanMoi).Dongs)
            Assert.Equal(d.GiaTris[0] - d.GiaTris[1], d.GiaTris[2]);
    }
}

public class TimKiemTest : TestCoSo
{
    private readonly TimKiemBLL _bll = new();

    private static TimKiemNangCaoDTO Dk(bool and, params (string, string, string)[] ds) => new()
    {
        KhopTatCa = and, DieuKiens = ds.Select(d => new DieuKienTimDTO { Truong = d.Item1, ToanTu = d.Item2, GiaTri = d.Item3 }).ToList()
    };

    [Fact]
    public void HoaDon_SoSanhLonNho_HaiCachGiongNhau()
    {
        var dk = Dk(true, ("tong_tien", ">=", "1000000"), ("ngay_lap", ">=", "01/01/2025"), ("ngay_lap", "<", "01/01/2026"));
        var moi = _bll.TimHoaDon(dk);
        var loc = _bll.LocHoaDon(new HoaDonBLL().LayDanhSach(), dk);
        Assert.NotEmpty(moi);
        Assert.All(moi, h => Assert.True(h.TongTien >= 1_000_000 && h.NgayLap.Year == 2025));
        Assert.Equal(moi.Select(h => h.MaHoaDon).OrderBy(x => x), loc.Select(h => h.MaHoaDon).OrderBy(x => x));
    }

    [Fact]
    public void KhachHang_HcmHoacHn_Hop()
    {
        var hop = _bll.TimKhachHang(Dk(false, ("tinh_thanh", "=", "TP Hồ Chí Minh"), ("tinh_thanh", "=", "Hà Nội")));
        var union = new KhachHangBLL().TheoHaiTinhHop("TP Hồ Chí Minh", "Hà Nội");
        var inList = _bll.TimKhachHang(Dk(true, ("tinh_thanh", "IN", "TP Hồ Chí Minh, Hà Nội")));
        Assert.NotEmpty(hop);
        Assert.Equal(union.Select(k => k.MaKh), hop.Select(k => k.MaKh));
        Assert.Equal(union.Select(k => k.MaKh), inList.Select(k => k.MaKh));
    }

    [Fact]
    public void HoaDon_KhachHcm_VaTongTienLon_Giao()
    {
        var kq = _bll.TimHoaDon(Dk(true, ("tinh_thanh", "=", "TP Hồ Chí Minh"), ("tong_tien", ">", "500000")));
        Assert.All(kq, h => Assert.True(h.TinhThanhKhach == "TP Hồ Chí Minh" && h.TongTien > 500_000));
    }

    [Theory]
    [InlineData("tong_tien; DROP TABLE HOA_DON", "=", "1")]
    [InlineData("tong_tien", "= 1 OR 1", "1")]
    [InlineData("tong_tien", ">", "abc")]
    [InlineData("tong_tien", "LIKE", "1")]
    public void DieuKienKhongHopLe_BiChan(string truong, string toanTu, string giaTri) =>
        Assert.Throws<LoiNghiepVu>(() => _bll.TimHoaDon(Dk(true, (truong, toanTu, giaTri))));

    [Fact]
    public void GiaTriCoDauNhay_LaThamSo_KhongLoiSql() =>
        Assert.Empty(_bll.TimKhachHang(Dk(true, ("ho_ten", "LIKE", "x' OR '1'='1"))));
}

public class XuatFileTest : TestCoSo
{
    [Fact]
    public void Excel_FileMau_DocLaiDuoc()
    {
        var bll = new ExcelBLL();
        var kq = bll.DocSanPham(new MemoryStream(bll.MauSanPham()));
        Assert.Single(kq.HopLe);
        Assert.Empty(kq.Lois);
        Assert.Equal("LSP01", kq.HopLe[0].MaLoaiSp);
        Assert.Equal(249000, kq.HopLe[0].DonGia);

        var ct = bll.DocChiTietPhieuNhap(new MemoryStream(bll.MauChiTietPhieuNhap()));
        Assert.Single(ct.HopLe);
        Assert.Equal(20 * 120000, ct.HopLe[0].ThanhTien);

        var khach = bll.DocKhachHang(new MemoryStream(bll.MauKhachHang()));
        Assert.Equal(new DateTime(2000, 8, 15), khach.HopLe[0].NgaySinh);
    }

    [Fact]
    public void Excel_XuatDanhSach_DocLaiThayLoiThieuCot()
    {
        var bll = new ExcelBLL();
        var file = bll.XuatSanPham(new SanPhamBLL().LayTatCa());
        Assert.True(file.Length > 1000);
        // file xuat khong dung mau nhap -> bao thieu cot
        Assert.Throws<LoiNghiepVu>(() => bll.DocSanPham(new MemoryStream(file)));
        Assert.Throws<LoiNghiepVu>(() => bll.DocSanPham(new MemoryStream(new byte[] { 1, 2, 3 })));
        Assert.True(bll.XuatBangThongKe(new ThongKeBLL().TheoNhanVienNam(ThongKeBLL.TruyVanMoi)).Length > 1000);
    }

    [Fact]
    public void Pdf_HoaDon_PhieuNhap_BaoCao()
    {
        var pdf = new PdfBLL();
        var hd = new HoaDonBLL().LayDanhSach(trangThai: "DA_THANH_TOAN").First();
        var a = pdf.HoaDon(new HoaDonBLL().LayTheoMa(hd.MaHoaDon));
        var b = pdf.PhieuNhap(new PhieuNhapBLL().LayTheoMa(new PhieuNhapBLL().LayDanhSach().First().MaPhieuNhap));
        var c = pdf.BaoCao(new ThongKeBLL().TheoSanPhamQuy(2025, ThongKeBLL.TruyVanMoi));
        foreach (var f in new[] { a, b, c })
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(f, 0, 4));
        var thuMuc = Environment.GetEnvironmentVariable("QLBTT_LUU_PDF");
        if (thuMuc != null)
        {
            File.WriteAllBytes(Path.Combine(thuMuc, "HoaDon.pdf"), a);
            File.WriteAllBytes(Path.Combine(thuMuc, "PhieuNhap.pdf"), b);
            File.WriteAllBytes(Path.Combine(thuMuc, "BaoCao.pdf"), c);
        }
    }
}

public class NhatKySqlTest : TestCoSo
{
    [Fact]
    public void GhiDuocFileVaDongCode()
    {
        using var dem = HeThongBLL.BatDauDemSql();
        new HoaDonBLL().LayChiTiet("HD00001");
        Assert.Equal(1, dem.SoCau);
        var cau = HeThongBLL.NhatKySqlGanNhat(1).Single();
        Assert.Equal("HoaDonDAL.cs", cau.File);
        Assert.Equal("LayChiTiet", cau.HamGoi);
        Assert.True(cau.Dong > 0);
        Assert.Contains("CHI_TIET_HOA_DON", cau.CauSql);
    }
}
