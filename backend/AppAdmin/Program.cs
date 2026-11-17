using System.Globalization;

namespace AppAdmin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Kich thuoc trong code tinh theo 96 DPI: de Windows phong ca cua so theo ti le man hinh (125%, 150%...),
        // chu GDI van net (GdiScaled), khong bi lech giua co chu va do rong cot / khung.
        Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // So tien 1.250.000, ngay dd/MM/yyyy
        CultureInfo.DefaultThreadCurrentCulture = GiaoDien.Vn;
        CultureInfo.DefaultThreadCurrentUICulture = GiaoDien.Vn;
        Thread.CurrentThread.CurrentCulture = GiaoDien.Vn;
        Thread.CurrentThread.CurrentUICulture = GiaoDien.Vn;

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show("Lỗi chưa xử lý: " + e.Exception.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);

        CauHinhApp.Doc();
        CauHinhApp.ApDung();

        // Dang xuat thi quay lai man hinh dang nhap, dong cua so chinh thi thoat
        while (true)
        {
            using (var dangNhap = new FormDangNhap())
                if (dangNhap.ShowDialog() != DialogResult.OK) return;

            using var chinh = new FormChinh();
            Application.Run(chinh);
            if (!chinh.DangXuat) return;
            Phien.Hien = null;
        }
    }
}
