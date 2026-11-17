namespace AppAdmin.CacManHinh;

// Yeu cau 6: tim kiem nang cao nhieu dieu kien (=, <>, >, <, >=, <=, LIKE, IN), ghep AND (giao) / OR (hop),
// chay theo 2 cach (yeu cau 2): truy van moi (TimKiemBLL.Tim...) hoac loc tren danh sach da tai (TimKiemBLL.Loc...).
// Kem cac truy van HOP / GIAO viet san bang UNION / INTERSECT trong KhachHangDAL.
public sealed class TimKiemMH : ManHinh
{
    private sealed record Truong(string Ma, string Ten, string GoiY);

    private sealed class HangDieuKien : FlowLayoutPanel
    {
        public ComboBox Truong { get; } = GiaoDien.Chon(210);
        public ComboBox ToanTu { get; } = GiaoDien.Chon(110);
        public TextBox GiaTri { get; } = GiaoDien.O(260);
        public Label GoiY { get; } = new() { AutoSize = true, ForeColor = GiaoDien.ChuPhu, Font = GiaoDien.ChuNho, Margin = new Padding(0, 9, 8, 0) };
        public Button Xoa { get; } = GiaoDien.NutPhu("✕");

        public HangDieuKien()
        {
            AutoSize = true;
            WrapContents = false;
            Margin = new Padding(0, 0, 0, 2);
            Xoa.MinimumSize = new Size(32, 30);
            Controls.AddRange(new Control[] { Truong, ToanTu, GiaTri, Xoa, GoiY });
        }
    }

    private static readonly Dictionary<string, List<Truong>> CacTruong = new()
    {
        ["HOA_DON"] = new()
        {
            new("ma_hoa_don", "Mã hóa đơn", "vd HD00012"), new("ngay_lap", "Ngày lập", "dd/MM/yyyy"), new("tong_tien", "Tổng tiền", "số, vd 1000000"),
            new("tien_giam", "Tiền giảm", "số"), new("trang_thai", "Trạng thái", "DA_THANH_TOAN, CHO_THANH_TOAN, DA_HUY"),
            new("phuong_thuc_tt", "Phương thức TT", "TIEN_MAT, COD, CHUYEN_KHOAN, ONLINE"), new("ma_nv", "Mã nhân viên", "vd NV003"),
            new("ten_nhan_vien", "Tên nhân viên", "chữ"), new("ma_kh", "Mã khách hàng", "vd KH001"), new("ten_khach_hang", "Tên khách hàng", "chữ"),
            new("tinh_thanh", "Tỉnh / thành của khách", "vd Hà Nội"),
        },
        ["KHACH_HANG"] = new()
        {
            new("ma_kh", "Mã khách hàng", "vd KH001"), new("ho_ten", "Họ tên", "chữ"), new("sdt", "Số điện thoại", "chữ số"), new("email", "Email", "chữ"),
            new("ngay_sinh", "Ngày sinh", "dd/MM/yyyy"), new("hang_thanh_vien", "Hạng thành viên", "DONG, BAC, VANG, KIM_CUONG"),
            new("diem_tich_luy", "Điểm tích lũy", "số"), new("dia_chi", "Địa chỉ", "chữ"), new("tinh_thanh", "Tỉnh / thành", "vd TP Hồ Chí Minh"),
        },
        ["SAN_PHAM"] = new()
        {
            new("ma_sp", "Mã SP", "vd SP012"), new("ten_sp", "Tên sản phẩm", "chữ"), new("ma_loai_sp", "Mã loại", "vd LSP01"),
            new("ten_loai_sp", "Tên loại", "chữ"), new("chat_lieu", "Chất liệu", "chữ"), new("don_gia", "Đơn giá", "số"),
            new("so_luong", "Tồn kho", "số"), new("trang_thai", "Trạng thái", "DANG_BAN, NGUNG_BAN"), new("kich_co", "Kích cỡ", "S, M, L..."),
            new("mau_sac", "Màu sắc", "chữ"),
        },
    };

