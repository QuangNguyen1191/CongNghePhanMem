using System.Collections.Concurrent;
using DAL;
using DTO;

namespace BLL;

public class TaiKhoanBLL
{
    private readonly TaiKhoanDAL _tkDal = new();
    private readonly NhanVienDAL _nvDal = new();
    private readonly KhachHangDAL _khDal = new();

    // Moi vai tro chi dang nhap dung noi: khach -> web ban hang, nhan vien -> web staff, admin -> app admin
    public PhienDangNhapDTO DangNhap(string tenDangNhap, string matKhau, NoiDangNhap noi)
    {
        var tk = _tkDal.LayTheoTenDangNhap(tenDangNhap.Trim());
        if (tk == null || !BCrypt.Net.BCrypt.Verify(matKhau, tk.MatKhau))
            throw new LoiNghiepVu("Sai tên đăng nhập hoặc mật khẩu");
        QuyTac.KiemTra(tk.TrangThai == "HOAT_DONG", "Tài khoản đã bị khóa, vui lòng liên hệ cửa hàng");

        var duocVao = noi switch
        {
            NoiDangNhap.WebBanHang => tk.VaiTro == "KHACH_HANG",
            NoiDangNhap.WebStaff => tk.VaiTro is "NHAN_VIEN" or "ADMIN",
            NoiDangNhap.AppAdmin => tk.VaiTro == "ADMIN",
            _ => false
        };
        QuyTac.KiemTra(duocVao, noi switch
        {
            NoiDangNhap.WebBanHang => "Tài khoản nhân viên không đăng nhập ở trang bán hàng",
            NoiDangNhap.WebStaff => "Chỉ nhân viên mới đăng nhập được web staff",
            _ => "Chỉ tài khoản quản lý (ADMIN) mới dùng được app admin"
        });

        var phien = new PhienDangNhapDTO { MaTk = tk.MaTk, TenDangNhap = tk.TenDangNhap, VaiTro = tk.VaiTro };
        if (tk.MaNv != null)
        {
            var nv = _nvDal.LayTheoMa(tk.MaNv)!;
            QuyTac.KiemTra(nv.TrangThai == "DANG_LAM", "Nhân viên đã nghỉ việc");
            (phien.MaNv, phien.HoTen, phien.ChucVu) = (nv.MaNv, nv.HoTen, nv.ChucVu);
        }
        else if (tk.MaKh != null)
        {
            (phien.MaKh, phien.HoTen) = (tk.MaKh, tk.HoTen ?? "");
        }
        return phien;
    }

