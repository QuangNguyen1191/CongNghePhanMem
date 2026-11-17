using System.Globalization;
using System.Text;
using AppAdmin.CacManHinh;
using AppAdmin.CacHopThoai;

namespace AppAdmin;

// Cua so chinh: menu ben trai (13 chuc nang + bao cao, tim kiem, nhat ky SQL), vung man hinh, thanh trang thai SQL.
public sealed class FormChinh : Form
{
    public sealed record MucMenu(string Khoa, string Ten, string Nhom, Keys Phim, Func<HaTang.ManHinh> Tao);

    private readonly List<MucMenu> _menu = new()
    {
        new("TongQuan", "Tổng quan", "TỔNG QUAN", Keys.Control | Keys.D1, () => new TongQuanMH()),
        new("DonHang", "Đơn đặt hàng", "BÁN HÀNG", Keys.Control | Keys.D2, () => new DonHangMH()),
        new("HoaDon", "Hóa đơn", "BÁN HÀNG", Keys.Control | Keys.D3, () => new HoaDonMH()),
        new("DoiTra", "Đổi trả", "BÁN HÀNG", Keys.None, () => new DoiTraMH()),
        new("HoanTien", "Hoàn tiền", "BÁN HÀNG", Keys.None, () => new HoanTienMH()),
        new("SanPham", "Sản phẩm & tồn kho", "KHO HÀNG", Keys.Control | Keys.D4, () => new SanPhamMH()),
        new("LoaiSp", "Loại sản phẩm", "KHO HÀNG", Keys.None, () => new LoaiSpMH()),
        new("NhaCungCap", "Nhà cung cấp", "KHO HÀNG", Keys.None, () => new NhaCungCapMH()),
        new("PhieuNhap", "Nhập hàng", "KHO HÀNG", Keys.Control | Keys.D5, () => new PhieuNhapMH()),
        new("KhachHang", "Khách hàng & giỏ hàng", "CON NGƯỜI", Keys.Control | Keys.D6, () => new KhachHangMH()),
        new("NhanVien", "Nhân viên", "CON NGƯỜI", Keys.Control | Keys.D7, () => new NhanVienMH()),
        new("TaiKhoan", "Tài khoản", "CON NGƯỜI", Keys.None, () => new TaiKhoanMH()),
        new("KhuyenMai", "Khuyến mãi", "KINH DOANH", Keys.Control | Keys.D8, () => new KhuyenMaiMH()),
        new("ThongKe", "Báo cáo thống kê", "KINH DOANH", Keys.Control | Keys.D9, () => new ThongKeMH()),
        new("TimKiem", "Tìm kiếm nâng cao", "KINH DOANH", Keys.Control | Keys.D0, () => new TimKiemMH()),
        new("NhatKySql", "Nhật ký SQL", "HỆ THỐNG", Keys.Control | Keys.L, () => new NhatKySqlMH()),
    };

