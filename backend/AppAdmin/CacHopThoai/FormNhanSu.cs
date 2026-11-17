namespace AppAdmin.CacHopThoai;

// Them nhan vien kem tai khoan (NhanVienBLL.Them, 1 giao dich) / sua ho so nhan vien
public sealed class FormNhanVien : FormNhap
{
    private static readonly string[] CacChucVu = { "Giám đốc", "Quản lý cửa hàng", "Thu ngân", "Thủ kho", "Nhân viên bán hàng online", "Nhân viên bán hàng" };
    private readonly NhanVienDTO? _cu;
    private readonly TextBox _hoTen = GiaoDien.O(320);
    private readonly ComboBox _chucVu = GiaoDien.Chon(260, goTuDo: true);
    private readonly DateTimePicker _ngayVao = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly NumericUpDown _luong = GiaoDien.So(1_000_000_000);
    private readonly TextBox _sdt = GiaoDien.O(180);
    private readonly TextBox _email = GiaoDien.O(320);
    private readonly TextBox _tenDn = GiaoDien.O(220);
    private readonly TextBox _matKhau = new() { Width = 220, UseSystemPasswordChar = true };
    private readonly ComboBox _vaiTro = GiaoDien.Chon(220);

    public FormNhanVien(NhanVienDTO? nv) : base(nv == null ? "Thêm nhân viên và cấp tài khoản" : $"Sửa nhân viên {nv.MaNv}")
    {
        _cu = nv;
        _chucVu.Items.AddRange(CacChucVu.Cast<object>().ToArray());
        ThemO("Họ tên", _hoTen, true);
        ThemO("Chức vụ", _chucVu, ghiChu: "Web staff phân quyền theo chức vụ: \"thu ngân\" → bán hàng, \"online\" → đơn online, \"kho\" → nhập hàng");
        ThemO("Ngày vào làm", _ngayVao);
        ThemO("Lương (đ)", _luong);
        ThemO("Số điện thoại", _sdt);
        ThemO("Email", _email, true);
        if (nv == null)
        {
            ThemRong(GiaoDien.TieuDeMuc("Tài khoản đăng nhập"));
            ThemO("Tên đăng nhập", _tenDn, true, "Ít nhất 4 ký tự");
            ThemO("Mật khẩu", _matKhau, true, $"Ít nhất {QuyTac.DoDaiMatKhauToiThieu} ký tự, nhân viên tự đổi sau khi đăng nhập");
            Muc.Nap(_vaiTro, new[] { new Muc("NHAN_VIEN", "Nhân viên (đăng nhập web staff)"), new Muc("ADMIN", "Quản trị (app admin + web staff)") });
            ThemO("Vai trò", _vaiTro);
            _ngayVao.Value = DateTime.Today;
            _ngayVao.Checked = true;
            return;
        }
        _hoTen.Text = nv.HoTen;
        _chucVu.Text = nv.ChucVu;
        _ngayVao.Checked = nv.NgayVaoLam != null;
        if (nv.NgayVaoLam is { } d) _ngayVao.Value = d;
        _luong.Value = Math.Min(_luong.Maximum, nv.Luong ?? 0);
        _sdt.Text = nv.Sdt;
        _email.Text = nv.Email;
        ThemO("Tài khoản", GiaoDien.Nhan($"{nv.TenDangNhap} · {Nhan.Ten(Nhan.VaiTro, nv.VaiTro)} (khóa / đặt lại mật khẩu ở màn hình Tài khoản)", mau: GiaoDien.ChuPhu));
    }

    protected override bool Luu()
    {
        var nv = new NhanVienDTO
        {
            MaNv = _cu?.MaNv ?? "", MaTk = _cu?.MaTk ?? "", HoTen = _hoTen.Text, ChucVu = QuyTac.RongThanhNull(_chucVu.Text),
            NgayVaoLam = _ngayVao.Checked ? _ngayVao.Value.Date : null, Luong = _luong.Value, Sdt = QuyTac.RongThanhNull(_sdt.Text),
            Email = _email.Text.Trim(), TrangThai = _cu?.TrangThai ?? "DANG_LAM"
        };
        if (_cu != null) return ThaoTac.Chay("Sửa nhân viên", () => new NhanVienBLL().Sua(nv));
        if (!ThaoTac.Chay("Thêm nhân viên", () => new NhanVienBLL().Them(nv, _tenDn.Text, _matKhau.Text, Muc.Lay(_vaiTro) ?? "NHAN_VIEN"),
                out var moi)) return false;
        HopThoai.ThongBao($"Đã thêm nhân viên {moi.MaNv} - {moi.HoTen}, tài khoản \"{moi.TenDangNhap}\".");
        return true;
    }
}

