using System.Diagnostics;

namespace AppAdmin.HaTang;

public static class HopThoai
{
    public static bool Hoi(string cauHoi, string tieuDe = "Xác nhận") =>
        MessageBox.Show(cauHoi, tieuDe, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
        == DialogResult.Yes;

    public static void ThongBao(string noiDung, string tieuDe = "Thông báo") =>
        MessageBox.Show(noiDung, tieuDe, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Canh(string noiDung) =>
        MessageBox.Show(noiDung, "Lưu ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    // Hop nhap ly do / ghi chu. goiY: cac ly do hay dung, chon nhanh bang mui ten.
    // Tra ve null khi bam Huy; "" khi khong bat buoc va de trong.
    public static string? NhapChuoi(IWin32Window? cha, string tieuDe, string nhan, bool batBuoc, string macDinh = "",
        string[]? goiY = null, int toiDa = 255, bool nhieuDong = true)
    {
        using var f = new Form
        {
            Text = tieuDe, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(480, nhieuDong ? 250 : 150),
            Font = GiaoDien.Chu, BackColor = Color.White, KeyPreview = true
        };
        var bang = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
        bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bang.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bang.Controls.Add(GiaoDien.Nhan(nhan + (batBuoc ? " *" : "")), 0, 0);
        var o = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = nhieuDong, MaxLength = toiDa, Text = macDinh, ScrollBars = nhieuDong ? ScrollBars.Vertical : ScrollBars.None
        };
        if (goiY is { Length: > 0 })
        {
            var cb = GiaoDien.Chon(440);
            cb.Items.Add("(chọn lý do có sẵn hoặc tự nhập bên dưới)");
            cb.Items.AddRange(goiY.Cast<object>().ToArray());
            cb.SelectedIndex = 0;
            cb.SelectedIndexChanged += (_, _) => { if (cb.SelectedIndex > 0) o.Text = cb.SelectedItem!.ToString(); };
            bang.Controls.Add(cb, 0, 1);
        }
        bang.Controls.Add(o, 0, 2);
        var nutHang = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
        var ok = GiaoDien.NutChinh("Đồng ý  (Ctrl+Enter)");
        var huy = GiaoDien.NutPhu("Hủy  (Esc)");
        huy.DialogResult = DialogResult.Cancel;
        nutHang.Controls.Add(ok);
        nutHang.Controls.Add(huy);
        bang.Controls.Add(nutHang, 0, 3);
        f.Controls.Add(bang);
        f.CancelButton = huy;
        if (!nhieuDong) f.AcceptButton = ok;
        ok.Click += (_, _) =>
        {
            if (batBuoc && string.IsNullOrWhiteSpace(o.Text))
            {
                Canh($"Vui lòng nhập {nhan.ToLower()}");
                o.Focus();
                return;
            }
            f.DialogResult = DialogResult.OK;
        };
        f.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.Return) { ok.PerformClick(); e.SuppressKeyPress = true; }
        };
        f.Shown += (_, _) => { o.Focus(); o.SelectAll(); };
        return f.ShowDialog(cha) == DialogResult.OK ? o.Text.Trim() : null;
    }

    // Hien chuoi co nut sao chep (vd mat khau tam sau khi dat lai)
    public static void HienChuoiSaoChep(IWin32Window? cha, string tieuDe, string loiDan, string chuoi)
    {
        using var f = new Form
        {
            Text = tieuDe, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(440, 170),
            Font = GiaoDien.Chu, BackColor = Color.White
        };
        var nhan = new Label { Text = loiDan, Location = new Point(16, 14), Size = new Size(410, 40) };
        var o = new TextBox
        {
            Text = chuoi, ReadOnly = true, Font = new Font(GiaoDien.ChuMay.FontFamily, 16f, FontStyle.Bold),
            Location = new Point(16, 60), Width = 300, TextAlign = HorizontalAlignment.Center
        };
        var chep = GiaoDien.NutChinh("Sao chép");
        chep.Location = new Point(326, 60);
        chep.Click += (_, _) => { Clipboard.SetText(chuoi); chep.Text = "Đã chép"; };
        var dong = GiaoDien.NutPhu("Đóng");
        dong.DialogResult = DialogResult.OK;
        dong.Location = new Point(340, 120);
        f.Controls.AddRange(new Control[] { nhan, o, chep, dong });
        f.AcceptButton = dong;
        f.CancelButton = dong;
        f.ShowDialog(cha);
    }
}

// Yeu cau 7 + 8: luu file Excel / PDF do BLL tao ra (BLL tra ve byte[]), mo bang ung dung mac dinh cua Windows
public static class XuatFile
{
    public static void LuuExcel(IWin32Window? cha, byte[] noiDung, string tenGoiY)
    {
        Directory.CreateDirectory(CauHinhApp.ThuMucXuat);
        using var d = new SaveFileDialog
        {
            Title = "Lưu file Excel", Filter = "Excel (*.xlsx)|*.xlsx", InitialDirectory = CauHinhApp.ThuMucXuat,
            FileName = $"{tenGoiY}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx", OverwritePrompt = true
        };
        if (d.ShowDialog(cha) != DialogResult.OK) return;
        try
        {
            File.WriteAllBytes(d.FileName, noiDung);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            HopThoai.Canh($"Không ghi được file (có thể file đang mở trong Excel):\n{e.Message}");
            return;
        }
        if (HopThoai.Hoi($"Đã lưu {Path.GetFileName(d.FileName)}.\nMở file ngay?", "Xuất Excel")) Mo(d.FileName);
    }

    // PDF: luu vao thu muc xuat roi mo luon de xem / in
    public static void MoPdf(byte[] noiDung, string ten)
    {
        Directory.CreateDirectory(CauHinhApp.ThuMucXuat);
        var duongDan = Path.Combine(CauHinhApp.ThuMucXuat, $"{ten}.pdf");
        try
        {
            File.WriteAllBytes(duongDan, noiDung);
        }
        catch (IOException)
        {
            // file cung ten dang mo trong trinh doc PDF: luu ten khac
            duongDan = Path.Combine(CauHinhApp.ThuMucXuat, $"{ten}_{DateTime.Now:HHmmss}.pdf");
            File.WriteAllBytes(duongDan, noiDung);
        }
        Mo(duongDan);
    }

    public static string? ChonFileExcel(IWin32Window? cha)
    {
        using var d = new OpenFileDialog
        {
            Title = "Chọn file Excel", Filter = "Excel (*.xlsx)|*.xlsx", InitialDirectory = CauHinhApp.ThuMucXuat
        };
        return d.ShowDialog(cha) == DialogResult.OK ? d.FileName : null;
    }

    public static void Mo(string duongDan)
    {
        try
        {
            Process.Start(new ProcessStartInfo(duongDan) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            HopThoai.Canh($"Không mở được file: {e.Message}\nFile nằm ở: {duongDan}");
        }
    }
}
