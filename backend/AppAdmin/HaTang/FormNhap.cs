namespace AppAdmin.HaTang;

// Lop cha cua cac hop thoai them / sua: luoi 2 cot (nhan | o nhap), nut Luu (Ctrl+S, Ctrl+Enter) va Huy (Esc).
// Luu() goi BLL trong ThaoTac: loi nghiep vu thi hien thong bao va GIU hop thoai mo de sua tiep.
public abstract class FormNhap : Form
{
    protected TableLayoutPanel Bang { get; }
    protected Button NutLuu { get; }
    protected Button NutHuy { get; }
    protected FlowLayoutPanel HangNut { get; }

    protected FormNhap(string tieuDe, int rong = 560)
    {
        Text = tieuDe;
        Font = GiaoDien.Chu;
        BackColor = Color.White;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(rong, 160);
        Padding = new Padding(18, 14, 18, 10);

        // Form AutoSize: cac khung con khong Dock, tu co gian theo noi dung
        var ngoai = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2,
            Location = new Point(Padding.Left, Padding.Top), Margin = new Padding(0)
        };
        Bang = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Padding = new Padding(0, 0, 0, 8),
            Margin = new Padding(0)
        };
        Bang.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        Bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        HangNut = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 8, 0, 0), Anchor = AnchorStyles.Right, Margin = new Padding(0)
        };
        NutLuu = GiaoDien.NutChinh("Lưu  (Ctrl+S)");
        NutHuy = GiaoDien.NutPhu("Hủy  (Esc)");
        NutHuy.DialogResult = DialogResult.Cancel;
        NutLuu.Click += (_, _) => BamLuu();
        HangNut.Controls.Add(NutLuu);
        HangNut.Controls.Add(NutHuy);
        CancelButton = NutHuy;

        ngoai.Controls.Add(Bang, 0, 0);
        ngoai.Controls.Add(HangNut, 0, 1);
        Controls.Add(ngoai);
    }

    // Them 1 dong "nhan | control". batBuoc: them dau * vao nhan.
    protected T ThemO<T>(string nhan, T control, bool batBuoc = false, string? ghiChu = null) where T : Control
    {
        var dong = Bang.RowCount++;
        Bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Bang.Controls.Add(new Label
        {
            Text = nhan + (batBuoc ? " *" : ""), AutoSize = true, Margin = new Padding(0, 7, 8, 0), ForeColor = GiaoDien.ChuChinh
        }, 0, dong);
        if (ghiChu != null)
        {
            var hop = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
            hop.Controls.Add(control);
            hop.Controls.Add(new Label
            {
                Text = ghiChu, AutoSize = true, MaximumSize = new Size(380, 0), ForeColor = GiaoDien.ChuPhu, Font = GiaoDien.ChuNho,
                Margin = new Padding(0, 0, 0, 4)
            });
            Bang.Controls.Add(hop, 1, dong);
        }
        else Bang.Controls.Add(control, 1, dong);
        return control;
    }

    // Dong chiem ca 2 cot (vd bang chi tiet, ghi chu)
    protected T ThemRong<T>(T control) where T : Control
    {
        var dong = Bang.RowCount++;
        Bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Bang.Controls.Add(control, 0, dong);
        Bang.SetColumnSpan(control, 2);
        return control;
    }

    // Tra ve true neu luu thanh cong (hop thoai se dong)
    protected abstract bool Luu();

    protected void BamLuu()
    {
        if (!NutLuu.Enabled) return;
        if (Luu()) DialogResult = DialogResult.OK;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is (Keys.Control | Keys.S) or (Keys.Control | Keys.Return))
        {
            BamLuu();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