    private readonly Dictionary<string, HaTang.ManHinh> _daTao = new();
    private readonly Dictionary<string, Button> _nutMenu = new();
    private readonly Panel _vung = new() { Dock = DockStyle.Fill, BackColor = GiaoDien.Nen };
    private readonly ToolStripStatusLabel _sql = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft, IsLink = false };
    private readonly ToolStripStatusLabel _tong = new() { BorderSides = ToolStripStatusLabelBorderSides.Left };
    private HaTang.ManHinh? _hien;
    private string? _khoaHien;
    private int _tongSql;

    public bool DangXuat { get; private set; }

    public FormChinh()
    {
        Text = $"{CauHinhApp.TenCuaHang} - App quản trị  ·  {Phien.HoTen}";
        Font = GiaoDien.Chu;
        BackColor = GiaoDien.Nen;
        MinimumSize = new Size(1100, 680);
        Size = new Size(1360, 820);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;

        var trangThai = new StatusStrip { BackColor = Color.White, SizingGrip = true, Font = GiaoDien.ChuNho };
        var nguoi = new ToolStripStatusLabel($"{Phien.HoTen} ({Phien.Hien?.TenDangNhap})") { BorderSides = ToolStripStatusLabelBorderSides.Left };
        var phimTat = new ToolStripStatusLabel("F1: phím tắt · Ctrl+K: mở nhanh") { BorderSides = ToolStripStatusLabelBorderSides.Left };
        _sql.Text = "Yêu cầu 1: mỗi thao tác hiện số câu SQL và dòng code DAL đã chạy câu cuối cùng";
        _sql.Click += (_, _) => Mo("NhatKySql");
        trangThai.Items.AddRange(new ToolStripItem[] { _sql, _tong, nguoi, phimTat });

        Controls.Add(_vung);
        Controls.Add(TaoMenuTrai());
        Controls.Add(trangThai);

        ThaoTac.DaXong += KhiXongThaoTac;
        HaTang.ManHinh.MoManHinh = Mo;
        Shown += (_, _) => Mo("TongQuan");
        FormClosed += (_, _) =>
        {
            ThaoTac.DaXong -= KhiXongThaoTac;
            HaTang.ManHinh.MoManHinh = null;
        };
    }

    private void KhiXongThaoTac(ThaoTac.KetQua kq)
    {
        _tongSql += kq.SoCau;
        _sql.Text = $"{kq.Ten}: {kq.SoCau} câu SQL" + (kq.ViTri != null ? $"  ·  câu cuối ở {kq.ViTri}" : "") + $"  ·  {kq.ThoiGianMs} ms";
        _sql.ForeColor = kq.ThanhCong ? GiaoDien.ChuChinh : GiaoDien.Do;
        _tong.Text = $"Tổng phiên: {_tongSql:N0} câu SQL";
    }

    // ------------------------------------------------------------------ menu trai
    private Control TaoMenuTrai()
    {
        var trai = new Panel { Dock = DockStyle.Left, Width = 236, BackColor = GiaoDien.Den };
        var logo = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = GiaoDien.Den };
        logo.Paint += (_, e) =>
        {
            using var co = new SolidBrush(GiaoDien.Do);
            e.Graphics.FillRectangle(co, 18, 16, 32, 32);
            using var chu = new Font(GiaoDien.TenFont, 14f, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, CauHinhApp.TenCuaHang.Length > 0 ? CauHinhApp.TenCuaHang[..1] : "U", chu,
                new Rectangle(18, 16, 32, 32), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, "ADMIN", chu, new Point(60, 20), Color.White);
        };

        var danhSach = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
            BackColor = GiaoDien.Den, Padding = new Padding(0, 4, 0, 8)
        };
        string? nhom = null;
        foreach (var m in _menu)
        {
            if (m.Nhom != nhom)
            {
                nhom = m.Nhom;
                danhSach.Controls.Add(new Label
                {
                    Text = nhom, Width = 220, Height = 28, ForeColor = Color.FromArgb(0x9A, 0x9A, 0x9A), Font = GiaoDien.ChuNho,
                    TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(16, 0, 0, 2), Margin = new Padding(0, 6, 0, 0)
                });
            }
            danhSach.Controls.Add(TaoNutMenu(m));
        }

        var duoi = new Panel { Dock = DockStyle.Bottom, Height = 104, BackColor = GiaoDien.DenNhat, Padding = new Padding(16, 10, 10, 8) };
        var ten = new Label
        {
            Text = Phien.HoTen, Dock = DockStyle.Top, Height = 22, ForeColor = Color.White, Font = GiaoDien.ChuDam, AutoEllipsis = true
        };
        var chucVu = new Label
        {
            Text = $"{Phien.Hien?.ChucVu ?? "Quản trị"} · {Phien.Hien?.MaNv}", Dock = DockStyle.Top, Height = 20,
            ForeColor = Color.FromArgb(0xB0, 0xB0, 0xB0), Font = GiaoDien.ChuNho
        };
        var hangNut = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, BackColor = GiaoDien.DenNhat, WrapContents = false };
        var doiMk = NutNhoToi("Đổi mật khẩu");
        doiMk.Click += (_, _) => { using var f = new FormDoiMatKhau(); f.ShowDialog(this); };
        var thoat = NutNhoToi("Đăng xuất");
        thoat.Click += (_, _) =>
        {
            if (!HopThoai.Hoi("Đăng xuất khỏi app quản trị?")) return;
            DangXuat = true;
            Close();
        };
        hangNut.Controls.Add(doiMk);
        hangNut.Controls.Add(thoat);
        duoi.Controls.Add(hangNut);
        duoi.Controls.Add(chucVu);
        duoi.Controls.Add(ten);

        trai.Controls.Add(danhSach);
        trai.Controls.Add(duoi);
        trai.Controls.Add(logo);
        return trai;
    }

    private Button TaoNutMenu(MucMenu m)
    {
        var b = new Button
        {
            Text = m.Ten, Width = 220, Height = 34, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(0xE6, 0xE6, 0xE6),
            BackColor = GiaoDien.Den, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0),
            Margin = new Padding(0), Cursor = Cursors.Hand, Font = GiaoDien.Chu, UseVisualStyleBackColor = false, TabStop = false,
            UseMnemonic = false   // de hien dung ky tu "&" trong "Khach hang & gio hang"
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = GiaoDien.DenNhat;
        b.Click += (_, _) => Mo(m.Khoa);
        b.Paint += (_, e) =>
        {
            if (_khoaHien == m.Khoa)
            {
                using var co = new SolidBrush(GiaoDien.Do);
                e.Graphics.FillRectangle(co, 0, 0, 4, b.Height);
            }
            if (m.Phim != Keys.None)
                TextRenderer.DrawText(e.Graphics, HaTang.ManHinh.TenPhim(m.Phim), GiaoDien.ChuNho,
                    new Rectangle(0, 0, b.Width - 10, b.Height), Color.FromArgb(0x8A, 0x8A, 0x8A),
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        };
        _nutMenu[m.Khoa] = b;
        return b;
    }

    private static Button NutNhoToi(string chu)
    {
        var b = new Button
        {
            Text = chu, AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = GiaoDien.DenNhat,
            Font = GiaoDien.ChuNho, Margin = new Padding(0, 4, 6, 0), Cursor = Cursors.Hand, UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(0x55, 0x55, 0x55);
        return b;
    }

    // ------------------------------------------------------------------ mo man hinh
    public void Mo(string khoa, string? thamSo = null)
    {
        var m = _menu.FirstOrDefault(x => x.Khoa == khoa);
        if (m == null) return;
        if (!_daTao.TryGetValue(khoa, out var mh))
        {
            mh = m.Tao();
            _daTao[khoa] = mh;
        }
        if (_hien != mh)
        {
            _vung.SuspendLayout();
            _vung.Controls.Clear();
            _vung.Controls.Add(mh);
            _vung.ResumeLayout();
            _hien = mh;
            _khoaHien = khoa;
            foreach (var (k, nut) in _nutMenu)
            {
                nut.BackColor = k == khoa ? GiaoDien.DenNhat : GiaoDien.Den;
                nut.ForeColor = k == khoa ? Color.White : Color.FromArgb(0xE6, 0xE6, 0xE6);
                nut.Font = k == khoa ? GiaoDien.ChuDam : GiaoDien.Chu;
                nut.Invalidate();
            }
        }
        mh.KhiHien(thamSo);
        (mh.OTim ?? (Control)mh).Focus();
    }

    // ------------------------------------------------------------------ phim tat (yeu cau 9)
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F1:
                MoBangPhimTat();
                return true;
            case Keys.Control | Keys.K:
                MoNhanh();
                return true;
            case Keys.F5:
                _hien?.LamMoi();
                return true;
            case Keys.Control | Keys.F:
                _hien?.OTim?.Focus();
                return true;
        }
        var menu = _menu.FirstOrDefault(x => x.Phim != Keys.None && x.Phim == keyData);
        if (menu != null)
        {
            Mo(menu.Khoa);
            return true;
        }

        var dangGo = DangGoChu();
        if (!dangGo && keyData == Keys.OemQuestion && _hien?.OTim != null)     // phim "/"
        {
            _hien.OTim.Focus();
            return true;
        }
        if (!dangGo && keyData == (Keys.Shift | Keys.OemQuestion))              // phim "?"
        {
            MoBangPhimTat();
            return true;
        }
        // Phim co Ctrl / Alt / F1-F24: luon chuyen cho man hinh.
        // Phim don (Enter, Delete): chi khi dang dung o bang du lieu / danh sach, de Enter tren 1 nut van la bam nut do,
        // Delete trong o nhap van la xoa chu.
        var coToHop = (keyData & (Keys.Control | Keys.Alt)) != 0 || (keyData & Keys.KeyCode) is >= Keys.F1 and <= Keys.F24;
        var oDangChon = DangChon();
        var oBang = oDangChon is DataGridView or ListBox || oDangChon?.Parent is DataGridView;
        if ((coToHop || oBang) && _hien?.XuLyPhim(keyData) == true) return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private Control? DangChon()
    {
        Control? c = this;
        while (c is ContainerControl cc && cc.ActiveControl != null) c = cc.ActiveControl;
        return c;
    }

    private bool DangGoChu() =>
        DangChon() is TextBoxBase or ComboBox { DropDownStyle: not ComboBoxStyle.DropDownList } or NumericUpDown or DateTimePicker
        || DangChon()?.Parent is NumericUpDown;

    private void MoBangPhimTat()
    {
        using var f = new FormPhimTat(_menu, _hien);
        f.ShowDialog(this);
    }

    private void MoNhanh()
    {
        using var f = new FormMoNhanh(_menu);
        if (f.ShowDialog(this) == DialogResult.OK && f.Chon != null) Mo(f.Chon);
    }

    // Bo dau tieng Viet de tim "don hang" ra "Đơn hàng"
    public static string BoDau(string s)
    {
        var tach = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(tach.Length);
        foreach (var c in tach)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();
    }
}