    // Khach tu dang ky tren web. Neu SDT trung khach da mua tai quay / mua vang lai (chua co tai khoan)
    // thi gan tai khoan vao khach do: don hang, hoa don, diem cu hien ngay trong tai khoan.
    // v3: SDT bat buoc va phai da xac thuc OTP (sdtDaXacThuc lay tu phien khach vang lai) -> khong ai dang ky
    // bang SDT cua nguoi khac de xem don cua ho duoc. Ho so de trong thi lay tu ho so khach cu.
    public PhienDangNhapDTO DangKy(string tenDangNhap, string matKhau, KhachHangDTO kh, string? sdtDaXacThuc)
    {
        tenDangNhap = tenDangNhap.Trim();
        QuyTac.KiemTra(tenDangNhap.Length >= 4, "Tên đăng nhập phải có ít nhất 4 ký tự");
        KiemTraMatKhau(matKhau);
        QuyTac.KiemTra(!_tkDal.TonTaiTenDangNhap(tenDangNhap), "Tên đăng nhập đã tồn tại");
        kh.Email = QuyTac.RongThanhNull(kh.Email);
        kh.Sdt = QuyTac.ChuanHoaSdt(kh.Sdt) ?? sdtDaXacThuc;
        QuyTac.KiemTra(kh.Sdt != null, "Vui lòng nhập số điện thoại");
        QuyTac.KiemTra(sdtDaXacThuc != null && kh.Sdt == sdtDaXacThuc,
            "Vui lòng xác thực số điện thoại bằng mã OTP trước khi đăng ký");
        kh.SdtDaXacThuc = true;

        var khCu = _khDal.LayTheoSdt(kh.Sdt!);
        QuyTac.KiemTra(khCu?.MaTk == null, "Số điện thoại đã được đăng ký tài khoản, vui lòng đăng nhập");
        if (khCu != null)
        {
            kh.HoTen = string.IsNullOrWhiteSpace(kh.HoTen) ? khCu.HoTen : kh.HoTen;
            kh.Email ??= khCu.Email;
            kh.DiaChi = QuyTac.RongThanhNull(kh.DiaChi) ?? khCu.DiaChi;
            kh.TinhThanh = QuyTac.RongThanhNull(kh.TinhThanh) ?? khCu.TinhThanh;
            kh.NgaySinh ??= khCu.NgaySinh;
        }
        QuyTac.KiemTra(!string.IsNullOrWhiteSpace(kh.HoTen), "Vui lòng nhập họ tên");
        kh.HoTen = kh.HoTen.Trim();
        if (kh.Email != null)
            QuyTac.KiemTra(!_khDal.TrungEmail(kh.Email, khCu?.MaKh), "Email đã được sử dụng");

        DbHelper.GiaoDich(tx =>
        {
            var maTk = DbHelper.SinhMa("TAI_KHOAN", "ma_tk", "TK", 3, tx);
            _tkDal.Them(new TaiKhoanDTO
            {
                MaTk = maTk, TenDangNhap = tenDangNhap, MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhau, 11),
                VaiTro = "KHACH_HANG", TrangThai = "HOAT_DONG"
            }, tx);
            if (khCu != null)
            {
                kh.MaKh = khCu.MaKh;
                _khDal.GanTaiKhoan(kh, maTk, tx);
            }
            else
            {
                kh.MaKh = DbHelper.SinhMa("KHACH_HANG", "ma_kh", "KH", 3, tx);
                kh.MaTk = maTk;
                _khDal.Them(kh, tx);
            }
        });
        var phien = DangNhap(tenDangNhap, matKhau, NoiDangNhap.WebBanHang);
        phien.SoDonDaGan = khCu == null ? 0 : new DonDatHangDAL().LayTheoKhach(khCu.MaKh).Count;
        return phien;
    }

    public void DoiMatKhau(string maTk, string matKhauCu, string matKhauMoi)
    {
        var tk = QuyTac.CanCo(_tkDal.LayTheoMa(maTk), "Không tìm thấy tài khoản");
        QuyTac.KiemTra(BCrypt.Net.BCrypt.Verify(matKhauCu, tk.MatKhau), "Mật khẩu cũ không đúng");
        KiemTraMatKhau(matKhauMoi);
        _tkDal.DoiMatKhau(maTk, BCrypt.Net.BCrypt.HashPassword(matKhauMoi, 11));
    }

    // Thay cho "quen mat khau": quan ly / nhan vien dat lai mat khau tam, bao cho nguoi dung
    public string DatLaiMatKhau(string maTk)
    {
        QuyTac.CanCo(_tkDal.LayTheoMa(maTk), "Không tìm thấy tài khoản");
        var matKhauTam = Random.Shared.Next(100000, 999999).ToString();
        _tkDal.DoiMatKhau(maTk, BCrypt.Net.BCrypt.HashPassword(matKhauTam, 11));
        return matKhauTam;
    }

    public void KhoaMo(string maTk, bool khoa)
    {
        QuyTac.CanCo(_tkDal.LayTheoMa(maTk), "Không tìm thấy tài khoản");
        _tkDal.DoiTrangThai(maTk, khoa ? "KHOA" : "HOAT_DONG");
        QuenTrangThai(maTk);
    }

    // ---- Token JWT con han nhung tai khoan da bi khoa (hoac nhan vien nghi viec) thi khong duoc dung nua.
    // Nho ket qua 30 giay de khong ton 1 cau SQL cho moi request; khoa / mo tren web thi co hieu luc ngay,
    // khoa tu app admin (process khac) thi co hieu luc trong toi da 30 giay.
    private static readonly ConcurrentDictionary<string, (bool HoatDong, DateTime LucKiem)> _trangThai = new();
    public static TimeSpan ThoiGianNhoTrangThai { get; set; } = TimeSpan.FromSeconds(30);

    public bool ConHoatDong(string maTk)
    {
        if (_trangThai.TryGetValue(maTk, out var x) && DateTime.Now - x.LucKiem < ThoiGianNhoTrangThai) return x.HoatDong;
        bool hoatDong;
        using (NhatKySql.TamNgungDem())
            hoatDong = _tkDal.LayTheoMa(maTk)?.TrangThai == "HOAT_DONG";
        _trangThai[maTk] = (hoatDong, DateTime.Now);
        return hoatDong;
    }

    internal static void QuenTrangThai(string maTk) => _trangThai.TryRemove(maTk, out _);

    public List<TaiKhoanDTO> LayTatCa()
    {
        var ds = _tkDal.LayTatCa();
        ds.ForEach(t => t.MatKhau = "");               // khong dua hash ra ngoai
        return ds;
    }

    internal static void KiemTraMatKhau(string matKhau) =>
        QuyTac.KiemTra(matKhau.Length >= QuyTac.DoDaiMatKhauToiThieu,
            $"Mật khẩu phải có ít nhất {QuyTac.DoDaiMatKhauToiThieu} ký tự");
}