    // Vi du de demo nhanh tung kieu ghep
    private static readonly (string Ten, string DoiTuong, bool Va, (string, string, string)[] Dk)[] ViDu =
    {
        ("HĐ đã thanh toán từ 1 triệu (AND)", "HOA_DON", true, new[] { ("trang_thai", "=", "DA_THANH_TOAN"), ("tong_tien", ">=", "1000000") }),
        ("HĐ năm nay trả COD hoặc chuyển khoản (IN)", "HOA_DON", true, new[] { ("ngay_lap", ">=", $"01/01/{DateTime.Today.Year}"), ("phuong_thuc_tt", "IN", "COD, CHUYEN_KHOAN") }),
        ("Khách ở TP HCM HOẶC hạng Vàng (OR = hợp)", "KHACH_HANG", false, new[] { ("tinh_thanh", "=", "TP Hồ Chí Minh"), ("hang_thanh_vien", "=", "VANG") }),
        ("Khách ở Hà Nội VÀ trên 100 điểm (AND = giao)", "KHACH_HANG", true, new[] { ("tinh_thanh", "=", "Hà Nội"), ("diem_tich_luy", ">", "100") }),
        ("SP giá 300k - 600k còn hàng", "SAN_PHAM", true, new[] { ("don_gia", ">=", "300000"), ("don_gia", "<=", "600000"), ("so_luong", ">", "0") }),
        ("SP tên có \"Áo\" khác size M", "SAN_PHAM", true, new[] { ("ten_sp", "LIKE", "Áo"), ("kich_co", "<>", "M") }),
    };

