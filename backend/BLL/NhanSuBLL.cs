using DAL;
using DTO;

namespace BLL;

public class NhanVienBLL
{
    private readonly NhanVienDAL _nvDal = new();
    private readonly TaiKhoanDAL _tkDal = new();

    public List<NhanVienDTO> LayTatCa() => _nvDal.LayTatCa();

    public NhanVienDTO LayTheoMa(string ma) => QuyTac.CanCo(_nvDal.LayTheoMa(ma), "Không tìm thấy nhân viên");

    // Them nhan vien kem cap tai khoan trong cung 1 giao dich
    public NhanVienDTO Them(NhanVienDTO nv, string tenDangNhap, string matKhau, string vaiTro)
    {
        KiemTra(nv);
        QuyTac.KiemTra(vaiTro is "ADMIN" or "NHAN_VIEN", "Vai trò nhân viên phải là ADMIN hoặc NHAN_VIEN");
        QuyTac.KiemTra(tenDangNhap.Trim().Length >= 4, "Tên đăng nhập phải có ít nhất 4 ký tự");
        TaiKhoanBLL.KiemTraMatKhau(matKhau);
        QuyTac.KiemTra(!_tkDal.TonTaiTenDangNhap(tenDangNhap.Trim()), "Tên đăng nhập đã tồn tại");
        QuyTac.KiemTra(!_nvDal.TrungEmail(nv.Email), "Email đã được dùng cho nhân viên khác");

        DbHelper.GiaoDich(tx =>
        {
            nv.MaTk = DbHelper.SinhMa("TAI_KHOAN", "ma_tk", "TK", 3, tx);
            _tkDal.Them(new TaiKhoanDTO
            {
                MaTk = nv.MaTk, TenDangNhap = tenDangNhap.Trim(), MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhau, 11),
                VaiTro = vaiTro, TrangThai = "HOAT_DONG"
            }, tx);
            nv.MaNv = DbHelper.SinhMa("NHAN_VIEN", "ma_nv", "NV", 3, tx);
            nv.TrangThai = "DANG_LAM";
            _nvDal.Them(nv, tx);
        });
        return LayTheoMa(nv.MaNv);
    }

    public void Sua(NhanVienDTO nv)
    {
        KiemTra(nv);
        QuyTac.KiemTra(!_nvDal.TrungEmail(nv.Email, nv.MaNv), "Email đã được dùng cho nhân viên khác");
        QuyTac.CanTimThay(_nvDal.Sua(nv) == 1, "Không tìm thấy nhân viên");
    }

    // Nghi viec: giu ho so (hoa don cu van tro toi), khoa tai khoan
    public void NghiViec(string maNv)
    {
        var nv = LayTheoMa(maNv);
        DbHelper.GiaoDich(tx =>
        {
            _nvDal.DoiTrangThai(maNv, "NGHI_VIEC", tx);
            _tkDal.DoiTrangThai(nv.MaTk, "KHOA", tx);
        });
        TaiKhoanBLL.QuenTrangThai(nv.MaTk);
    }

    private static void KiemTra(NhanVienDTO nv)
    {
        nv.HoTen = nv.HoTen.Trim();
        QuyTac.KiemTra(nv.HoTen.Length > 0, "Vui lòng nhập họ tên");
        QuyTac.KiemTra(nv.Email.Contains('@'), "Email không hợp lệ");
        QuyTac.KiemTra(nv.Luong is null or >= 0, "Lương không được âm");
    }
}

public class KhachHangBLL
{
    private readonly KhachHangDAL _dal = new();

    public List<KhachHangDTO> LayTatCa() => _dal.LayTatCa();

    public KhachHangDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy khách hàng");

    public KhachHangDTO? TimTheoSdt(string sdt) => _dal.LayTheoSdt(sdt.Trim());

