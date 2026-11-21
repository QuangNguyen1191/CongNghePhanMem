namespace AppAdmin.HaTang;

// Lop cha cua moi man hinh trong form chinh: tieu de + thanh cong cu + vung noi dung.
// Moi nut duoc khai bao kem phim tat (yeu cau 9): form chinh chuyen phim xuong man hinh dang mo,
// bang phim tat (F1) doc tu danh sach HanhDongs nen luon dung voi nut that.
public abstract class ManHinh : UserControl
{
    public sealed record HanhDong(string Ten, Keys Phim, Action Lam, Button? Nut);

    private readonly FlowLayoutPanel _congCu;
    private readonly Label _moTa;
    private bool _daMo;

    public string TieuDe { get; }
    public List<HanhDong> HanhDongs { get; } = new();
    protected Panel NoiDung { get; }

    // Form chinh gan: mo man hinh khac (vd tu o tong quan bam sang danh sach don cho xac nhan)
    public static Action<string, string?>? MoManHinh { get; set; }

    protected ManHinh(string tieuDe, string moTa = "")
    {
        TieuDe = tieuDe;
        Dock = DockStyle.Fill;
        BackColor = GiaoDien.Nen;
        Font = GiaoDien.Chu;
        Padding = new Padding(20, 14, 20, 14);

        NoiDung = new Panel { Dock = DockStyle.Fill, BackColor = GiaoDien.Nen, Padding = new Padding(0, 8, 0, 0) };
        _congCu = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true,
            Padding = new Padding(0, 6, 0, 2)
        };
        _moTa = new Label
        {
            Text = moTa, Dock = DockStyle.Top, Height = moTa.Length > 0 ? 22 : 0, ForeColor = GiaoDien.ChuPhu,
            Font = GiaoDien.Chu
        };
        var tieuDeLb = new Label { Text = tieuDe, Dock = DockStyle.Top, Height = 36, Font = GiaoDien.TieuDe, ForeColor = GiaoDien.ChuChinh };
        var vach = new Panel { Dock = DockStyle.Top, Height = 3, Width = 48 };
        vach.Paint += (_, e) =>
        {
            using var co = new SolidBrush(GiaoDien.Do);
            e.Graphics.FillRectangle(co, 0, 0, 48, 3);
        };
        // them theo thu tu nguoc vi Dock = Top xep tu duoi len
        Controls.Add(NoiDung);
        Controls.Add(_congCu);
        Controls.Add(_moTa);
        Controls.Add(vach);
        Controls.Add(tieuDeLb);
    }

    protected void DatMoTa(string moTa)
    {
        _moTa.Text = moTa;
        _moTa.Height = moTa.Length > 0 ? 22 : 0;
    }

    // Nut tren thanh cong cu, chu co ghi phim tat
    protected Button Nut(string ten, Keys phim, Action lam, bool chinh = false, bool nguyHiem = false)
    {
        var chu = phim == Keys.None ? ten : $"{ten}  ({TenPhim(phim)})";
        var b = chinh ? GiaoDien.NutChinh(chu) : nguyHiem ? GiaoDien.NutNguyHiem(chu) : GiaoDien.NutPhu(chu);
        b.Click += (_, _) => lam();
        _congCu.Controls.Add(b);
        HanhDongs.Add(new HanhDong(ten, phim, lam, b));
        return b;
    }

    // Phim tat khong co nut rieng
    protected void Phim(string ten, Keys phim, Action lam) => HanhDongs.Add(new HanhDong(ten, phim, lam, null));

    protected void KhoangCach() => _congCu.Controls.Add(new Panel { Width = 14, Height = 30, Margin = new Padding(0) });

    // Form chinh goi khi man hinh duoc chon. Lan dau tu tai du lieu.
    public void KhiHien(string? thamSo)
    {
        if (thamSo != null) NhanThamSo(thamSo);
        if (!_daMo || thamSo != null || LamMoiKhiMoLai)
        {
            _daMo = true;
            LamMoi();
        }
    }

    // Man hinh tong hop (vd tong quan) tai lai moi lan quay lai, cac man hinh danh sach giu nguyen (F5 de tai lai)
    protected virtual bool LamMoiKhiMoLai => false;

    // vd "CHO_XAC_NHAN" -> man hinh don hang dat san bo loc
    protected virtual void NhanThamSo(string thamSo) { }

    public abstract void LamMoi();

    // O tim kiem cua man hinh (Ctrl+F hoac /)
    public virtual Control? OTim => null;

    public bool XuLyPhim(Keys phim)
    {
        var h = HanhDongs.FirstOrDefault(x => x.Phim == phim && x.Phim != Keys.None && (x.Nut == null || x.Nut.Enabled && x.Nut.Visible));
        if (h == null) return false;
        h.Lam();
        return true;
    }

    public static string TenPhim(Keys k)
    {
        var phan = new List<string>();
        if (k.HasFlag(Keys.Control)) phan.Add("Ctrl");
        if (k.HasFlag(Keys.Shift)) phan.Add("Shift");
        if (k.HasFlag(Keys.Alt)) phan.Add("Alt");
        var chinh = k & Keys.KeyCode;
        phan.Add(chinh switch
        {
            Keys.Return => "Enter",
            Keys.Delete => "Delete",
            Keys.Insert => "Insert",
            Keys.Escape => "Esc",
            >= Keys.D0 and <= Keys.D9 => ((int)(chinh - Keys.D0)).ToString(),
            Keys.OemQuestion => "?",
            _ => chinh.ToString()
        });
        return string.Join("+", phan);
    }
}

