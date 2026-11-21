using System.Reflection;

namespace AppAdmin.HaTang;

// Bang du lieu chi doc dung chung cho moi man hinh (DataGridView che do VirtualMode):
//   - khai bao cot bang ham lay gia tri (khong phu thuoc ten thuoc tinh, hien duoc nhan tieng Viet)
//   - bam tieu de cot de sap xep, giu dong dang chon khi tai lai
//   - su kien ChonThayDoi de lam main-sub (yeu cau 4): chon dong o bang chinh -> nap chi tiet
//   - dieu khien hoan toan bang ban phim: mui ten, Home / End, PageUp / PageDown, Tab de roi bang
public sealed class Luoi<T> where T : class
{
    private sealed record Cot(Func<T, object?> Lay, Func<T, Color?>? Mau);

    private readonly List<Cot> _cots = new();
    private List<T> _ds = new();
    private bool _dangNap;
    private int _cotSapXep = -1;
    private bool _giam;
    private T? _chonCuoi;

    public DataGridView Grid { get; }

    // Khoa cua 1 dong (vd ma hoa don) de chon lai dung dong sau khi tai lai danh sach
    public Func<T, string>? Khoa { get; set; }

    // Dong in dam (vd dong "Tong cong")
    public Func<T, bool>? InDam { get; set; }

    public string ChuKhiTrong { get; set; } = "Không có dữ liệu";

    public event Action<T?>? ChonThayDoi;
    public event Action<T>? MoDong;

    // The xem nhanh khi re chuot vao 1 dong (null = tat). Ham tra ve null thi khong hien the cho dong do.
    // Mac dinh lay mau theo kieu T trong TheMau (don hang, hoa don, san pham ...).
    public Func<T, TheXemNhanh.NoiDung?>? XemNhanh { get; set; } = TheMau.MacDinh<T>();
    private TheXemNhanh? _the;
    private readonly System.Windows.Forms.Timer _henThe = new() { Interval = 300 };
    private int _dongRe = -1;

