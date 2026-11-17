namespace AppAdmin;

// Dang nhap app admin: TaiKhoanBLL.DangNhap(..., NoiDangNhap.AppAdmin) chi cho vai tro ADMIN vao
public sealed class FormDangNhap : Form
{
    private readonly TextBox _ten = new() { Width = 300, Font = new Font(GiaoDien.TenFont, 11f) };
    private readonly TextBox _matKhau = new() { Width = 300, Font = new Font(GiaoDien.TenFont, 11f), UseSystemPasswordChar = true };
    private readonly Label _loi = new() { AutoSize = true, MaximumSize = new Size(320, 0), ForeColor = GiaoDien.Do };
    private readonly Label _ketNoi = new() { AutoSize = true, MaximumSize = new Size(320, 0), ForeColor = GiaoDien.ChuPhu, Font = GiaoDien.ChuNho };

    public FormDangNhap()
    {
        Text = "Đăng nhập - App quản trị";
        Font = GiaoDien.Chu;
        BackColor = Color.White;
        ClientSize = new Size(760, 440);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        KeyPreview = true;

        // ---- ben trai: thuong hieu
        var trai = new Panel { Dock = DockStyle.Left, Width = 300, BackColor = GiaoDien.Do };
        trai.Paint += (_, e) =>
        {
            var g = e.Graphics;
            using var chuLon = new Font(GiaoDien.TenFont, 30f, FontStyle.Bold);
            TextRenderer.DrawText(g, CauHinhApp.TenCuaHang.ToUpper(), chuLon, new Point(28, 120), Color.White);
            TextRenderer.DrawText(g, "App quản trị cửa hàng", GiaoDien.TieuDeNho, new Point(32, 180), Color.White);
            TextRenderer.DrawText(g, "Danh mục · Nhân sự · Khuyến mãi\nBáo cáo thống kê · Nhập hàng", GiaoDien.Chu,
                new Rectangle(32, 214, 250, 60), Color.FromArgb(0xFF, 0xD6, 0xD9), TextFormatFlags.WordBreak);
            TextRenderer.DrawText(g, "QL_BanThoiTrang · .NET 8 WinForms", GiaoDien.ChuNho, new Point(32, 400),
                Color.FromArgb(0xFF, 0xD6, 0xD9));
        };

        // ---- ben phai: form
        var phai = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(48, 56, 40, 20), ColumnCount = 1 };
        void Them(Control c) { phai.RowStyles.Add(new RowStyle(SizeType.AutoSize)); phai.Controls.Add(c); }
        Them(new Label { Text = "Đăng nhập", AutoSize = true, Font = GiaoDien.TieuDe, Margin = new Padding(0, 0, 0, 4) });
        Them(new Label
        {
            Text = "Chỉ tài khoản quản trị (ADMIN) dùng được ứng dụng này.", AutoSize = true, ForeColor = GiaoDien.ChuPhu,
            Margin = new Padding(0, 0, 0, 18)
        });
        Them(GiaoDien.Nhan("Tên đăng nhập"));
        Them(_ten);
        Them(GiaoDien.Nhan("Mật khẩu"));
        Them(_matKhau);
        var hien = new CheckBox { Text = "Hiện mật khẩu", AutoSize = true, Margin = new Padding(0, 4, 0, 10) };
        hien.CheckedChanged += (_, _) => _matKhau.UseSystemPasswordChar = !hien.Checked;
        Them(hien);
        var nut = GiaoDien.NutChinh("Đăng nhập  (Enter)");
        nut.MinimumSize = new Size(300, 38);
        nut.Click += (_, _) => DangNhap();
        Them(nut);
        Them(_loi);
        var cauHinh = new LinkLabel { Text = "Cấu hình kết nối CSDL  (F8)", AutoSize = true, Margin = new Padding(0, 16, 0, 2), LinkColor = GiaoDien.XanhDuong };
        cauHinh.LinkClicked += (_, _) => MoCauHinh();
        Them(cauHinh);
        Them(_ketNoi);

        Controls.Add(phai);
        Controls.Add(trai);
        AcceptButton = nut;
        _ten.Text = CauHinhApp.TenDangNhapCuoi;
        HienKetNoi();
        Shown += (_, _) => (_ten.Text.Length > 0 ? _matKhau : _ten).Focus();
    }

    private void HienKetNoi()
    {
        var c = CauHinhKetNoiForm.TachChuoi(CauHinhApp.ChuoiKetNoi);
        c.TryGetValue("server", out var sv);
        c.TryGetValue("database", out var db);
        _ketNoi.Text = $"Đang dùng: {sv ?? "?"} / {db ?? "?"}";
    }

    private void DangNhap()
    {
        _loi.Text = "";
        if (_ten.Text.Trim().Length == 0 || _matKhau.Text.Length == 0)
        {
            _loi.Text = "Vui lòng nhập tên đăng nhập và mật khẩu";
            return;
        }
        if (!ThaoTac.Thu("Đăng nhập", () => new TaiKhoanBLL().DangNhap(_ten.Text, _matKhau.Text, NoiDangNhap.AppAdmin),
                out var phien, out var loi))
        {
            _loi.Text = loi;
            _matKhau.SelectAll();
            _matKhau.Focus();
            return;
        }
        Phien.Hien = phien;
        CauHinhApp.TenDangNhapCuoi = phien.TenDangNhap;
        try { CauHinhApp.Luu(); } catch (IOException) { /* thu muc chi doc: bo qua, chi mat ten dang nhap goi y */ }
        DialogResult = DialogResult.OK;
    }

    private void MoCauHinh()
    {
        using var f = new CauHinhKetNoiForm();
        if (f.ShowDialog(this) == DialogResult.OK) HienKetNoi();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F8)
        {
            MoCauHinh();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}