// Khung "nhan : gia tri" o ben phai man hinh main-sub
public sealed class KhungThongTin : TableLayoutPanel
{
    public KhungThongTin()
    {
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnCount = 2;
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Padding = new Padding(0, 4, 0, 8);
    }

    public void Dat(params (string Nhan, string? GiaTri)[] dong) => Dat(dong.Select(d => (d.Nhan, d.GiaTri, (Color?)null)).ToArray());

    public void Dat(params (string Nhan, string? GiaTri, Color? Mau)[] dong)
    {
        SuspendLayout();
        Controls.Clear();
        RowStyles.Clear();
        RowCount = dong.Length;
        for (var i = 0; i < dong.Length; i++)
        {
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(new Label
            {
                Text = dong[i].Nhan, AutoSize = true, ForeColor = GiaoDien.ChuPhu, Margin = new Padding(0, 3, 6, 3)
            }, 0, i);
            Controls.Add(new Label
            {
                Text = string.IsNullOrEmpty(dong[i].GiaTri) ? "-" : dong[i].GiaTri, AutoSize = true, MaximumSize = new Size(420, 0),
                ForeColor = dong[i].Mau ?? GiaoDien.ChuChinh, Font = GiaoDien.ChuDam, Margin = new Padding(0, 3, 0, 3)
            }, 1, i);
        }
        ResumeLayout();
    }

    public void Xoa(string chu = "Chọn 1 dòng ở danh sách để xem chi tiết")
    {
        Controls.Clear();
        RowStyles.Clear();
        RowCount = 1;
        var l = new Label { Text = chu, AutoSize = true, ForeColor = GiaoDien.ChuPhu, Margin = new Padding(0, 6, 0, 6) };
        Controls.Add(l, 0, 0);
        SetColumnSpan(l, 2);
    }
}

// Nua phai cua man hinh main-sub: tieu de + khung thong tin + (tuy chon) bang chi tiet + hang nut thao tac
public sealed class KhungChiTiet : Panel
{
    public Label TieuDe { get; }
    public KhungThongTin ThongTin { get; } = new();
    public FlowLayoutPanel HangNut { get; }

