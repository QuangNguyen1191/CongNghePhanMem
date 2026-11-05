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
    private readonly KhachHangDAL _khDal = new();

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
            // v3: khach co tai khoan duoc tinh them uu dai thanh vien moi (neu con luot)
            var bang = _kmBll.TinhTien(gio.ChiTiets.Select(c => new DongHangDTO { MaSp = c.MaSp, SoLuong = c.SoLuong }).ToList(),
                ngay, yc.MaVoucher, tx, maKhThanhVien: maKh);
            var maDon = DbHelper.SinhMa("DON_DAT_HANG", "ma_don", "DH", 5, tx);
            _dal.Them(TaoDon(maDon, maKh, ngay, bang, "TAI_KHOAN", yc.DiaChiGiao.Trim(), yc.SdtNhan.Trim(),
                QuyTac.RongThanhNull(yc.TenNguoiNhan), null, yc.PhuongThucTt), tx);
            _gioDal.XoaHet(gio.MaGioHang, tx);
            return maDon;
        });
        return LayTheoMa(maDon);
    }

    // ===== v3: KHACH VANG LAI (khong co tai khoan). SDT da duoc xac thuc OTP o XacThucBLL truoc khi goi cac ham nay.

    // Dat hang khong can tai khoan. Gio hang nam o trinh duyet nen gui kem danh sach dong hang.
    // Khach nhan dien bang SDT: chua co thi tao KHACH_HANG (ma_tk NULL), co roi thi dung lai -> cac don cung SDT gom ve 1 khach.
    public DonDatHangDTO DatHangVangLai(string sdt, DatHangVangLaiDTO yc)
    {
        sdt = XacThucBLL.KiemTraSdt(sdt);
        var hoTen = (yc.HoTen ?? "").Trim();
        QuyTac.KiemTra(hoTen.Length >= 2, "Vui lòng nhập họ tên người nhận");
        var diaChi = (yc.DiaChiGiao ?? "").Trim();
        QuyTac.KiemTra(diaChi.Length >= 10, "Vui lòng nhập địa chỉ giao hàng đầy đủ");
        var email = QuyTac.RongThanhNull(yc.Email);
        QuyTac.KiemTra(email == null || email.Contains('@') && email.Length <= 100, "Email không hợp lệ");
        QuyTac.KiemTra(yc.PhuongThucTt is "COD" or "CHUYEN_KHOAN" or "ONLINE", "Phương thức thanh toán không hợp lệ");
        var dongs = (yc.DongHangs ?? new()).Where(d => d.SoLuong > 0).ToList();
        QuyTac.KiemTra(dongs.Count > 0, "Giỏ hàng đang trống");
        QuyTac.KiemTra(dongs.GroupBy(d => d.MaSp.Trim().ToUpper()).All(g => g.Sum(d => d.SoLuong) <= QuyTac.VangLaiToiDaMoiSanPham),
            $"Khách chưa có tài khoản đặt tối đa {QuyTac.VangLaiToiDaMoiSanPham} cái mỗi sản phẩm. Vui lòng tạo tài khoản để đặt số lượng lớn");

        var khCu = _khDal.LayTheoSdt(sdt);
        if (khCu?.MaTk != null)
            QuyTac.KiemTra(new TaiKhoanDAL().LayTheoMa(khCu.MaTk)?.TrangThai == "HOAT_DONG",
                "Số điện thoại này thuộc tài khoản đã bị khóa, vui lòng liên hệ cửa hàng");
        var emailKh = email != null && !_khDal.TrungEmail(email, khCu?.MaKh) ? email : null;

        var maDon = DbHelper.GiaoDich(tx =>
        {
            var kh = _khDal.LayTheoSdt(sdt, tx);
            string maKh;
            if (kh == null)
            {
                maKh = DbHelper.SinhMa("KHACH_HANG", "ma_kh", "KH", 3, tx);
                _khDal.Them(new KhachHangDTO { MaKh = maKh, HoTen = hoTen, Sdt = sdt, Email = emailKh, DiaChi = diaChi, SdtDaXacThuc = true }, tx);
            }
            else
            {
                maKh = kh.MaKh;
                _khDal.CapNhatVangLai(maKh, hoTen, emailKh, diaChi, tx);   // khach co tai khoan thi khong dung vao ho so
            }

            var dangCho = _dal.DemDonVangLaiDangCho(maKh, tx);
            QuyTac.KiemTra(dangCho < QuyTac.VangLaiToiDaDonDangCho,
                $"Số điện thoại này đang có {dangCho} đơn chờ cửa hàng xác nhận. Vui lòng đợi xác nhận, hoặc tạo tài khoản để đặt thêm");
            if (yc.PhuongThucTt == "COD")
            {
                var bom = _dal.DemLichSu(maKh, tx).SoLanBomHang;
                QuyTac.KiemTra(bom < QuyTac.SoLanBomChanCod,
                    $"Số điện thoại này có {bom} đơn đã xác nhận nhưng không nhận hàng. Vui lòng chọn chuyển khoản hoặc thanh toán online");
            }

            var ngay = DateTime.Now;
            var bang = _kmBll.TinhTien(dongs, ngay, yc.MaVoucher, tx);
            if (yc.PhuongThucTt == "COD")
                QuyTac.KiemTra(bang.TongTien <= QuyTac.VangLaiCodToiDa,
                    $"Đơn trả khi nhận hàng (COD) của khách chưa có tài khoản tối đa {QuyTac.VangLaiCodToiDa:N0}đ. "
                    + "Vui lòng chọn chuyển khoản / online hoặc tạo tài khoản");
            var ma = DbHelper.SinhMa("DON_DAT_HANG", "ma_don", "DH", 5, tx);
            _dal.Them(TaoDon(ma, maKh, ngay, bang, "VANG_LAI", diaChi, sdt, hoTen, email, yc.PhuongThucTt), tx);
            return ma;
        });
        return LayTheoMa(maDon);
    }

    // Tra cuu don bang SDT da xac thuc: tat ca don cua SDT do (ca don dat bang tai khoan neu SDT da co tai khoan)
    public List<DonDatHangDTO> LayCuaSdt(string sdt) =>
        _khDal.LayTheoSdt(sdt) is { } kh ? _dal.LayTheoKhach(kh.MaKh) : new();

    public DonDatHangDTO LayCuaSdt(string sdt, string maDon)
    {
        var don = LayTheoMa(maDon);
        if (_khDal.LayTheoSdt(sdt)?.MaKh != don.MaKh) throw new KhongTimThay("Không tìm thấy đơn hàng");
        return don;
    }

    public void VangLaiHuy(string sdt, string maDon) => KhachHuy(LayCuaSdt(sdt, maDon).MaKh, maDon);

    public DonDatHangDTO VangLaiYeuCauHuy(string sdt, string maDon, string? lyDo) =>
        YeuCauHuy(LayCuaSdt(sdt, maDon).MaKh, maDon, lyDo);

    private static DonDatHangDTO TaoDon(string maDon, string maKh, DateTime ngay, BangTinhTienDTO bang, string nguon,
        string diaChi, string sdtNhan, string? tenNguoiNhan, string? email, string phuongThuc) => new()
    {
        MaDon = maDon, MaKh = maKh, NgayDat = ngay, DiaChiGiao = diaChi, SdtNhan = sdtNhan, TenNguoiNhan = tenNguoiNhan,
        PhuongThucTt = phuongThuc, Nguon = nguon, EmailNhan = email, MaKm = bang.MaKm,
        // uu dai thanh vien moi co the thang voucher: khi do khong ghi voucher vao don
        MaVoucher = bang.MaVoucher,
        TienGiam = bang.TienGiam, TongTien = bang.TongTien,
        ChiTiets = bang.Dongs.Select(d => new ChiTietDonDatHangDTO
        {
            MaDon = maDon, MaSp = d.MaSp, SoLuong = d.SoLuong, DonGia = d.DonGiaBan, ThanhTien = d.ThanhTien
        }).ToList()
    };

    public DonDatHangDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy đơn hàng");

    public List<DonDatHangDTO> LayCuaKhach(string maKh) => _dal.LayTheoKhach(maKh);

    // nguon (v3): TAI_KHOAN | VANG_LAI, de web staff loc rieng don khach vang lai
    public List<DonDatHangDTO> LayDanhSach(string? trangThai = null, DateTime? tuNgay = null, DateTime? denNgay = null,
        string? nguon = null, string? yeuCauHuy = null) =>
        _dal.LayDanhSach(QuyTac.RongThanhNull(trangThai), tuNgay, denNgay, QuyTac.RongThanhNull(nguon), QuyTac.RongThanhNull(yeuCauHuy));

    // Khach tu huy khi don chua duoc xac nhan (huy ngay, khong can duyet)
    public void KhachHuy(string maKh, string maDon, string? lyDo = null)
    {
        var don = LayTheoMa(maDon);
        QuyTac.KiemTra(don.MaKh == maKh, "Đơn hàng không phải của bạn");
        lyDo = KiemTraLyDo(lyDo, batBuoc: false);
        QuyTac.KiemTra(DbHelper.GiaoDich(tx =>
            {
                if (!_dal.DoiTrangThai(maDon, "CHO_XAC_NHAN", "DA_HUY", null, tx)) return false;
                _dal.GhiHuy(maDon, "KHACH", lyDo, tx);
                return true;
            }),
            "Đơn đã được xác nhận. Bạn có thể gửi yêu cầu hủy để cửa hàng xem xét");
    }

    // v3: Khach muon huy.
    //   CHO_XAC_NHAN -> huy ngay.  DA_XAC_NHAN (chua giao) -> gui yeu cau, nhan vien duyet / tu choi (moi don 1 lan).
    //   DANG_GIAO -> khong huy online duoc (lien he cua hang / tu choi nhan hang).
    public DonDatHangDTO YeuCauHuy(string maKh, string maDon, string? lyDo)
    {
        var don = LayTheoMa(maDon);
        QuyTac.KiemTra(don.MaKh == maKh, "Đơn hàng không phải của bạn");
        var lyDoDung = KiemTraLyDo(lyDo, batBuoc: true)!;
        switch (don.TrangThai)
        {
            case "CHO_XAC_NHAN":
                KhachHuy(maKh, maDon, lyDoDung);
                break;
            case "DA_XAC_NHAN":
                QuyTac.KiemTra(don.YeuCauHuy != "CHO_DUYET", "Bạn đã gửi yêu cầu hủy, cửa hàng đang xem xét");
                QuyTac.KiemTra(don.YeuCauHuy != "TU_CHOI",
                    $"Cửa hàng đã từ chối yêu cầu hủy đơn này: {don.PhanHoiHuy}");
                QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.GuiYeuCauHuy(maDon, lyDoDung, tx)),
                    "Đơn vừa được cập nhật, vui lòng tải lại trang");
                break;
            case "DANG_GIAO":
                throw new LoiNghiepVu("Đơn đang được giao nên không hủy trực tuyến được. Vui lòng liên hệ hotline của cửa hàng");
            default:
                throw new LoiNghiepVu("Đơn đã kết thúc, không hủy được");
        }
        return LayTheoMa(maDon);
    }

    // v3: nhan vien dong y yeu cau huy cua khach: huy nhu binh thuong (tra kho, huy HD, hoan tien neu da tra) nhung khong tinh bom hang
    public void DuyetHuy(string maDon, string maNv)
    {
        DbHelper.GiaoDich(tx =>
        {
            QuyTac.KiemTra(_dal.XuLyYeuCauHuy(maDon, "DA_DUYET", null, maNv, tx), "Đơn không có yêu cầu hủy đang chờ duyệt");
            HuyTrongGiaoDich(maDon, maNv, tx, "KHACH", null);
        });
        ThongKeBLL.XoaBoNhoDem();
    }

    // v3: nhan vien tu choi yeu cau huy (vd hang da dong goi / da giao cho don vi van chuyen): bat buoc ghi ly do cho khach
    public void TuChoiHuy(string maDon, string maNv, string? phanHoi)
    {
        var p = KiemTraLyDo(phanHoi, batBuoc: true, ten: "lý do từ chối")!;
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.XuLyYeuCauHuy(maDon, "TU_CHOI", p, maNv, tx)),
            "Đơn không có yêu cầu hủy đang chờ duyệt");
    }

    private static string? KiemTraLyDo(string? lyDo, bool batBuoc, string ten = "lý do hủy")
    {
        var t = QuyTac.RongThanhNull(lyDo);
        QuyTac.KiemTra(t != null || !batBuoc, $"Vui lòng nhập {ten}");
        QuyTac.KiemTra(t == null || t.Length <= 255, $"{char.ToUpper(ten[0]) + ten[1..]} tối đa 255 ký tự");
        return t;
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

    public void GiaoHang(string maDon, string maNv)
    {
        QuyTac.KiemTra(LayTheoMa(maDon).YeuCauHuy != "CHO_DUYET", "Khách đang xin hủy đơn này, vui lòng duyệt hoặc từ chối trước khi giao");
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.DoiTrangThai(maDon, "DA_XAC_NHAN", "DANG_GIAO", maNv, tx)),
            "Chỉ giao được đơn đã xác nhận");
    }

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

    // Nhan vien huy don da xac nhan / dang giao (khach bom hang...): tra hang ve kho, huy hoa don, hoan tien neu da tra.
    // Don dang co yeu cau huy cua khach -> coi nhu duyet yeu cau (khong tinh bom hang).
    public void NhanVienHuy(string maDon, string maNv, string? lyDo = null)
    {
        if (LayTheoMa(maDon).YeuCauHuy == "CHO_DUYET") { DuyetHuy(maDon, maNv); return; }
        var l = KiemTraLyDo(lyDo, batBuoc: false);
        DbHelper.GiaoDich(tx => HuyTrongGiaoDich(maDon, maNv, tx, "NHAN_VIEN", l));
        ThongKeBLL.XoaBoNhoDem();
    }

    private void HuyTrongGiaoDich(string maDon, string maNv, System.Data.IDbTransaction tx, string nguoiHuy, string? lyDo)
    {
        var don = QuyTac.CanCo(_dal.LayTheoMa(maDon, tx), "Không tìm thấy đơn hàng");
        QuyTac.KiemTra(don.TrangThai is "CHO_XAC_NHAN" or "DA_XAC_NHAN" or "DANG_GIAO", "Đơn đã kết thúc, không hủy được");
        QuyTac.KiemTra(_dal.DoiTrangThai(maDon, don.TrangThai, "DA_HUY", maNv, tx), "Đơn vừa được người khác cập nhật");
        _dal.GhiHuy(maDon, nguoiHuy, lyDo, tx);
        var hd = _hdDal.LayTheoDon(maDon, tx);
        if (hd == null) return;
        _hdDal.DoiTrangThai(hd.MaHoaDon, hd.TrangThai, "DA_HUY", tx);
        foreach (var c in don.ChiTiets) _spDal.CapNhatTon(c.MaSp, c.SoLuong, tx);
        TichDiem.TinhLai(hd.MaKh, tx);
        // v3: khach da tra tien (chuyen khoan / online) -> phai hoan lai
        if (hd.TrangThai == "DA_THANH_TOAN")
            new HoanTienDAL().Them(new HoanTienDTO
            {
                MaHoanTien = DbHelper.SinhMa("HOAN_TIEN", "ma_hoan_tien", "HT", 5, tx), MaDon = maDon, MaHoaDon = hd.MaHoaDon,
                SoTien = hd.TongTien, PhuongThuc = hd.PhuongThucTt,
                GhiChu = nguoiHuy == "KHACH" ? "Khách xin hủy đơn đã thanh toán" : "Cửa hàng hủy đơn đã thanh toán",
            }, tx);
    }

    // v3: don moi da luu ma_km luc dat; don cu (truoc v3) thi tinh lai nhu truoc
    private string? TimMaKm(DonDatHangDTO don, System.Data.IDbTransaction tx) =>
        don.MaKm != null ? don.MaKm
        : don.MaVoucher != null
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

// v3: hoan tien cho don da thanh toan truoc bi huy (danh sach cho web staff, nhan vien xac nhan da chuyen tien lai)
public class HoanTienBLL
{
    private readonly HoanTienDAL _dal = new();

    public List<HoanTienDTO> LayDanhSach(string? trangThai = null) => _dal.LayDanhSach(QuyTac.RongThanhNull(trangThai));

    public HoanTienDTO DaHoan(string ma, string maNv, string? ghiChu)
    {
        var g = QuyTac.RongThanhNull(ghiChu);
        QuyTac.KiemTra(g == null || g.Length <= 255, "Ghi chú tối đa 255 ký tự");
        QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy phiếu hoàn tiền");
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.DanhDauDaHoan(ma, maNv, g, tx)), "Phiếu này đã hoàn tiền trước đó");
        return _dal.LayTheoMa(ma)!;
    }
}