// Khach hang: admin them khach tai quay (khong tai khoan) hoac sua ho so
public sealed class FormKhachHang : FormNhap
{
    private static readonly string[] CacTinh = { "TP Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng", "Bình Dương", "Đồng Nai", "Khánh Hòa" };
    private readonly KhachHangDTO? _cu;
    private readonly TextBox _hoTen = GiaoDien.O(320);
    private readonly TextBox _sdt = GiaoDien.O(180);
    private readonly TextBox _email = GiaoDien.O(320);
    private readonly DateTimePicker _ngaySinh = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly TextBox _diaChi = GiaoDien.O(360);
    private readonly ComboBox _tinh = GiaoDien.Chon(240, goTuDo: true);

    public KhachHangDTO? KetQua { get; private set; }

    public FormKhachHang(KhachHangDTO? k) : base(k == null ? "Thêm khách hàng (mua tại quầy)" : $"Sửa khách hàng {k.MaKh}")
    {
        _cu = k;
        _tinh.Items.AddRange(CacTinh.Cast<object>().ToArray());
        ThemO("Họ tên", _hoTen, true);
        ThemO("Số điện thoại", _sdt, ghiChu: k?.SdtDaXacThuc == true ? "SĐT đã xác thực OTP. Đổi số thì mất trạng thái xác thực" : "9 - 11 chữ số, mỗi SĐT chỉ 1 khách");
        ThemO("Email", _email);
        ThemO("Ngày sinh", _ngaySinh);
        ThemO("Địa chỉ", _diaChi);
        ThemO("Tỉnh / thành", _tinh, ghiChu: "Dùng cho truy vấn hợp / giao theo tỉnh (yêu cầu 6)");
        _ngaySinh.Checked = false;
        if (k == null) return;
        _hoTen.Text = k.HoTen;
        _sdt.Text = k.Sdt;
        _email.Text = k.Email;
        _ngaySinh.Checked = k.NgaySinh != null;
        if (k.NgaySinh is { } d) _ngaySinh.Value = d;
        _diaChi.Text = k.DiaChi;
        _tinh.Text = k.TinhThanh;
    }

    protected override bool Luu()
    {
        var k = new KhachHangDTO
        {
            MaKh = _cu?.MaKh ?? "", MaTk = _cu?.MaTk, HoTen = _hoTen.Text, Sdt = _sdt.Text, Email = _email.Text,
            NgaySinh = _ngaySinh.Checked ? _ngaySinh.Value.Date : null, DiaChi = _diaChi.Text, TinhThanh = _tinh.Text,
            HangThanhVien = _cu?.HangThanhVien ?? "DONG", DiemTichLuy = _cu?.DiemTichLuy ?? 0, SdtDaXacThuc = _cu?.SdtDaXacThuc ?? false
        };
        if (_cu == null)
        {
            if (!ThaoTac.Chay("Thêm khách hàng", () => new KhachHangBLL().ThemTaiQuay(k), out var moi)) return false;
            KetQua = moi;
            return true;
        }
        if (!ThaoTac.Chay("Sửa khách hàng", () => new KhachHangBLL().Sua(k))) return false;
        KetQua = k;
        return true;
    }
}

// Quan ly dieu chinh hang / diem bang tay (KhachHangBLL.SuaHangDiem)
public sealed class FormHangDiem : FormNhap
{
    private readonly KhachHangDTO _k;
    private readonly ComboBox _hang = GiaoDien.Chon(200);
    private readonly NumericUpDown _diem = GiaoDien.So(10_000_000);

    public FormHangDiem(KhachHangDTO k) : base($"Điều chỉnh hạng / điểm - {k.MaKh} {k.HoTen}", 520)
    {
        _k = k;
        Muc.Nap(_hang, Nhan.CacMuc(Nhan.HangThanhVien, null), k.HangThanhVien);
        _diem.Value = k.DiemTichLuy;
        ThemO("Hạng thành viên", _hang);
        ThemO("Điểm tích lũy", _diem, ghiChu: $"Hệ thống tự tính lại sau mỗi hóa đơn thanh toán ({QuyTac.TienMoiDiem:N0}đ = 1 điểm), "
                                             + "nên điều chỉnh tay có thể bị ghi đè ở lần mua sau");
    }

    protected override bool Luu() =>
        ThaoTac.Chay("Sửa hạng / điểm", () => new KhachHangBLL().SuaHangDiem(_k.MaKh, Muc.Lay(_hang) ?? "DONG", (int)_diem.Value));
}