// F1 / "?": bang phim tat lay tu menu + cac nut cua man hinh dang mo
public sealed class FormPhimTat : Form
{
    private sealed record Dong(string Phim, string TacDung, string PhamVi);

    public FormPhimTat(IEnumerable<FormChinh.MucMenu> menu, HaTang.ManHinh? hien)
    {
        Text = "Bảng phím tắt";
        Font = GiaoDien.Chu;
        ClientSize = new Size(720, 560);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        KeyPreview = true;
        var ds = new List<Dong>();
        if (hien != null)
            ds.AddRange(hien.HanhDongs.Where(h => h.Phim != Keys.None)
                .Select(h => new Dong(HaTang.ManHinh.TenPhim(h.Phim), h.Ten, hien.TieuDe)));
        ds.AddRange(new[]
        {
            new Dong("F1  hoặc  ?", "Mở bảng phím tắt này", "Toàn app"),
            new Dong("Ctrl+K", "Mở nhanh màn hình (gõ tên, Enter)", "Toàn app"),
            new Dong("F5", "Làm mới dữ liệu màn hình đang mở", "Toàn app"),
            new Dong("Ctrl+F  hoặc  /", "Vào ô tìm kiếm của màn hình", "Toàn app"),
            new Dong("F3", "Đổi chế độ tìm: tái sử dụng danh sách / truy vấn mới", "Ô tìm kiếm"),
            new Dong("↑ ↓ PgUp PgDn Home End", "Chọn dòng, chi tiết bên phải tự cập nhật (main-sub)", "Bảng dữ liệu"),
            new Dong("Tab / Shift+Tab", "Chuyển giữa các ô, bảng, nút", "Toàn app"),
            new Dong("Ctrl+C", "Sao chép các dòng đang chọn", "Bảng dữ liệu"),
            new Dong("Ctrl+S  hoặc  Ctrl+Enter", "Lưu / đồng ý", "Hộp thoại"),
            new Dong("Esc", "Đóng hộp thoại / xóa ô tìm", "Hộp thoại"),
        });
        ds.AddRange(menu.Where(m => m.Phim != Keys.None).Select(m => new Dong(HaTang.ManHinh.TenPhim(m.Phim), "Mở " + m.Ten, "Menu")));

        var luoi = new Luoi<Dong>()
            .Them("Phím", d => d.Phim, 200)
            .Them("Tác dụng", d => d.TacDung, 330, fill: true)
            .Them("Phạm vi", d => d.PhamVi, 150, mau: d => d.PhamVi == "Toàn app" ? GiaoDien.ChuPhu : GiaoDien.ChuChinh);
        luoi.Nap(ds);
        var dong = GiaoDien.NutPhu("Đóng  (Esc)");
        dong.DialogResult = DialogResult.Cancel;
        CancelButton = dong;
        var duoi = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        duoi.Controls.Add(dong);
        Controls.Add(luoi.Grid);
        Controls.Add(duoi);
        KeyDown += (_, e) => { if (e.KeyCode == Keys.F1) Close(); };
    }
}