    public Luoi(bool nhieuDong = false)
    {
        Grid = TaoBang(nhieuDong);
        Grid.CellValueNeeded += (_, e) =>
        {
            if (e.RowIndex < _ds.Count && e.ColumnIndex < _cots.Count) e.Value = _cots[e.ColumnIndex].Lay(_ds[e.RowIndex]);
        };
        Grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _ds.Count || e.ColumnIndex >= _cots.Count || e.CellStyle == null) return;
            var x = _ds[e.RowIndex];
            if (_cots[e.ColumnIndex].Mau?.Invoke(x) is { } mau)
            {
                e.CellStyle.ForeColor = mau;
                e.CellStyle.SelectionForeColor = mau;
            }
            if (InDam?.Invoke(x) == true) e.CellStyle.Font = GiaoDien.ChuDam;
        };
        Grid.SelectionChanged += (_, _) =>
        {
            if (_dangNap) return;
            BaoChon(false);
        };
        Grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _ds.Count) MoDong?.Invoke(_ds[e.RowIndex]);
        };
        Grid.ColumnHeaderMouseClick += (_, e) => SapXep(e.ColumnIndex);
        Grid.Paint += (_, e) =>
        {
            if (_ds.Count > 0 || _cots.Count == 0) return;
            var vung = new Rectangle(0, Grid.ColumnHeadersHeight, Grid.Width, Math.Max(40, Grid.Height - Grid.ColumnHeadersHeight));
            TextRenderer.DrawText(e.Graphics, ChuKhiTrong, GiaoDien.Chu, vung, GiaoDien.ChuPhu,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        GanTheXemNhanh();
    }

    // ------------------------------------------------------------------ the xem nhanh (re chuot)
    private void GanTheXemNhanh()
    {
        _henThe.Tick += (_, _) =>
        {
            _henThe.Stop();
            HienThe();
        };
        Grid.CellMouseEnter += (_, e) =>
        {
            if (XemNhanh == null) return;
            if (e.RowIndex < 0 || e.RowIndex >= _ds.Count)
            {
                AnThe();
                return;
            }
            if (e.RowIndex == _dongRe) return;          // chuyen o trong cung 1 dong: giu the
            _dongRe = e.RowIndex;
            if (_the is { Visible: true }) HienThe();     // the dang hien: doi ngay sang nguoi khac
            else _henThe.Start();                          // chua hien: doi chuot dung yen 0,3 giay
        };
        Grid.MouseLeave += (_, _) => AnThe();          // chuot ra khoi bang
        Grid.MouseMove += (_, e) =>
        {
            // chuot vao vung trong duoi dong cuoi / tieu de cot
            if (_dongRe >= 0 && Grid.HitTest(e.X, e.Y).RowIndex < 0) AnThe();
        };
        Grid.Scroll += (_, _) => AnThe();
        Grid.MouseDown += (_, _) => AnThe();
        Grid.KeyDown += (_, _) => AnThe();
        Grid.VisibleChanged += (_, _) => { if (!Grid.Visible) AnThe(); };
        Grid.Disposed += (_, _) =>
        {
            _henThe.Dispose();
            _the?.Dispose();
        };
    }

    private void HienThe()
    {
        // chi hien khi cua so chua bang dang duoc dung (khong hien the khi app nam sau cua so khac)
        if (XemNhanh == null || _dongRe < 0 || _dongRe >= _ds.Count || !Grid.Visible || Grid.FindForm() is not { } form
            || !form.ContainsFocus && Form.ActiveForm != form && Form.ActiveForm is not TheXemNhanh)
        {
            AnThe();
            return;
        }
        var nd = XemNhanh(_ds[_dongRe]);
        if (nd == null)
        {
            _the?.Hide();
            return;
        }
        _the ??= new TheXemNhanh();
        _the.HienTai(nd, Cursor.Position);
    }

    public void AnThe()
    {
        _henThe.Stop();
        _dongRe = -1;
        _the?.Hide();
    }

    // ------------------------------------------------------------------ khai bao cot
    public Luoi<T> Them(string tieuDe, Func<T, object?> lay, int rong = 110, string? dinhDang = null, bool canPhai = false,
        bool fill = false, Func<T, Color?>? mau = null)
    {
        var c = new DataGridViewTextBoxColumn
        {
            HeaderText = tieuDe, Width = rong, MinimumWidth = Math.Min(rong, 60), SortMode = DataGridViewColumnSortMode.Programmatic,
            ReadOnly = true
        };
        c.DefaultCellStyle.FormatProvider = GiaoDien.Vn;
        if (dinhDang != null) c.DefaultCellStyle.Format = dinhDang;
        if (canPhai)
        {
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (fill) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        Grid.Columns.Add(c);
        _cots.Add(new Cot(lay, mau));
        return this;
    }

    public Luoi<T> Tien(string tieuDe, Func<T, decimal?> lay, int rong = 120) =>
        Them(tieuDe, x => lay(x), rong, "N0", canPhai: true);

    public Luoi<T> So(string tieuDe, Func<T, object?> lay, int rong = 80) => Them(tieuDe, lay, rong, "N0", canPhai: true);

    public Luoi<T> Ngay(string tieuDe, Func<T, DateTime?> lay, int rong = 100, bool coGio = false) =>
        Them(tieuDe, x => lay(x), rong, coGio ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy");

    public Luoi<T> TrangThai(string tieuDe, Func<T, string?> ma, Dictionary<string, Nhan.Kieu> bang, int rong = 120) =>
        Them(tieuDe, x => Nhan.Ten(bang, ma(x)), rong, mau: x => Nhan.Mau(bang, ma(x)));

    public void XoaCot()
    {
        _dangNap = true;
        Grid.RowCount = 0;
        Grid.Columns.Clear();
        _cots.Clear();
        _cotSapXep = -1;
        _dangNap = false;
    }

    // ------------------------------------------------------------------ du lieu
    public List<T> DuLieu => _ds;

    public int SoDong => _ds.Count;

    public T? Chon
    {
        get
        {
            var i = Grid.CurrentCell?.RowIndex ?? -1;
            if (i < 0 && Grid.SelectedRows.Count > 0) i = Grid.SelectedRows[0].Index;
            return i >= 0 && i < _ds.Count ? _ds[i] : null;
        }
    }

    public List<T> CacDongChon => Grid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index)
        .Where(i => i >= 0 && i < _ds.Count).OrderBy(i => i).Select(i => _ds[i]).ToList();

    public void Nap(IEnumerable<T> ds)
    {
        var cu = Chon;
        var khoaCu = cu != null && Khoa != null ? Khoa(cu) : null;
        AnThe();    // danh sach doi: the dang hien co the la cua dong khac
        _dangNap = true;
        try
        {
            _ds = ds.ToList();
            if (_cotSapXep >= 0) SapXepTrong();
            Grid.RowCount = 0;
            if (_cots.Count > 0) Grid.RowCount = _ds.Count;
            var i = khoaCu == null || Khoa == null ? -1 : _ds.FindIndex(x => Khoa(x) == khoaCu);
            if (_ds.Count > 0) DatDongHienTai(Math.Max(0, i));
            Grid.Invalidate();
        }
        finally
        {
            _dangNap = false;
        }
        BaoChon(true);
    }

    // Ve lai sau khi sua 1 doi tuong trong danh sach (khong tai lai tu CSDL)
    public void VeLai() => Grid.Invalidate();

    public void ChonDong(Func<T, bool> dieuKien)
    {
        var i = _ds.FindIndex(x => dieuKien(x));
        if (i < 0) return;
        DatDongHienTai(i);
        // Bang chua hien len man hinh (vd vua mo man hinh kem ma chung tu) thi DataGridView khong phat SelectionChanged:
        // tu bao chon de khung chi tiet nap dung dong. Da bao roi (cung doi tuong) thi BaoChon tu bo qua.
        BaoChon(false);
    }

    private void DatDongHienTai(int i)
    {
        try
        {
            var cot = Grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
            if (cot == null) return;
            Grid.ClearSelection();
            Grid.CurrentCell = Grid.Rows[i].Cells[cot.Index];
            Grid.Rows[i].Selected = true;
        }
        catch (InvalidOperationException)
        {
            // bang chua hien len man hinh: bo qua, lan chon sau se dung
        }
    }

    private void BaoChon(bool batBuoc)
    {
        var x = Chon;
        if (!batBuoc && ReferenceEquals(x, _chonCuoi)) return;
        _chonCuoi = x;
        ChonThayDoi?.Invoke(x);
    }

    // ------------------------------------------------------------------ sap xep
    private void SapXep(int cot)
    {
        if (cot < 0 || cot >= _cots.Count) return;
        _giam = _cotSapXep == cot && !_giam;
        _cotSapXep = cot;
        var cu = Chon;
        SapXepTrong();
        foreach (DataGridViewColumn c in Grid.Columns) c.HeaderCell.SortGlyphDirection = SortOrder.None;
        Grid.Columns[cot].HeaderCell.SortGlyphDirection = _giam ? SortOrder.Descending : SortOrder.Ascending;
        _dangNap = true;
        if (cu != null) DatDongHienTai(Math.Max(0, _ds.IndexOf(cu)));
        _dangNap = false;
        Grid.Invalidate();
    }

    private void SapXepTrong()
    {
        if (_cotSapXep < 0 || _cotSapXep >= _cots.Count) return;
        var lay = _cots[_cotSapXep].Lay;
        var thuTu = _ds.Select((x, i) => (x, i)).ToList();
        thuTu.Sort((a, b) =>
        {
            var ss = SoSanh(lay(a.x), lay(b.x));
            if (_giam) ss = -ss;
            return ss != 0 ? ss : a.i.CompareTo(b.i);     // on dinh: giu thu tu cu khi bang nhau
        });
        _ds = thuTu.Select(t => t.x).ToList();
    }

    private static int SoSanh(object? a, object? b)
    {
        if (a == null) return b == null ? 0 : -1;
        if (b == null) return 1;
        if (a is IComparable ca && a.GetType() == b.GetType()) return ca.CompareTo(b);
        return string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }

    // ------------------------------------------------------------------ giao dien
    public static DataGridView TaoBang(bool nhieuDong = false)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill, VirtualMode = true, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AllowUserToOrderColumns = true,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = nhieuDong,
            BorderStyle = BorderStyle.None, BackgroundColor = Color.White, GridColor = GiaoDien.Vien,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, ColumnHeadersHeight = 34,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single, AutoGenerateColumns = false,
            StandardTab = true, Font = GiaoDien.Chu
        };
        g.RowTemplate.Height = 28;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
        g.ColumnHeadersDefaultCellStyle.ForeColor = GiaoDien.ChuChinh;
        g.ColumnHeadersDefaultCellStyle.Font = GiaoDien.ChuDam;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        g.DefaultCellStyle.SelectionBackColor = GiaoDien.DongChon;
        g.DefaultCellStyle.SelectionForeColor = GiaoDien.ChuChinh;
        g.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        g.AlternatingRowsDefaultCellStyle.BackColor = GiaoDien.DongXen;
        // giam nhap nhay khi cuon bang lon
        typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(g, true);
        return g;
    }
}