    public KhungChiTiet(string tieuDe, Control? bangCon = null, string? tieuDeBangCon = null)
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(14, 10, 14, 10);
        Paint += (_, e) =>
        {
            using var but = new Pen(GiaoDien.Vien);
            e.Graphics.DrawRectangle(but, 0, 0, Width - 1, Height - 1);
        };
        TieuDe = new Label { Text = tieuDe, Dock = DockStyle.Top, Height = 30, Font = GiaoDien.TieuDeNho, AutoEllipsis = true };
        HangNut = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 2, 0, 6) };
        // Thu tu tu tren xuong: tieu de, hang nut, BANG CHI TIET (sub), roi moi toi khung thong tin.
        // Bang con len truoc de nhieu dong van thay ngay, khong phai keo xuong duoi khung thong tin.
        if (bangCon != null)
        {
            var vung = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
            bangCon.Dock = DockStyle.Fill;
            vung.Controls.Add(bangCon);
            if (tieuDeBangCon != null) vung.Controls.Add(GiaoDien.TieuDeMuc(tieuDeBangCon));
            Controls.Add(vung);
            // Khung thong tin xuong cuoi, co tieu de muc rieng va cuon duoc khi dai
            _duoi = new Panel { Dock = DockStyle.Bottom, AutoScroll = true, Height = 200 };
            ThongTin.Dock = DockStyle.Top;
            _duoi.Controls.Add(ThongTin);
            _duoi.Controls.Add(GiaoDien.TieuDeMuc("Thông tin"));
            ThongTin.SizeChanged += (_, _) => CanChieuCaoDuoi();
            SizeChanged += (_, _) => CanChieuCaoDuoi();
            Controls.Add(_duoi);
        }
        else Controls.Add(ThongTin);
        Controls.Add(HangNut);
        Controls.Add(TieuDe);
        ThongTin.Xoa();
    }

    private readonly Panel? _duoi;

    // Them 1 khung (vd do tin cay SDT) vao phan thong tin phia duoi, nam giua tieu de "Thong tin" va khung thong tin
    public void ThemVaoThongTin(Control c)
    {
        if (_duoi == null)
        {
            HangNut.Controls.Add(c);
            return;
        }
        c.Dock = DockStyle.Top;
        _duoi.Controls.Add(c);
        _duoi.Controls.SetChildIndex(c, 1);    // Dock Top: chi so cao nam tren -> tieu de (2), khung nay (1), thong tin (0)
        c.SizeChanged += (_, _) => CanChieuCaoDuoi();
        CanChieuCaoDuoi();
    }

    // Phan thong tin cao vua du noi dung, toi da 1/2 khung chi tiet (dai hon thi cuon), bang chi tiet chiem phan con lai
    private void CanChieuCaoDuoi()
    {
        if (_duoi == null) return;
        var canCo = _duoi.Controls.Cast<Control>().Where(c => c.Visible).Sum(c => c.Height) + 4;
        _duoi.Height = Math.Max(80, Math.Min(canCo, (Height - 120) / 2));
    }
}

// Bo khung main-sub: danh sach ben trai, chi tiet ben phai (keo duoc vach chia)
public static class BoCuc
{
    public static SplitContainer ChiaDoi(Control trai, Control phai, int phanTramTrai = 60)
    {
        var s = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Vertical, BackColor = GiaoDien.Nen, SplitterWidth = 8,
            FixedPanel = FixedPanel.None
        };
        s.Panel1.Controls.Add(trai);
        s.Panel2.Controls.Add(phai);
        // giu ti le khi doi kich thuoc cua so, cho toi khi nguoi dung tu keo vach chia
        bool dangDat = false, nguoiDungKeo = false;
        void DatViTri()
        {
            if (nguoiDungKeo || s.Width <= 0) return;
            var x = s.Width * phanTramTrai / 100;
            if (x <= s.Panel1MinSize || x >= s.Width - s.Panel2MinSize) return;
            dangDat = true;
            s.SplitterDistance = x;
            dangDat = false;
        }
        s.HandleCreated += (_, _) => DatViTri();
        s.SizeChanged += (_, _) => DatViTri();
        s.SplitterMoved += (_, _) => { if (!dangDat && s.Visible && s.Focused) nguoiDungKeo = true; };
        return s;
    }

    // Khung trang chua bang + tieu de, dung lam 1 nua man hinh
    public static Panel KhungBang(Control bang, string? tieuDe = null, Control? duoi = null)
    {
        var k = GiaoDien.Khung(dem: 1);
        k.Controls.Add(bang);
        if (duoi != null)
        {
            duoi.Dock = DockStyle.Bottom;
            k.Controls.Add(duoi);
        }
        if (tieuDe != null) k.Controls.Add(GiaoDien.TieuDeMuc(tieuDe));
        return k;
    }
}
