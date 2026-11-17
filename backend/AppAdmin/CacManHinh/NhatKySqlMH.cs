namespace AppAdmin.CacManHinh;

// Yeu cau 1: moi cau SQL chay qua DAL/DbHelper.cs deu duoc ghi kem file + dong code + ham trong DAL (CallerFilePath / CallerLineNumber).
// Man hinh nay doc nhat ky do (HeThongBLL.NhatKySqlGanNhat, khong chay them SQL) va gom theo vi tri de thay ham nao chay nhieu nhat.
public sealed class NhatKySqlMH : ManHinh
{
    private sealed record TheoViTri(string ViTri, string Ham, int SoLan, long TongMs, long LauNhatMs);

    private readonly OTimKiem _tim = new("Lọc theo file, hàm hoặc câu SQL", haiCheDo: false);
    private readonly ComboBox _soDong = GiaoDien.Chon(90);
    private readonly CheckBox _tuLamMoi = new() { Text = "Tự làm mới mỗi 2 giây", AutoSize = true, Margin = new Padding(0, 7, 12, 0) };
    private readonly CheckBox _ghi = new() { Text = "Đang ghi nhật ký", AutoSize = true, Margin = new Padding(0, 7, 12, 0) };
    private readonly Luoi<NhatKySqlDTO> _luoi = new();
    private readonly Luoi<TheoViTri> _gom = new();
    private readonly TextBox _chiTiet = new()
    {
        Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = true, Font = GiaoDien.ChuMay,
        BackColor = Color.FromArgb(0xFA, 0xFA, 0xFA), BorderStyle = BorderStyle.None
    };
    private readonly System.Windows.Forms.Timer _dongHo = new() { Interval = 2000 };
    private List<NhatKySqlDTO> _tatCa = new();

    public override Control OTim => _tim.O;

    public NhatKySqlMH() : base("Nhật ký SQL", "Mỗi câu SQL kèm vị trí File.cs:dòng trong DAL đã chạy nó (yêu cầu 1). Lưu tối đa 500 câu gần nhất trong bộ nhớ của app.")
    {
        Nut("Làm mới", Keys.F5, LamMoi, chinh: true);
        Nut("Sao chép câu SQL", Keys.Control | Keys.Shift | Keys.C, () => { if (_luoi.Chon is { } d) Clipboard.SetText(d.CauSql); });
        Nut("Xóa nhật ký", Keys.Control | Keys.Delete, () =>
        {
            if (!HopThoai.Hoi("Xóa toàn bộ nhật ký SQL đang lưu?")) return;
            HeThongBLL.XoaNhatKySql();
            LamMoi();
        }, nguyHiem: true);

        Muc.Nap(_soDong, new[] { new Muc("100", "100 câu"), new Muc("200", "200 câu"), new Muc("500", "500 câu") }, "200");
        _ghi.Checked = HeThongBLL.GhiNhatKySql;
        _ghi.CheckedChanged += (_, _) => HeThongBLL.GhiNhatKySql = _ghi.Checked;
        _tuLamMoi.CheckedChanged += (_, _) => _dongHo.Enabled = _tuLamMoi.Checked;
        _dongHo.Tick += (_, _) => { if (Visible) LamMoi(); };
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { _tim, GiaoDien.Nhan("Hiện"), _soDong, _tuLamMoi, _ghi });

        _luoi.Them("Thời điểm", d => d.ThoiDiem.ToString("HH:mm:ss.fff"), 95).Them("Vị trí trong DAL", d => $"{d.File}:{d.Dong}", 150)
            .Them("Hàm", d => d.HamGoi, 150).Them("ms", d => d.ThoiGianMs, 50, "N0", canPhai: true, mau: d => d.ThoiGianMs >= 100 ? GiaoDien.Do : null)
            .Them("Số dòng", d => d.SoDong, 65, "N0", canPhai: true).Them("Câu SQL", d => d.CauSql, 400, fill: true);
        _luoi.ChuKhiTrong = "Chưa có câu SQL nào";
        _luoi.ChonThayDoi += d => _chiTiet.Text = d == null ? "" :
            $"-- {d.ThoiDiem:dd/MM/yyyy HH:mm:ss.fff}  ·  {d.File}:{d.Dong}  {d.HamGoi}()  ·  {d.ThoiGianMs} ms  ·  {d.SoDong} dòng\r\n"
            + (d.ThamSo.Length > 0 ? $"-- Tham số: {d.ThamSo}\r\n" : "") + "\r\n" + DinhDangSql(d.CauSql);
        _gom.Them("Vị trí", g => g.ViTri, 150).Them("Hàm", g => g.Ham, 140, fill: true).So("Số lần", g => g.SoLan, 65)
            .So("Tổng ms", g => g.TongMs, 75).So("Lâu nhất", g => g.LauNhatMs, 75);

        var chiaPhai = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 8 };
        chiaPhai.Panel1.Controls.Add(BoCuc.KhungBang(_gom.Grid, "Gom theo vị trí code"));
        var khungSql = GiaoDien.Khung(dem: 8);
        khungSql.Controls.Add(_chiTiet);
        khungSql.Controls.Add(GiaoDien.TieuDeMuc("Câu SQL đang chọn"));
        chiaPhai.Panel2.Controls.Add(khungSql);
        HandleCreated += (_, _) => { if (chiaPhai.Height > 200) chiaPhai.SplitterDistance = chiaPhai.Height / 2; };

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, chiaPhai, 62));
        _tim.CanTim += Loc;
        _soDong.SelectedIndexChanged += (_, _) => LamMoi();
    }

    public override void LamMoi()
    {
        _tatCa = HeThongBLL.NhatKySqlGanNhat(int.TryParse(Muc.Lay(_soDong), out var n) ? n : 200);
        Loc();
    }

    private void Loc()
    {
        var tk = _tim.TuKhoa;
        var ds = tk.Length == 0
            ? _tatCa
            : _tatCa.Where(d => $"{d.File}:{d.Dong} {d.HamGoi} {d.CauSql}".Contains(tk, StringComparison.CurrentCultureIgnoreCase)).ToList();
        _luoi.Nap(ds);
        _gom.Nap(ds.GroupBy(d => (ViTri: $"{d.File}:{d.Dong}", d.HamGoi))
            .Select(g => new TheoViTri(g.Key.ViTri, g.Key.HamGoi, g.Count(), g.Sum(x => x.ThoiGianMs), g.Max(x => x.ThoiGianMs)))
            .OrderByDescending(g => g.SoLan).ThenByDescending(g => g.TongMs));
        _tim.BaoKetQua(ds.Count);
    }

    // Xuong dong truoc cac tu khoa chinh cho de doc
    private static string DinhDangSql(string sql)
    {
        foreach (var tu in new[] { " FROM ", " LEFT JOIN ", " JOIN ", " WHERE ", " GROUP BY ", " ORDER BY ", " UNION ALL ", " UNION ", " INTERSECT ", " VALUES ", " SET " })
            sql = sql.Replace(tu, "\r\n" + tu.TrimStart());
        return sql.Replace(" AND ", "\r\n   AND ");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _dongHo.Dispose();
        base.Dispose(disposing);
    }
}
