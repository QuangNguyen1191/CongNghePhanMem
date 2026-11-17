namespace AppAdmin.HaTang;

// Yeu cau 2: o tim kiem co 2 che do, doi bang F3 hoac bam nut:
//   TAI SU DUNG : loc tren danh sach da tai san (0 cau SQL), dung TimKiemBLL.Loc...
//   TRUY VAN MOI: moi lan tim la 1 cau SELECT ... WHERE moi, dung TimKiemBLL.Tim... / SanPhamBLL.TimKiem
// Ket qua (so dong, so cau SQL, thoi gian) hien ngay canh o de so sanh 2 cach.
public sealed class OTimKiem : FlowLayoutPanel
{
    private readonly System.Windows.Forms.Timer _cho = new() { Interval = 350 };
    private readonly Button _nutCheDo;
    private readonly Label _ketQua;
    private readonly bool _haiCheDo;

    public TextBox O { get; }
    public bool TaiSuDung { get; private set; } = true;
    public string TuKhoa => O.Text.Trim();

    // Goi khi nguoi dung ngung go 350 ms, bam Enter, hoac doi che do
    public event Action? CanTim;

    public OTimKiem(string goiY = "Tìm...", int rong = 260, bool haiCheDo = true)
    {
        _haiCheDo = haiCheDo;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false;
        Margin = new Padding(0, 0, 10, 0);
        O = new TextBox { Width = rong, Font = GiaoDien.Chu, Margin = new Padding(0, 4, 4, 0), PlaceholderText = goiY + "  ( / )" };
        _nutCheDo = GiaoDien.NutPhu("");
        _nutCheDo.Margin = new Padding(0, 1, 6, 0);
        _nutCheDo.Visible = haiCheDo;
        _nutCheDo.Click += (_, _) => DoiCheDo();
        _ketQua = new Label { AutoSize = true, ForeColor = GiaoDien.ChuPhu, Margin = new Padding(0, 9, 0, 0), Font = GiaoDien.ChuNho };
        Controls.Add(O);
        Controls.Add(_nutCheDo);
        Controls.Add(_ketQua);
        CapNhatNut();

        O.TextChanged += (_, _) => { _cho.Stop(); _cho.Start(); };
        O.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Return)
            {
                _cho.Stop();
                CanTim?.Invoke();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape && O.Text.Length > 0)
            {
                O.Clear();
                e.SuppressKeyPress = true;
            }
        };
        _cho.Tick += (_, _) =>
        {
            _cho.Stop();
            CanTim?.Invoke();
        };
    }

    public void DoiCheDo()
    {
        if (!_haiCheDo) return;
        TaiSuDung = !TaiSuDung;
        CapNhatNut();
        CanTim?.Invoke();
    }

    private void CapNhatNut() =>
        _nutCheDo.Text = TaiSuDung ? "Chế độ: tái sử dụng (F3)" : "Chế độ: truy vấn mới (F3)";

    // Doc ket qua tu ThaoTac.Cuoi (lenh vua chay)
    public void BaoKetQua(int soDong)
    {
        var kq = ThaoTac.Cuoi;
        _ketQua.Text = kq == null
            ? $"{soDong:N0} dòng"
            : $"{soDong:N0} dòng · {kq.SoCau} câu SQL · {kq.ThoiGianMs} ms" + (_haiCheDo ? (TaiSuDung ? " (lọc trên danh sách đã tải)" : " (câu SELECT mới)") : "");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _cho.Dispose();
        base.Dispose(disposing);
    }
}