    private readonly ComboBox _doiTuong = GiaoDien.Chon(170);
    private readonly ComboBox _viDu = GiaoDien.Chon(330);
    private readonly RadioButton _va = new() { Text = "Thỏa TẤT CẢ (AND - giao)", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 10, 0) };
    private readonly RadioButton _hoac = new() { Text = "Thỏa ÍT NHẤT 1 (OR - hợp)", AutoSize = true, Margin = new Padding(0, 6, 10, 0) };
    private readonly RadioButton _moi = new() { Text = "Truy vấn mới", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 10, 0) };
    private readonly RadioButton _taiSuDung = new() { Text = "Tái sử dụng danh sách đã tải", AutoSize = true, Margin = new Padding(0, 6, 10, 0) };
    private readonly FlowLayoutPanel _dieuKien = new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Label _ketQua = new() { Dock = DockStyle.Top, Height = 44, ForeColor = GiaoDien.ChuPhu };
    private readonly Panel _vungKetQua = new() { Dock = DockStyle.Fill };
    private readonly Luoi<HoaDonDTO> _hd = new();
    private readonly Luoi<KhachHangDTO> _kh = new();
    private readonly Luoi<SanPhamDTO> _sp = new();
    private readonly ComboBox _tinhA = GiaoDien.Chon(170, goTuDo: true);
    private readonly ComboBox _tinhB = GiaoDien.Chon(170, goTuDo: true);
    private readonly Dictionary<string, object> _daTai = new();

    public TimKiemMH() : base("Tìm kiếm nâng cao", "Ghép nhiều điều kiện bằng AND (giao) hoặc OR (hợp). Giá trị luôn gửi bằng tham số SQL, tên trường chỉ lấy trong danh sách cho phép.")
    {
        Nut("Tìm", Keys.F5, Tim, chinh: true);
        Phim("Tìm", Keys.Control | Keys.Return, Tim);
        Nut("Thêm điều kiện", Keys.Control | Keys.N, () => ThemHang());
        Nut("Xóa hết điều kiện", Keys.Control | Keys.Delete, () => { _dieuKien.Controls.Clear(); ThemHang(); });
        Nut("Đổi cách chạy", Keys.F3, () => (_moi.Checked ? _taiSuDung : _moi).Checked = true);
        Nut("Tải lại danh sách gốc", Keys.Control | Keys.Shift | Keys.R, () => { _daTai.Clear(); HopThoai.ThongBao("Lần tìm tái sử dụng kế tiếp sẽ tải lại danh sách gốc."); });
        Nut("Xuất Excel kết quả", Keys.Control | Keys.E, XuatExcel);

        Muc.Nap(_doiTuong, new[] { new Muc("HOA_DON", "Hóa đơn"), new Muc("KHACH_HANG", "Khách hàng"), new Muc("SAN_PHAM", "Sản phẩm") });
        Muc.Nap(_viDu, new[] { new Muc(null, "(chọn ví dụ có sẵn)") }.Concat(ViDu.Select((v, i) => new Muc(i.ToString(), v.Ten))));
        var hang1 = GiaoDien.HangNgang();
        hang1.Controls.AddRange(new Control[] { GiaoDien.Nhan("Tìm trong"), _doiTuong, GiaoDien.Nhan("Ví dụ"), _viDu });
        var nhomGhep = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 24, 0) };
        nhomGhep.Controls.AddRange(new Control[] { GiaoDien.Nhan("Ghép:"), _va, _hoac });
        var nhomCach = new FlowLayoutPanel { AutoSize = true };
        nhomCach.Controls.AddRange(new Control[] { GiaoDien.Nhan("Cách chạy:"), _moi, _taiSuDung });
        var hang2 = GiaoDien.HangNgang();
        hang2.Controls.AddRange(new Control[] { nhomGhep, nhomCach });

        _hd.Them("Mã HĐ", h => h.MaHoaDon, 80).Ngay("Ngày lập", h => h.NgayLap).Them("Khách hàng", h => h.TenKhachHang ?? "Khách vãng lai", 160, fill: true)
            .Them("Tỉnh", h => h.TinhThanhKhach, 120).Them("Nhân viên", h => h.TenNhanVien, 140).Them("Thanh toán", h => Nhan.Ten(Nhan.PhuongThuc, h.PhuongThucTt), 110)
            .Tien("Giảm", h => h.TienGiam, 90).Tien("Tổng tiền", h => h.TongTien, 110).TrangThai("Trạng thái", h => h.TrangThai, Nhan.TrangThaiHoaDon, 110);
        _hd.MoDong += h => MoManHinh?.Invoke("HoaDon", h.MaHoaDon);
        _kh.Them("Mã KH", k => k.MaKh, 70).Them("Họ tên", k => k.HoTen, 170, fill: true).Them("SĐT", k => k.Sdt, 105).Them("Email", k => k.Email, 170)
            .Them("Tỉnh / thành", k => k.TinhThanh, 130).TrangThai("Hạng", k => k.HangThanhVien, Nhan.HangThanhVien, 85).So("Điểm", k => k.DiemTichLuy, 70);
        _sp.Them("Mã SP", s => s.MaSp, 70).Them("Tên sản phẩm", s => s.TenSp, 220, fill: true).Them("Loại", s => s.TenLoaiSp, 110).Them("Màu", s => s.MauSac, 100)
            .Them("Size", s => s.KichCo, 55).Tien("Đơn giá", s => s.DonGia, 100).So("Tồn", s => s.SoLuong, 60)
            .TrangThai("Trạng thái", s => s.TrangThai, Nhan.TrangThaiSanPham, 95);

        // ---- hop / giao viet san
        foreach (var cb in new[] { _tinhA, _tinhB })
            cb.Items.AddRange(new object[] { "TP Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng" });
        _tinhA.Text = "TP Hồ Chí Minh";
        _tinhB.Text = "Hà Nội";
        var hop = GiaoDien.NutPhu("A ∪ B: khách ở tỉnh A hoặc tỉnh B (UNION)");
        hop.Click += (_, _) => ChayHopGiao("Khách ở A ∪ B (UNION)", b => b.TheoHaiTinhHop(_tinhA.Text.Trim(), _tinhB.Text.Trim()));
        var giao = GiaoDien.NutPhu("Khách ở A ∩ có đơn giao tới B (INTERSECT)");
        giao.Click += (_, _) => ChayHopGiao("Khách ở A ∩ có đơn giao tới B (INTERSECT)", b => b.OTinhACoDonGiaoTinhB(_tinhA.Text.Trim(), _tinhB.Text.Trim()));
        var giao2 = GiaoDien.NutPhu("Vừa mua tại quầy ∩ vừa đặt online (INTERSECT)");
        giao2.Click += (_, _) => ChayHopGiao("Vừa mua tại quầy ∩ vừa đặt online (INTERSECT)", b => b.VuaMuaTaiQuayVuaDatOnline());
        var hangHop = GiaoDien.HangNgang();
        hangHop.Controls.AddRange(new Control[] { GiaoDien.Nhan("Tỉnh A"), _tinhA, GiaoDien.Nhan("Tỉnh B"), _tinhB, hop, giao, giao2 });

        var tren = GiaoDien.Khung(DockStyle.Top, 12);
        tren.AutoSize = true;
        tren.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        tren.Controls.Add(hangHop);
        tren.Controls.Add(GiaoDien.TieuDeMuc("Truy vấn hợp / giao viết sẵn (khách hàng)"));
        tren.Controls.Add(_dieuKien);
        tren.Controls.Add(GiaoDien.TieuDeMuc("Điều kiện (tối đa 10)"));
        tren.Controls.Add(hang2);
        tren.Controls.Add(hang1);

        var duoi = GiaoDien.Khung(dem: 8);
        duoi.Controls.Add(_vungKetQua);
        duoi.Controls.Add(_ketQua);
        NoiDung.Controls.Add(duoi);
        NoiDung.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        NoiDung.Controls.Add(tren);

        _doiTuong.SelectedIndexChanged += (_, _) =>
        {
            _dieuKien.Controls.Clear();
            ThemHang();
            HienBang();
        };
        _viDu.SelectedIndexChanged += (_, _) => ApViDu();
        ThemHang();
        HienBang();
    }

    // F5 / lan dau mo man hinh: chi tim khi da co dieu kien
    public override void LamMoi()
    {
        if (DieuKien().DieuKiens.Count > 0) Tim();
    }

    public override Control? OTim => _dieuKien.Controls.OfType<HangDieuKien>().FirstOrDefault()?.GiaTri;

    private string DoiTuong => Muc.Lay(_doiTuong) ?? "HOA_DON";

    private HangDieuKien? ThemHang(string? truong = null, string toanTu = "=", string giaTri = "")
    {
        if (_dieuKien.Controls.Count >= 10)
        {
            HopThoai.Canh("Tối đa 10 điều kiện.");
            return null;
        }
        var h = new HangDieuKien();
        var ds = CacTruong[DoiTuong];
        Muc.Nap(h.Truong, ds.Select(t => new Muc(t.Ma, $"{t.Ten}  ({t.Ma})")), truong);
        Muc.Nap(h.ToanTu, TimKiemBLL.CacToanTu.Select(t => new Muc(t, t)), toanTu);
        h.GiaTri.Text = giaTri;
        void CapNhatGoiY()
        {
            var t = ds.FirstOrDefault(x => x.Ma == Muc.Lay(h.Truong));
            h.GoiY.Text = (t?.GoiY ?? "") + (Muc.Lay(h.ToanTu) == "IN" ? " · nhiều giá trị cách nhau dấu phẩy" : "")
                          + (Muc.Lay(h.ToanTu) == "LIKE" ? " · chứa chuỗi" : "");
        }
        h.Truong.SelectedIndexChanged += (_, _) => CapNhatGoiY();
        h.ToanTu.SelectedIndexChanged += (_, _) => CapNhatGoiY();
        h.Xoa.Click += (_, _) =>
        {
            _dieuKien.Controls.Remove(h);
            h.Dispose();
            if (_dieuKien.Controls.Count == 0) ThemHang();
        };
        h.GiaTri.KeyDown += (_, e) => { if (e.KeyCode == Keys.Return) { Tim(); e.SuppressKeyPress = true; } };
        CapNhatGoiY();
        _dieuKien.Controls.Add(h);
        return h;
    }

    private void ApViDu()
    {
        if (!int.TryParse(Muc.Lay(_viDu), out var i)) return;
        var v = ViDu[i];
        Muc.Nap(_doiTuong, _doiTuong.Items.Cast<Muc>().ToList(), v.DoiTuong);  // doi doi tuong -> xoa dieu kien cu
        _dieuKien.Controls.Clear();
        foreach (var (t, op, g) in v.Dk) ThemHang(t, op, g);
        (v.Va ? _va : _hoac).Checked = true;
        HienBang();
        Tim();
    }

    private TimKiemNangCaoDTO DieuKien() => new()
    {
        KhopTatCa = _va.Checked,
        DieuKiens = _dieuKien.Controls.OfType<HangDieuKien>().Where(h => h.GiaTri.Text.Trim().Length > 0)
            .Select(h => new DieuKienTimDTO { Truong = Muc.Lay(h.Truong) ?? "", ToanTu = Muc.Lay(h.ToanTu) ?? "=", GiaTri = h.GiaTri.Text.Trim() }).ToList()
    };

    private void HienBang()
    {
        _vungKetQua.Controls.Clear();
        _vungKetQua.Controls.Add(DoiTuong switch { "KHACH_HANG" => _kh.Grid, "SAN_PHAM" => _sp.Grid, _ => _hd.Grid });
    }

    // Danh sach goc cho cach tai su dung: tai 1 lan, cac lan tim sau 0 cau SQL
    private List<T> Goc<T>(string khoa, Func<List<T>> tai)
    {
        if (_daTai.TryGetValue(khoa, out var o)) return (List<T>)o;
        var ds = tai();
        _daTai[khoa] = ds;
        return ds;
    }

    private void Tim()
    {
        var dk = DieuKien();
        if (dk.DieuKiens.Count == 0)
        {
            HopThoai.Canh("Nhập giá trị cho ít nhất 1 điều kiện.");
            return;
        }
        var bll = new TimKiemBLL();
        var taiSuDung = _taiSuDung.Checked;
        int soDong = 0;
        var ok = DoiTuong switch
        {
            "KHACH_HANG" => ThaoTac.Chay("Tìm nâng cao khách hàng", () => taiSuDung
                ? bll.LocKhachHang(Goc("KH", () => new KhachHangBLL().LayTatCa()), dk) : bll.TimKhachHang(dk), out var kh) && Nap(_kh, kh, out soDong),
            "SAN_PHAM" => ThaoTac.Chay("Tìm nâng cao sản phẩm", () => taiSuDung
                ? bll.LocSanPham(Goc("SP", () => new SanPhamBLL().LayTatCa()), dk) : bll.TimSanPham(dk), out var sp) && Nap(_sp, sp, out soDong),
            _ => ThaoTac.Chay("Tìm nâng cao hóa đơn", () => taiSuDung
                ? bll.LocHoaDon(Goc("HD", () => new HoaDonBLL().LayDanhSach()), dk) : bll.TimHoaDon(dk), out var hd) && Nap(_hd, hd, out soDong),
        };
        if (!ok) return;
        var kq = ThaoTac.Cuoi!;
        var where = string.Join(dk.KhopTatCa ? "  AND  " : "  OR  ",
            dk.DieuKiens.Select(d => d.ToanTu == "IN" ? $"{d.Truong} IN ({d.GiaTri})" : $"{d.Truong} {d.ToanTu} '{d.GiaTri}'"));
        _ketQua.Text = $"{soDong:N0} dòng · {kq.SoCau} câu SQL · {kq.ThoiGianMs} ms · "
                       + (taiSuDung ? "lọc trên danh sách đã tải (TimKiemBLL.Loc...)" : "câu SELECT mới (TimKiemBLL.Tim...)")
                       + $"\nĐiều kiện: {where}";
        _ketQua.ForeColor = GiaoDien.ChuChinh;
    }

    private static bool Nap<T>(Luoi<T> luoi, List<T> ds, out int soDong) where T : class
    {
        luoi.Nap(ds);
        soDong = ds.Count;
        return true;
    }

    private void ChayHopGiao(string ten, Func<KhachHangBLL, List<KhachHangDTO>> lam)
    {
        if (!ThaoTac.Chay(ten, () => lam(new KhachHangBLL()), out var ds)) return;
        _dieuKien.Controls.Clear();
        Muc.Nap(_doiTuong, _doiTuong.Items.Cast<Muc>().ToList(), "KHACH_HANG");
        HienBang();
        _kh.Nap(ds);
        var kq = ThaoTac.Cuoi!;
        _ketQua.Text = $"{ds.Count:N0} khách · {kq.SoCau} câu SQL · {kq.ThoiGianMs} ms · {ten}\nTỉnh A = {_tinhA.Text}, tỉnh B = {_tinhB.Text}"
                       + (kq.ViTri != null ? $" · câu SQL ở {kq.ViTri}" : "");
        _ketQua.ForeColor = GiaoDien.ChuChinh;
    }

    private void XuatExcel()
    {
        var excel = new ExcelBLL();
        switch (DoiTuong)
        {
            case "KHACH_HANG":
                var kh = _kh.DuLieu;
                if (ThaoTac.Chay("Xuất Excel kết quả", () => excel.XuatKhachHang(kh), out var f1)) XuatFile.LuuExcel(FindForm(), f1, "TimKiem_KhachHang");
                break;
            case "SAN_PHAM":
                var sp = _sp.DuLieu;
                if (ThaoTac.Chay("Xuất Excel kết quả", () => excel.XuatSanPham(sp), out var f2)) XuatFile.LuuExcel(FindForm(), f2, "TimKiem_SanPham");
                break;
            default:
                var hd = _hd.DuLieu;
                if (ThaoTac.Chay("Xuất Excel kết quả", () => excel.XuatHoaDon(hd), out var f3)) XuatFile.LuuExcel(FindForm(), f3, "TimKiem_HoaDon");
                break;
        }
    }
}
