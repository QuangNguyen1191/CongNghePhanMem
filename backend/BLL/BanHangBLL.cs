using DAL;
using DTO;

namespace BLL;

public class GioHangBLL
{
    private readonly GioHangDAL _dal = new();
    private readonly SanPhamDAL _spDal = new();
    private readonly KhuyenMaiBLL _kmBll = new();

    // Xem gio: gia tung dong da tinh khuyen mai san pham hom nay
    public GioHangDTO Xem(string maKh)
    {
        var gio = _dal.LayTheoKhach(maKh) ?? new GioHangDTO { MaKh = maKh };
        if (gio.ChiTiets.Count > 0)
        {
            var gia = _kmBll.TinhTien(gio.ChiTiets.Select(c => new DongHangDTO { MaSp = c.MaSp, SoLuong = c.SoLuong }).ToList(),
                DateTime.Today, null, kiemTraTon: false);
            foreach (var c in gio.ChiTiets)
                c.DonGia = gia.Dongs.First(d => d.MaSp == c.MaSp).DonGiaBan;
        }
        return gio;
    }

    public GioHangDTO Them(string maKh, string maSp, int soLuong)
    {
        QuyTac.KiemTra(soLuong > 0, "Số lượng phải lớn hơn 0");
        var maGio = LayHoacTaoGio(maKh);
        return DatSoLuong(maKh, maGio, maSp, _dal.SoLuongTrongGio(maGio, maSp) + soLuong);
    }

    public GioHangDTO CapNhat(string maKh, string maSp, int soLuong)
    {
        var maGio = LayHoacTaoGio(maKh);
        if (soLuong <= 0)
        {
            _dal.XoaDong(maGio, maSp);
            return Xem(maKh);
        }
        return DatSoLuong(maKh, maGio, maSp, soLuong);
    }

    public GioHangDTO XoaDong(string maKh, string maSp)
    {
        var gio = _dal.LayTheoKhach(maKh);
        if (gio != null) _dal.XoaDong(gio.MaGioHang, maSp);
        return Xem(maKh);
    }

    public void XoaHet(string maKh)
    {
        var gio = _dal.LayTheoKhach(maKh);
        if (gio != null) _dal.XoaHet(gio.MaGioHang);
    }

    // Gio tam cua khach chua dang nhap (luu tren trinh duyet) duoc gop vao gio khi dang nhap
    public GioHangDTO GopGioTam(string maKh, List<DongHangDTO> gioTam)
    {
        foreach (var d in gioTam.Where(d => d.SoLuong > 0))
        {
            try { Them(maKh, d.MaSp, d.SoLuong); }
            catch (LoiNghiepVu) { /* SP het hang / ngung ban thi bo qua dong do */ }
        }
        return Xem(maKh);
    }

    private GioHangDTO DatSoLuong(string maKh, string maGio, string maSp, int soLuong)
    {
        var sp = QuyTac.CanCo(_spDal.LayTheoMa(maSp), "Không tìm thấy sản phẩm");
        QuyTac.KiemTra(sp.TrangThai == "DANG_BAN", "Sản phẩm đã ngừng bán");
        QuyTac.KiemTra(soLuong <= sp.SoLuong, $"{KhuyenMaiBLL.TenDayDu(sp)} chỉ còn {sp.SoLuong} cái");
        _dal.DatSoLuong(maGio, maSp, soLuong);
        return Xem(maKh);
    }

    private string LayHoacTaoGio(string maKh)
    {
        var gio = _dal.LayTheoKhach(maKh);
        if (gio != null) return gio.MaGioHang;
        return DbHelper.GiaoDich(tx =>
        {
            var ma = DbHelper.SinhMa("GIO_HANG", "ma_gio_hang", "GH", 3, tx);
            _dal.TaoGio(ma, maKh, tx);
            return ma;
        });
    }
}

public class DonDatHangBLL
{
    private readonly DonDatHangDAL _dal = new();
    private readonly GioHangDAL _gioDal = new();
    private readonly HoaDonDAL _hdDal = new();
    private readonly SanPhamDAL _spDal = new();
    private readonly KhuyenMaiBLL _kmBll = new();