// Ctrl+K: go vai chu (khong dau cung duoc) roi Enter de mo man hinh
public sealed class FormMoNhanh : Form
{
    private readonly TextBox _o = new() { Dock = DockStyle.Top, Font = new Font(GiaoDien.TenFont, 13f) };
    private readonly ListBox _ds = new() { Dock = DockStyle.Fill, Font = new Font(GiaoDien.TenFont, 11f), IntegralHeight = false, BorderStyle = BorderStyle.None };
    private readonly List<FormChinh.MucMenu> _menu;

    public string? Chon { get; private set; }

    public FormMoNhanh(IEnumerable<FormChinh.MucMenu> menu)
    {
        _menu = menu.ToList();
        Text = "Mở nhanh màn hình";
        ClientSize = new Size(460, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        Padding = new Padding(10);
        _ds.Format += (_, e) =>
        {
            if (e.ListItem is FormChinh.MucMenu m) e.Value = $"{m.Ten}    ·  {m.Nhom.ToLower()}";
        };
        Controls.Add(_ds);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        Controls.Add(_o);
        Loc();
        _o.TextChanged += (_, _) => Loc();
        _o.KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.Down when _ds.SelectedIndex < _ds.Items.Count - 1: _ds.SelectedIndex++; e.Handled = true; break;
                case Keys.Up when _ds.SelectedIndex > 0: _ds.SelectedIndex--; e.Handled = true; break;
                case Keys.Return: XacNhan(); e.SuppressKeyPress = true; break;
                case Keys.Escape: DialogResult = DialogResult.Cancel; break;
            }
        };
        _ds.DoubleClick += (_, _) => XacNhan();
        _ds.KeyDown += (_, e) => { if (e.KeyCode == Keys.Return) XacNhan(); };
    }

    private void Loc()
    {
        var tk = FormChinh.BoDau(_o.Text.Trim());
        _ds.Items.Clear();
        foreach (var m in _menu.Where(m => tk.Length == 0 || FormChinh.BoDau(m.Ten + " " + m.Nhom).Contains(tk)))
            _ds.Items.Add(m);
        if (_ds.Items.Count > 0) _ds.SelectedIndex = 0;
    }

    private void XacNhan()
    {
        if (_ds.SelectedItem is not FormChinh.MucMenu m) return;
        Chon = m.Khoa;
        DialogResult = DialogResult.OK;
    }
}

public sealed class FormDoiMatKhau : FormNhap
{
    private readonly TextBox _cu = new() { Width = 260, UseSystemPasswordChar = true };
    private readonly TextBox _moi = new() { Width = 260, UseSystemPasswordChar = true };
    private readonly TextBox _lai = new() { Width = 260, UseSystemPasswordChar = true };

    public FormDoiMatKhau() : base("Đổi mật khẩu", 460)
    {
        ThemO("Mật khẩu hiện tại", _cu, true);
        ThemO("Mật khẩu mới", _moi, true, $"Ít nhất {QuyTac.DoDaiMatKhauToiThieu} ký tự");
        ThemO("Nhập lại mật khẩu mới", _lai, true);
        AcceptButton = NutLuu;
    }

    protected override bool Luu()
    {
        if (_moi.Text != _lai.Text)
        {
            HopThoai.Canh("Mật khẩu nhập lại không khớp");
            _lai.Focus();
            return false;
        }
        if (!ThaoTac.Chay("Đổi mật khẩu", () => new TaiKhoanBLL().DoiMatKhau(Phien.MaTk, _cu.Text, _moi.Text))) return false;
        HopThoai.ThongBao("Đã đổi mật khẩu.");
        return true;
    }
}
