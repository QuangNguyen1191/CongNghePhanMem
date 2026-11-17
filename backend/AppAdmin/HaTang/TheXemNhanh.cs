namespace AppAdmin.HaTang;

// The xem nhanh (hover card): rê chuột vào 1 dòng của bảng -> hiện form nhỏ cạnh con trỏ với thông tin của dòng đó.
// Không lấy focus của cửa sổ chính (không làm mất dòng đang chọn / ô đang gõ), không hiện trên thanh tác vụ.
// Dùng qua Luoi<T>.XemNhanh; nội dung lấy từ dữ liệu đã tải nên rê chuột không chạy câu SQL nào.
public sealed class TheXemNhanh : Form
{
    public sealed record Dong(string Nhan, string? GiaTri, Color? Mau = null);

    public sealed record NoiDung(string TieuDe, string? PhuDe, IReadOnlyList<Dong> Dongs, Color? MauNhan = null, string? Nhan = null);

    private const int Le = 14;          // le trong
    private const int RongToiDa = 360;
    private const int CaoDong = 22;

    private NoiDung? _nd;
    private int CotNhan = 96;           // be rong cot nhan: do theo nhan dai nhat cua the

    public TheXemNhanh()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        // Khong dat TopMost = true: thuoc tinh nay lam WinForms kich hoat cua so (cuop focus cua cua so chinh).
        // Nam tren cung bang WS_EX_TOPMOST trong CreateParams.
        BackColor = Color.White;
        Font = GiaoDien.Chu;
        DoubleBuffered = true;
    }

    // Hien ma khong kich hoat cua so (focus van o cua so chinh)
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOPMOST = 0x8, CS_DROPSHADOW = 0x20000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }

    // Khong cho chuot "bam xuyen" vao the: chuot di qua the coi nhu di qua bang ben duoi
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84, HTTRANSPARENT = -1;
        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = HTTRANSPARENT;
            return;
        }
        base.WndProc(ref m);
    }

    public void HienTai(NoiDung nd, Point conTro)
    {
        _nd = nd;
        Size = TinhKichThuoc(nd);

        // dat ben phai - duoi con tro; tran man hinh thi lat sang trai / len tren
        var vung = Screen.FromPoint(conTro).WorkingArea;
        var x = conTro.X + 18;
        var y = conTro.Y + 20;
        if (x + Width > vung.Right) x = conTro.X - Width - 12;
        if (y + Height > vung.Bottom) y = conTro.Y - Height - 12;
        Location = new Point(Math.Max(vung.Left, x), Math.Max(vung.Top, y));

        Invalidate();
        if (!Visible) Show();
    }

    private Size TinhKichThuoc(NoiDung nd)
    {
        CotNhan = Math.Min(140, nd.Dongs.Select(d => TextRenderer.MeasureText(d.Nhan, GiaoDien.ChuNho).Width).DefaultIfEmpty(80).Max() + 12);
        var rongGiaTri = RongToiDa - 2 * Le - CotNhan;
        var cao = Le + 24 + (nd.PhuDe != null ? 18 : 0) + 10;
        foreach (var d in nd.Dongs)
        {
            var h = TextRenderer.MeasureText(d.GiaTri ?? "-", GiaoDien.Chu, new Size(rongGiaTri, 0), TextFormatFlags.WordBreak).Height;
            cao += Math.Max(CaoDong, h + 4);
        }
        return new Size(RongToiDa, cao + Le - 4);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_nd is not { } nd) return;
        var g = e.Graphics;
        using (var vien = new Pen(GiaoDien.Vien)) g.DrawRectangle(vien, 0, 0, Width - 1, Height - 1);
        using (var thanh = new SolidBrush(nd.MauNhan ?? GiaoDien.Do)) g.FillRectangle(thanh, 0, 0, Width, 4);

        var y = Le;
        var rongTieuDe = Width - 2 * Le;
        if (nd.Nhan != null)
        {
            // nhan nho goc phai (vd vai tro)
            var kt = TextRenderer.MeasureText(nd.Nhan, GiaoDien.ChuNho);
            var o = new Rectangle(Width - Le - kt.Width - 8, y + 2, kt.Width + 8, kt.Height + 2);
            using (var nen = new SolidBrush(Color.FromArgb(24, nd.MauNhan ?? GiaoDien.Do))) g.FillRectangle(nen, o);
            TextRenderer.DrawText(g, nd.Nhan, GiaoDien.ChuNho, o, nd.MauNhan ?? GiaoDien.Do,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            rongTieuDe = o.Left - Le - 6;
        }
        TextRenderer.DrawText(g, nd.TieuDe, GiaoDien.TieuDeNho, new Rectangle(Le, y, rongTieuDe, 24), GiaoDien.ChuChinh,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        y += 24;
        if (nd.PhuDe != null)
        {
            TextRenderer.DrawText(g, nd.PhuDe, GiaoDien.ChuNho, new Rectangle(Le, y, Width - 2 * Le, 18), GiaoDien.ChuPhu,
                TextFormatFlags.EndEllipsis);
            y += 18;
        }
        y += 4;
        using (var ke = new Pen(GiaoDien.Vien)) g.DrawLine(ke, Le, y, Width - Le, y);
        y += 6;

        var rongGiaTri = Width - 2 * Le - CotNhan;
        foreach (var d in nd.Dongs)
        {
            var h = Math.Max(CaoDong, TextRenderer.MeasureText(d.GiaTri ?? "-", GiaoDien.Chu, new Size(rongGiaTri, 0), TextFormatFlags.WordBreak).Height + 4);
            TextRenderer.DrawText(g, d.Nhan, GiaoDien.ChuNho, new Rectangle(Le, y, CotNhan, CaoDong), GiaoDien.ChuPhu,
                TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, string.IsNullOrWhiteSpace(d.GiaTri) ? "-" : d.GiaTri, d.Mau == null ? GiaoDien.Chu : GiaoDien.ChuDam,
                new Rectangle(Le + CotNhan, y + 2, rongGiaTri, h), d.Mau ?? GiaoDien.ChuChinh, TextFormatFlags.WordBreak);
            y += h;
        }
    }
}