    // Khach dat hang tu gio: tao don CHO_XAC_NHAN, xoa gio. Chua tru ton (tru khi nhan vien xac nhan).
    public DonDatHangDTO DatHang(string maKh, DatHangDTO yc)
    {
        QuyTac.KiemTra(!string.IsNullOrWhiteSpace(yc.DiaChiGiao), "Vui lòng nhập địa chỉ giao hàng");
        QuyTac.KiemTra(!string.IsNullOrWhiteSpace(yc.SdtNhan), "Vui lòng nhập số điện thoại nhận hàng");
        QuyTac.KiemTra(yc.PhuongThucTt is "COD" or "CHUYEN_KHOAN" or "ONLINE", "Phương thức thanh toán không hợp lệ");
        var gio = QuyTac.CanCo(_gioDal.LayTheoKhach(maKh), "Giỏ hàng đang trống");
        QuyTac.KiemTra(gio.ChiTiets.Count > 0, "Giỏ hàng đang trống");

        var maDon = DbHelper.GiaoDich(tx =>
        {
            var ngay = DateTime.Now;
            var bang = _kmBll.TinhTien(gio.ChiTiets.Select(c => new DongHangDTO { MaSp = c.MaSp, SoLuong = c.SoLuong }).ToList(),
                ngay, yc.MaVoucher, tx);
            var maDon = DbHelper.SinhMa("DON_DAT_HANG", "ma_don", "DH", 5, tx);
            _dal.Them(new DonDatHangDTO
            {
                MaDon = maDon, MaKh = maKh, NgayDat = ngay, DiaChiGiao = yc.DiaChiGiao.Trim(), SdtNhan = yc.SdtNhan.Trim(),
                TenNguoiNhan = QuyTac.RongThanhNull(yc.TenNguoiNhan), PhuongThucTt = yc.PhuongThucTt,
                MaVoucher = QuyTac.RongThanhNull(yc.MaVoucher)?.ToUpper(), TienGiam = bang.TienGiam, TongTien = bang.TongTien,
                ChiTiets = bang.Dongs.Select(d => new ChiTietDonDatHangDTO
                {
                    MaDon = maDon, MaSp = d.MaSp, SoLuong = d.SoLuong, DonGia = d.DonGiaBan, ThanhTien = d.ThanhTien
                }).ToList()
            }, tx);
            _gioDal.XoaHet(gio.MaGioHang, tx);
            return maDon;
        });
        return LayTheoMa(maDon);
    }

    public DonDatHangDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy đơn hàng");

    public List<DonDatHangDTO> LayCuaKhach(string maKh) => _dal.LayTheoKhach(maKh);

    public List<DonDatHangDTO> LayDanhSach(string? trangThai = null, DateTime? tuNgay = null, DateTime? denNgay = null) =>
        _dal.LayDanhSach(QuyTac.RongThanhNull(trangThai), tuNgay, denNgay);

