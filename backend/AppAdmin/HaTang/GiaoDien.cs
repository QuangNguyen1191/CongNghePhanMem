using System.Globalization;

namespace AppAdmin.HaTang;

// Mau, font va cac ham tao control dung chung. Cung tinh than voi web staff:
// menu den, do UNIQLO #E50012 cho muc dang chon / nut chinh, goc vuong, chu dam, so canh phai.
public static class GiaoDien
{
    public static readonly CultureInfo Vn = CultureInfo.GetCultureInfo("vi-VN");

    public static readonly Color Do = Color.FromArgb(0xE5, 0x00, 0x12);
    public static readonly Color DoDam = Color.FromArgb(0xB8, 0x00, 0x0E);
    public static readonly Color DoNhat = Color.FromArgb(0xFD, 0xEC, 0xEE);
    public static readonly Color Den = Color.FromArgb(0x1B, 0x1B, 0x1B);
    public static readonly Color DenNhat = Color.FromArgb(0x2E, 0x2E, 0x2E);
    public static readonly Color Nen = Color.FromArgb(0xF4, 0xF4, 0xF4);
    public static readonly Color Trang = Color.White;
    public static readonly Color Vien = Color.FromArgb(0xDD, 0xDD, 0xDD);
    public static readonly Color ChuChinh = Color.FromArgb(0x1B, 0x1B, 0x1B);
    public static readonly Color ChuPhu = Color.FromArgb(0x6B, 0x6B, 0x6B);
    public static readonly Color XanhLa = Color.FromArgb(0x1F, 0x7A, 0x3D);
    public static readonly Color Vang = Color.FromArgb(0x9A, 0x62, 0x00);
    public static readonly Color XanhDuong = Color.FromArgb(0x1D, 0x4E, 0xD8);
    public static readonly Color Xam = Color.FromArgb(0x8C, 0x8C, 0x8C);
    public static readonly Color DongChon = Color.FromArgb(0xFF, 0xE3, 0xE6);
    public static readonly Color DongXen = Color.FromArgb(0xFA, 0xFA, 0xFA);

    // Bang mau cho bieu do (moi chuoi so lieu 1 mau, luon kem chu trong chu thich)
    public static readonly Color[] BangMau =
    {
        Color.FromArgb(0x1B, 0x1B, 0x1B), Do, Color.FromArgb(0x8C, 0x8C, 0x8C), Color.FromArgb(0xE0, 0x9B, 0x00),
        XanhDuong, XanhLa, Color.FromArgb(0x7C, 0x3A, 0xED), Color.FromArgb(0x0E, 0x94, 0x9B)
    };

    public const string TenFont = "Segoe UI";
    public static readonly Font Chu = new(TenFont, 9.75f);
    public static readonly Font ChuDam = new(TenFont, 9.75f, FontStyle.Bold);
    public static readonly Font ChuNho = new(TenFont, 8.5f);
    public static readonly Font TieuDe = new(TenFont, 16f, FontStyle.Bold);
    public static readonly Font TieuDeNho = new(TenFont, 11f, FontStyle.Bold);
    public static readonly Font SoLon = new(TenFont, 22f, FontStyle.Bold);
    public static readonly Font ChuMay = new("Consolas", 9.75f);

    // ------------------------------------------------------------------ nut
    public static Button NutChinh(string chu) => TaoNut(chu, Do, Color.White, Do);

    public static Button NutPhu(string chu) => TaoNut(chu, Color.White, ChuChinh, Vien);

    public static Button NutNguyHiem(string chu) => TaoNut(chu, Color.White, Do, Do);

