using System.Drawing.Drawing2D;

namespace AppAdmin.HaTang;

// Bieu do cot nhom ve bang GDI+ (khong them thu vien). Ho tro so am (loi nhuan lo), chu thich, re chuot xem gia tri.
public sealed class BieuDoCot : Control
{
    public sealed record Chuoi(string Ten, Color Mau, List<decimal> GiaTris);

    private List<string> _nhans = new();
    private List<Chuoi> _chuois = new();
    private readonly ToolTip _goiY = new() { InitialDelay = 100, ReshowDelay = 50 };
    private readonly List<(RectangleF Vung, string Chu)> _cacCot = new();
    private string? _goiYCuoi;

    public string TieuDe { get; set; } = "";
    public string ChuKhiTrong { get; set; } = "Chưa có dữ liệu để vẽ biểu đồ";

    public BieuDoCot()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                 | ControlStyles.UserPaint, true);
        BackColor = Color.White;
        Font = GiaoDien.ChuNho;
    }

    public void Dat(List<string> nhans, List<Chuoi> chuois)
    {
        _nhans = nhans;
        _chuois = chuois;
        Invalidate();
    }

    public void Xoa() => Dat(new List<string>(), new List<Chuoi>());

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        _cacCot.Clear();

        var tren = 12f;
        if (TieuDe.Length > 0)
        {
            TextRenderer.DrawText(g, TieuDe, GiaoDien.TieuDeNho, new Point(10, 8), GiaoDien.ChuChinh);
            tren = 38;
        }
        if (_nhans.Count == 0 || _chuois.Count == 0)
        {
            TextRenderer.DrawText(g, ChuKhiTrong, GiaoDien.Chu, ClientRectangle, GiaoDien.ChuPhu,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        // chu thich
        var x0 = 10f;
        foreach (var c in _chuois)
        {
            using var co = new SolidBrush(c.Mau);
            g.FillRectangle(co, x0, tren + 3, 12, 12);
            TextRenderer.DrawText(g, c.Ten, Font, new Point((int)x0 + 16, (int)tren + 1), GiaoDien.ChuChinh);
            x0 += 24 + TextRenderer.MeasureText(c.Ten, Font).Width;
        }
        tren += 26;

        var tatCa = _chuois.SelectMany(c => c.GiaTris).ToList();
        var lon = Math.Max(0, tatCa.Max());
        var nho = Math.Min(0, tatCa.Min());
        if (lon == nho) lon = nho + 1;
        var buoc = BuocDep((lon - nho) / 5);
        lon = Math.Ceiling(lon / buoc) * buoc;
        nho = Math.Floor(nho / buoc) * buoc;

        var trai = 64f;
        var phai = Width - 12f;
        var xoay = _nhans.Count > 8;
        var duoi = Height - (xoay ? 70f : 32f);
        if (duoi - tren < 40 || phai - trai < 60) return;

        float Y(decimal v) => duoi - (float)((v - nho) / (lon - nho)) * (duoi - tren);

        // luoi ngang + nhan truc
        using (var but = new Pen(GiaoDien.Vien))
        {
            for (var v = nho; v <= lon + buoc / 2; v += buoc)
            {
                var y = Y(v);
                g.DrawLine(but, trai, y, phai, y);
                var chu = GiaoDien.TienGon(v);
                var sz = TextRenderer.MeasureText(chu, Font);
                TextRenderer.DrawText(g, chu, Font, new Point((int)trai - sz.Width - 4, (int)y - sz.Height / 2), GiaoDien.ChuPhu);
            }
        }
        using (var butKhong = new Pen(GiaoDien.ChuPhu))
            g.DrawLine(butKhong, trai, Y(0), phai, Y(0));

        // cot
        var rongNhom = (phai - trai) / _nhans.Count;
        var rongCot = Math.Max(2f, Math.Min(36f, rongNhom * 0.75f / _chuois.Count));
        for (var i = 0; i < _nhans.Count; i++)
        {
            var batDau = trai + i * rongNhom + (rongNhom - rongCot * _chuois.Count) / 2;
            for (var j = 0; j < _chuois.Count; j++)
            {
                var v = i < _chuois[j].GiaTris.Count ? _chuois[j].GiaTris[i] : 0;
                var y1 = Y(Math.Max(v, 0));
                var y2 = Y(Math.Min(v, 0));
                var vung = new RectangleF(batDau + j * rongCot, y1, rongCot - 1, Math.Max(1, y2 - y1));
                using var co = new SolidBrush(_chuois[j].Mau);
                g.FillRectangle(co, vung);
                _cacCot.Add((vung, $"{_nhans[i]}\n{_chuois[j].Ten}: {v.ToString("N0", GiaoDien.Vn)} đ"));
            }
            // nhan truc ngang
            var nhan = _nhans[i].Length > 22 ? _nhans[i][..21] + "…" : _nhans[i];
            if (xoay)
            {
                var trangThai = g.Save();
                g.TranslateTransform(trai + i * rongNhom + rongNhom / 2, duoi + 6);
                g.RotateTransform(35);
                using var co = new SolidBrush(GiaoDien.ChuPhu);
                g.DrawString(nhan, Font, co, 0, 0);
                g.Restore(trangThai);
            }
            else
            {
                var vungChu = new Rectangle((int)(trai + i * rongNhom), (int)duoi + 4, (int)rongNhom, 22);
                TextRenderer.DrawText(g, nhan, Font, vungChu, GiaoDien.ChuPhu,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var trung = _cacCot.FirstOrDefault(c => c.Vung.Contains(e.Location)).Chu;
        if (trung == _goiYCuoi) return;
        _goiYCuoi = trung;
        if (trung == null) _goiY.Hide(this);
        else _goiY.Show(trung, this, e.X + 12, e.Y + 12, 4000);
    }

    // 1, 2, 5 x 10^n
    private static decimal BuocDep(decimal tho)
    {
        if (tho <= 0) return 1;
        var mu = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)tho)));
        var ti = tho / mu;
        return (ti <= 1 ? 1 : ti <= 2 ? 2 : ti <= 5 ? 5 : 10) * mu;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _goiY.Dispose();
        base.Dispose(disposing);
    }
}