    // v3: web staff xem lich su 1 SDT truoc khi xac nhan don (nhat la don khach vang lai)
    // maKh (tuy chon): don cua khach co tai khoan nhung nhap SDT nguoi nhan khac SDT tai khoan
    // thi van xem duoc lich su cua khach do, khong bao nham "khach moi".
    public RuiRoKhachDTO RuiRo(string sdt, string? maKh = null)
    {
        sdt = QuyTac.ChuanHoaSdt(sdt) ?? "";
        var kh = _dal.LayTheoSdt(sdt) ?? (string.IsNullOrWhiteSpace(maKh) ? null : _dal.LayTheoMa(maKh.Trim()));
        if (kh == null)
            return new RuiRoKhachDTO { Sdt = sdt, DuocDatCod = true, MucRuiRo = "TRUNG_BINH", GhiChu = "Khách mới, chưa có lịch sử mua hàng" };
        var r = new DonDatHangDAL().DemLichSu(kh.MaKh);
        (r.Sdt, r.MaKh, r.HoTen, r.CoTaiKhoan, r.SdtDaXacThuc) = (sdt, kh.MaKh, kh.HoTen, kh.MaTk != null, kh.SdtDaXacThuc);
        r.DuocDatCod = r.SoLanBomHang < QuyTac.SoLanBomChanCod;
        (r.MucRuiRo, r.GhiChu) = r switch
        {
            { SoLanBomHang: >= QuyTac.SoLanBomChanCod } =>
                ("CAO", $"{r.SoLanBomHang} lần không nhận hàng. Không cho COD, nên gọi xác nhận trước khi giao"),
            { SoLanBomHang: > 0 } or { SoDonBiTuChoi: >= 2 } =>
                ("TRUNG_BINH", "Có lịch sử không nhận hàng / bị từ chối, nên gọi xác nhận"),
            { SoDonHoanTat: 0 } => ("TRUNG_BINH", "Chưa có đơn hoàn tất nào, nên gọi xác nhận"),
            _ => ("THAP", $"Đã nhận hàng thành công {r.SoDonHoanTat} đơn")
        };
        return r;
    }

    // Nhan vien them khach tai quay (khong co tai khoan)
    public KhachHangDTO ThemTaiQuay(KhachHangDTO k)
    {
        KiemTra(k);
        QuyTac.KiemTra(k.Sdt == null || _dal.LayTheoSdt(k.Sdt) == null, "Số điện thoại đã có khách hàng");
        return DbHelper.GiaoDich(tx =>
        {
            k.MaKh = DbHelper.SinhMa("KHACH_HANG", "ma_kh", "KH", 3, tx);
            k.MaTk = null;
            _dal.Them(k, tx);
            return _dal.LayTheoMa(k.MaKh, tx)!;
        });
    }

    // Khach tu sua ho so tren web, hoac nhan vien sua
    public void Sua(KhachHangDTO k)
    {
        KiemTra(k);
        var cung = k.Sdt == null ? null : _dal.LayTheoSdt(k.Sdt);
        QuyTac.KiemTra(cung == null || cung.MaKh == k.MaKh, "Số điện thoại đã có khách hàng khác");
        QuyTac.CanTimThay(_dal.Sua(k) == 1, "Không tìm thấy khách hàng");
    }

    // Quan ly dieu chinh hang / diem bang tay
    public void SuaHangDiem(string ma, string hang, int diem)
    {
        QuyTac.KiemTra(hang is "DONG" or "BAC" or "VANG" or "KIM_CUONG", "Hạng thành viên không hợp lệ");
        QuyTac.KiemTra(diem >= 0, "Điểm không được âm");
        QuyTac.CanTimThay(_dal.SuaHangDiem(ma, hang, diem) == 1, "Không tìm thấy khách hàng");
    }

    // Yeu cau 6 - HOP (UNION / OR): khach o tinh A hoac tinh B
    public List<KhachHangDTO> TheoHaiTinhHop(string tinhA, string tinhB) => _dal.TheoTinhThanhHop(tinhA, tinhB);

    // Yeu cau 6 - GIAO (INTERSECT / AND)
    public List<KhachHangDTO> OTinhACoDonGiaoTinhB(string tinhA, string tinhB) => _dal.OTinhACoDonGiaoTinhB(tinhA, tinhB);

    public List<KhachHangDTO> VuaMuaTaiQuayVuaDatOnline() => _dal.VuaMuaTaiQuayVuaDatOnline();

    private void KiemTra(KhachHangDTO k)
    {
        k.HoTen = k.HoTen.Trim();
        QuyTac.KiemTra(k.HoTen.Length > 0, "Vui lòng nhập họ tên");
        k.Sdt = QuyTac.RongThanhNull(k.Sdt);
        k.Email = QuyTac.RongThanhNull(k.Email);
        k.DiaChi = QuyTac.RongThanhNull(k.DiaChi);
        k.TinhThanh = QuyTac.RongThanhNull(k.TinhThanh);
        QuyTac.KiemTra(k.Sdt == null || k.Sdt.All(char.IsDigit) && k.Sdt.Length is >= 9 and <= 11, "Số điện thoại không hợp lệ");
        if (k.Email != null)
            QuyTac.KiemTra(!_dal.TrungEmail(k.Email, k.MaKh), "Email đã được sử dụng");
    }
}