    private static Button TaoNut(string chu, Color nen, Color mauChu, Color vien)
    {
        var b = new Button
        {
            Text = chu, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly, MinimumSize = new Size(84, 32),
            FlatStyle = FlatStyle.Flat, BackColor = nen, ForeColor = mauChu, Font = ChuDam, Cursor = Cursors.Hand,
            Padding = new Padding(10, 0, 10, 0), Margin = new Padding(0, 0, 6, 0), UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderColor = vien;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = nen == Do ? DoDam : Nen;
        b.EnabledChanged += (_, _) =>
        {
            b.BackColor = b.Enabled ? nen : Nen;
            b.ForeColor = b.Enabled ? mauChu : Xam;
            b.FlatAppearance.BorderColor = b.Enabled ? vien : Vien;
        };
        return b;
    }

    // ------------------------------------------------------------------ chu
    public static Label Nhan(string chu, Font? font = null, Color? mau = null) => new()
    {
        Text = chu, AutoSize = true, Font = font ?? Chu, ForeColor = mau ?? ChuChinh, Margin = new Padding(0, 6, 8, 0)
    };

    public static TextBox O(int rong = 220) => new() { Width = rong, Font = Chu, Margin = new Padding(0, 3, 8, 3) };

    public static ComboBox Chon(int rong = 180, bool goTuDo = false) => new()
    {
        Width = rong, Font = Chu, DropDownStyle = goTuDo ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
        Margin = new Padding(0, 3, 8, 3)
    };

    public static NumericUpDown So(decimal toiDa = 1_000_000_000, int rong = 160, int thapPhan = 0) => new()
    {
        Width = rong, Font = Chu, Maximum = toiDa, Minimum = 0, DecimalPlaces = thapPhan, ThousandsSeparator = true,
        TextAlign = HorizontalAlignment.Right, Margin = new Padding(0, 3, 8, 3)
    };

    public static DateTimePicker Ngay(bool coTheBoTrong = false, int rong = 140) => new()
    {
        Width = rong, Font = Chu, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy",
        ShowCheckBox = coTheBoTrong, Margin = new Padding(0, 3, 8, 3)
    };

    // Dong cong cu / bo loc: cac control xep ngang, tu xuong dong khi hep
    public static FlowLayoutPanel HangNgang(int cao = 40) => new()
    {
        Dock = DockStyle.Top, Height = cao, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true, Padding = new Padding(0, 4, 0, 4), BackColor = Color.Transparent
    };

    // Khung trang co vien mong, chua noi dung
    public static Panel Khung(DockStyle dock = DockStyle.Fill, int dem = 12)
    {
        var p = new Panel { Dock = dock, BackColor = Trang, Padding = new Padding(dem) };
        p.Paint += (_, e) =>
        {
            using var but = new Pen(Vien);
            e.Graphics.DrawRectangle(but, 0, 0, p.Width - 1, p.Height - 1);
        };
        return p;
    }

    // Tieu de muc nho co vach do ben trai
    public static Label TieuDeMuc(string chu)
    {
        var l = new Label
        {
            Text = chu, Dock = DockStyle.Top, Height = 30, Font = TieuDeNho, ForeColor = ChuChinh,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
        };
        l.Paint += (_, e) =>
        {
            using var co = new SolidBrush(Do);
            e.Graphics.FillRectangle(co, 0, 7, 3, l.Height - 14);
        };
        return l;
    }

    // ------------------------------------------------------------------ dinh dang
    public static string Tien(decimal? so) => so == null ? "" : so.Value.ToString("N0", Vn) + " đ";

    public static string NgayVn(DateTime? d) => d?.ToString("dd/MM/yyyy") ?? "";

    public static string GioVn(DateTime? d) => d?.ToString("dd/MM/yyyy HH:mm") ?? "";

    // 1.250.000 -> "1,3 tr"; dung cho truc bieu do
    public static string TienGon(decimal so)
    {
        var a = Math.Abs(so);
        return a switch
        {
            >= 1_000_000_000 => (so / 1_000_000_000).ToString("0.#", Vn) + " tỷ",
            >= 1_000_000 => (so / 1_000_000).ToString("0.#", Vn) + " tr",
            >= 1_000 => (so / 1_000).ToString("0.#", Vn) + " k",
            _ => so.ToString("0", Vn)
        };
    }
}

// Muc trong ComboBox: Ma la gia tri gui xuong BLL (null = tat ca), Ten la chu hien thi
public sealed record Muc(string? Ma, string Ten)
{
    public override string ToString() => Ten;

    public static void Nap(ComboBox cb, IEnumerable<Muc> ds, string? chon = null)
    {
        cb.Items.Clear();
        foreach (var m in ds) cb.Items.Add(m);
        var i = cb.Items.Cast<Muc>().ToList().FindIndex(m => m.Ma == chon);
        if (cb.Items.Count > 0) cb.SelectedIndex = Math.Max(0, i);
    }

    public static string? Lay(ComboBox cb) => (cb.SelectedItem as Muc)?.Ma;
}