    // Khach tu huy khi don chua duoc xac nhan
    public void KhachHuy(string maKh, string maDon)
    {
        var don = LayTheoMa(maDon);
        QuyTac.KiemTra(don.MaKh == maKh, "Đơn hàng không phải của bạn");
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.DoiTrangThai(maDon, "CHO_XAC_NHAN", "DA_HUY", null, tx)),
            "Đơn đã được xác nhận, vui lòng liên hệ cửa hàng để hủy");
    }

    // Nhan vien xac nhan: tru ton + lap hoa don trong 1 giao dich.
    // Chuyen khoan / online coi nhu da thanh toan; COD thi hoa don cho thanh toan den khi giao xong.
    public HoaDonDTO XacNhan(string maDon, string maNv)
    {
        var maHd = DbHelper.GiaoDich(tx =>
        {
            var don = QuyTac.CanCo(_dal.LayTheoMa(maDon, tx), "Không tìm thấy đơn hàng");
            QuyTac.KiemTra(_dal.DoiTrangThai(maDon, "CHO_XAC_NHAN", "DA_XAC_NHAN", maNv, tx),
                "Đơn không còn ở trạng thái chờ xác nhận");
            foreach (var c in don.ChiTiets)
                QuyTac.KiemTra(_spDal.CapNhatTon(c.MaSp, -c.SoLuong, tx), $"Sản phẩm {c.TenSp} không đủ hàng để giao");
            var daTra = don.PhuongThucTt != "COD";
            var hd = new HoaDonDTO
            {
                MaHoaDon = DbHelper.SinhMa("HOA_DON", "ma_hoa_don", "HD", 5, tx), MaDon = maDon, MaKh = don.MaKh, MaNv = maNv,
                NgayLap = DateTime.Today, TrangThai = daTra ? "DA_THANH_TOAN" : "CHO_THANH_TOAN",
                MaKm = TimMaKm(don, tx), TienGiam = don.TienGiam, PhuongThucTt = don.PhuongThucTt, TongTien = don.TongTien,
                ChiTiets = don.ChiTiets.Select(c => new ChiTietHoaDonDTO
                    { MaSp = c.MaSp, SoLuong = c.SoLuong, DonGiaBan = c.DonGia, ThanhTien = c.ThanhTien }).ToList()
            };
            hd.ChiTiets.ForEach(c => c.MaHoaDon = hd.MaHoaDon);
            _hdDal.Them(hd, tx);
            if (daTra) TichDiem.TinhLai(don.MaKh, tx);
            return hd.MaHoaDon;
        });
        ThongKeBLL.XoaBoNhoDem();
        return _hdDal.LayTheoMa(maHd)!;
    }

    public void TuChoi(string maDon, string maNv) =>
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.DoiTrangThai(maDon, "CHO_XAC_NHAN", "TU_CHOI", maNv, tx)),
            "Chỉ từ chối được đơn đang chờ xác nhận");

    public void GiaoHang(string maDon, string maNv) =>
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.DoiTrangThai(maDon, "DA_XAC_NHAN", "DANG_GIAO", maNv, tx)),
            "Chỉ giao được đơn đã xác nhận");

    // Giao thanh cong: don HOAN_TAT, hoa don COD chuyen sang da thanh toan, cong diem
    public void HoanTat(string maDon, string maNv)
    {
        DbHelper.GiaoDich(tx =>
        {
            QuyTac.KiemTra(_dal.DoiTrangThai(maDon, "DANG_GIAO", "HOAN_TAT", maNv, tx), "Chỉ hoàn tất được đơn đang giao");
            var hd = _hdDal.LayTheoDon(maDon, tx)!;
            _hdDal.DoiTrangThai(hd.MaHoaDon, "CHO_THANH_TOAN", "DA_THANH_TOAN", tx);
            TichDiem.TinhLai(hd.MaKh, tx);
        });
        ThongKeBLL.XoaBoNhoDem();
    }

    // Nhan vien huy don da xac nhan / dang giao (khach bom hang...): tra hang ve kho, huy hoa don
    public void NhanVienHuy(string maDon, string maNv)
    {
        DbHelper.GiaoDich(tx => HuyTrongGiaoDich(maDon, maNv, tx));
        ThongKeBLL.XoaBoNhoDem();
    }

    private void HuyTrongGiaoDich(string maDon, string maNv, System.Data.IDbTransaction tx)
    {
        var don = QuyTac.CanCo(_dal.LayTheoMa(maDon, tx), "Không tìm thấy đơn hàng");
        QuyTac.KiemTra(don.TrangThai is "CHO_XAC_NHAN" or "DA_XAC_NHAN" or "DANG_GIAO", "Đơn đã kết thúc, không hủy được");
        QuyTac.KiemTra(_dal.DoiTrangThai(maDon, don.TrangThai, "DA_HUY", maNv, tx), "Đơn vừa được người khác cập nhật");
        var hd = _hdDal.LayTheoDon(maDon, tx);
        if (hd == null) return;
        _hdDal.DoiTrangThai(hd.MaHoaDon, hd.TrangThai, "DA_HUY", tx);
        foreach (var c in don.ChiTiets) _spDal.CapNhatTon(c.MaSp, c.SoLuong, tx);
        TichDiem.TinhLai(hd.MaKh, tx);
    }

    private string? TimMaKm(DonDatHangDTO don, System.Data.IDbTransaction tx) =>
        don.MaVoucher != null
            ? new KhuyenMaiDAL().LayVoucherDangChay(don.MaVoucher, don.NgayDat, tx)?.MaKm
            : _kmBll.TinhTien(don.ChiTiets.Select(c => new DongHangDTO { MaSp = c.MaSp, SoLuong = c.SoLuong }).ToList(),
                don.NgayDat, null, tx, kiemTraTon: false).MaKm;
}

public class HoaDonBLL
{
    private readonly HoaDonDAL _dal = new();
    private readonly SanPhamDAL _spDal = new();
    private readonly KhachHangDAL _khDal = new();
    private readonly DoiTraDAL _dtDal = new();
    private readonly KhuyenMaiBLL _kmBll = new();