// Sua chuoi ket noi SQL Server ngay trong app (may cham bai thuong la SQLEXPRESS hoac localhost)
public sealed class CauHinhKetNoiForm : FormNhap
{
    private readonly TextBox _server = GiaoDien.O(300);
    private readonly TextBox _csdl = GiaoDien.O(300);
    private readonly ComboBox _xacThuc = GiaoDien.Chon(300);
    private readonly TextBox _taiKhoan = GiaoDien.O(300);
    private readonly TextBox _matKhau = new() { Width = 300, UseSystemPasswordChar = true };
    private readonly TextBox _chuoi = new() { Width = 380, Height = 64, Multiline = true, Font = GiaoDien.ChuMay, ScrollBars = ScrollBars.Vertical };
    private bool _dangGhep;

    public CauHinhKetNoiForm() : base("Cấu hình kết nối cơ sở dữ liệu", 600)
    {
        Muc.Nap(_xacThuc, new[] { new Muc("WIN", "Windows Authentication"), new Muc("SQL", "SQL Server (tài khoản + mật khẩu)") });
        var c = TachChuoi(CauHinhApp.ChuoiKetNoi);
        _server.Text = c.GetValueOrDefault("server") ?? c.GetValueOrDefault("data source") ?? "localhost\\SQLEXPRESS";
        _csdl.Text = c.GetValueOrDefault("database") ?? c.GetValueOrDefault("initial catalog") ?? "QL_BanThoiTrang";
        var coUser = c.ContainsKey("user id") || c.ContainsKey("uid");
        _xacThuc.SelectedIndex = coUser ? 1 : 0;
        _taiKhoan.Text = c.GetValueOrDefault("user id") ?? c.GetValueOrDefault("uid") ?? "sa";
        _matKhau.Text = c.GetValueOrDefault("password") ?? c.GetValueOrDefault("pwd") ?? "";
        _chuoi.Text = CauHinhApp.ChuoiKetNoi;

        ThemO("Máy chủ (Server)", _server, true, "vd: localhost, localhost\\SQLEXPRESS, .\\SQL2022");
        ThemO("Cơ sở dữ liệu", _csdl, true);
        ThemO("Xác thực", _xacThuc);
        ThemO("Tài khoản SQL", _taiKhoan);
        ThemO("Mật khẩu SQL", _matKhau);
        ThemO("Chuỗi kết nối", _chuoi, ghiChu: "Tự ghép từ các ô trên. Có thể sửa tay nếu cần tham số khác.");

        foreach (var o in new Control[] { _server, _csdl, _taiKhoan, _matKhau }) o.TextChanged += (_, _) => GhepChuoi();
        _xacThuc.SelectedIndexChanged += (_, _) => { CapNhatO(); GhepChuoi(); };
        CapNhatO();

        var kiemTra = GiaoDien.NutPhu("Kiểm tra kết nối  (F5)");
        kiemTra.Click += (_, _) => KiemTra(true);
        HangNut.Controls.Add(kiemTra);
        KeyDown += (_, e) => { if (e.KeyCode == Keys.F5) KiemTra(true); };
    }

    private void CapNhatO() => _taiKhoan.Enabled = _matKhau.Enabled = Muc.Lay(_xacThuc) == "SQL";

    private void GhepChuoi()
    {
        if (_dangGhep) return;
        _dangGhep = true;
        var xacThuc = Muc.Lay(_xacThuc) == "SQL"
            ? $"User Id={_taiKhoan.Text.Trim()};Password={_matKhau.Text}"
            : "Trusted_Connection=True";
        _chuoi.Text = $"Server={_server.Text.Trim()};Database={_csdl.Text.Trim()};{xacThuc};TrustServerCertificate=True";
        _dangGhep = false;
    }

    private bool KiemTra(bool baoThanhCong)
    {
        HeThongBLL.CauHinh(_chuoi.Text.Trim(), CauHinhApp.InSqlRaConsole);
        var ok = ThaoTac.Thu("Kiểm tra kết nối", () => new LoaiSpBLL().LayTatCa().Count, out var soLoai, out var loi);
        if (!ok) HopThoai.Canh("Không kết nối được:\n\n" + loi);
        else if (baoThanhCong) HopThoai.ThongBao($"Kết nối thành công. CSDL có {soLoai} loại sản phẩm.");
        CauHinhApp.ApDung();                 // chua bam Luu thi tra lai cau hinh cu
        return ok;
    }

    protected override bool Luu()
    {
        if (!KiemTra(false) && !HopThoai.Hoi("Chưa kết nối được bằng chuỗi này. Vẫn lưu?")) return false;
        CauHinhApp.ChuoiKetNoi = _chuoi.Text.Trim();
        try
        {
            CauHinhApp.Luu();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            HopThoai.Canh("Không ghi được appsettings.json: " + e.Message);
            return false;
        }
        CauHinhApp.ApDung();
        return true;
    }

    // "Server=a;Database=b" -> { server: a, database: b } (khoa viet thuong)
    public static Dictionary<string, string> TachChuoi(string chuoi) =>
        chuoi.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .GroupBy(p => p[0].Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Last()[1].Trim());
}