    // Xem truoc tien truoc khi chot (man hinh ban tai quay)
    public BangTinhTienDTO TinhTruoc(BanTaiQuayDTO yc) => _kmBll.TinhTien(yc.DongHangs, DateTime.Today, yc.MaVoucher);

    // Ban tai quay: lap hoa don da thanh toan, tru ton, tinh lai diem - tat ca trong 1 giao dich
    public HoaDonDTO BanTaiQuay(BanTaiQuayDTO yc, string maNv)
    {
        QuyTac.KiemTra(yc.PhuongThucTt is "TIEN_MAT" or "CHUYEN_KHOAN", "Bán tại quầy chỉ nhận tiền mặt hoặc chuyển khoản");
        var maKh = QuyTac.RongThanhNull(yc.MaKh);
        if (maKh != null) QuyTac.CanCo(_khDal.LayTheoMa(maKh), "Không tìm thấy khách hàng");

        var maHd = DbHelper.GiaoDich(tx =>
        {
            var bang = _kmBll.TinhTien(yc.DongHangs, DateTime.Today, yc.MaVoucher, tx);
            var hd = new HoaDonDTO
            {
                MaHoaDon = DbHelper.SinhMa("HOA_DON", "ma_hoa_don", "HD", 5, tx), MaKh = maKh, MaNv = maNv,
                NgayLap = DateTime.Today, TrangThai = "DA_THANH_TOAN", MaKm = bang.MaKm, TienGiam = bang.TienGiam,
                PhuongThucTt = yc.PhuongThucTt, TongTien = bang.TongTien, ChiTiets = bang.Dongs
            };
            hd.ChiTiets.ForEach(c => c.MaHoaDon = hd.MaHoaDon);
            _dal.Them(hd, tx);
            foreach (var c in hd.ChiTiets)
                QuyTac.KiemTra(_spDal.CapNhatTon(c.MaSp, -c.SoLuong, tx), $"Sản phẩm {c.TenSp} không đủ hàng");
            TichDiem.TinhLai(maKh, tx);
            return hd.MaHoaDon;
        });
        ThongKeBLL.XoaBoNhoDem();
        return LayTheoMa(maHd);
    }

    public HoaDonDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy hóa đơn");

    // Main-sub form (yeu cau 4): danh sach ben trai...
    public List<HoaDonDTO> LayDanhSach(DateTime? tuNgay = null, DateTime? denNgay = null, string? trangThai = null,
        string? maNv = null, string? maKh = null) =>
        _dal.LayDanhSach(tuNgay, denNgay, QuyTac.RongThanhNull(trangThai), QuyTac.RongThanhNull(maNv), QuyTac.RongThanhNull(maKh));

    // ... va chi tiet ben phai khi bam vao 1 hoa don
    public List<ChiTietHoaDonDTO> LayChiTiet(string ma) => _dal.LayChiTiet(ma);

    public List<HoaDonDTO> LayCuaKhach(string maKh) => _dal.LayDanhSach(null, null, null, null, maKh);

    // Huy hoa don ban tai quay (nhap sai...): tra hang ve kho, tinh lai diem. Hoa don tu don online thi huy qua don.
    public void Huy(string maHd)
    {
        DbHelper.GiaoDich(tx =>
        {
            var hd = QuyTac.CanCo(_dal.LayTheoMa(maHd, tx), "Không tìm thấy hóa đơn");
            QuyTac.KiemTra(hd.MaDon == null, "Hóa đơn của đơn online, vui lòng hủy từ màn hình đơn hàng");
            QuyTac.KiemTra(hd.TrangThai != "DA_HUY", "Hóa đơn đã bị hủy trước đó");
            QuyTac.KiemTra(_dtDal.LayDanhSach(null, null).All(d => d.MaHoaDon != maHd || d.TrangThai == "TU_CHOI"),
                "Hóa đơn đã có phiếu đổi trả, không hủy được");
            QuyTac.KiemTra(_dal.DoiTrangThai(maHd, hd.TrangThai, "DA_HUY", tx), "Hóa đơn vừa được người khác cập nhật");
            foreach (var c in hd.ChiTiets) _spDal.CapNhatTon(c.MaSp, c.SoLuong, tx);
            TichDiem.TinhLai(hd.MaKh, tx);
        });
        ThongKeBLL.XoaBoNhoDem();
    }
}
