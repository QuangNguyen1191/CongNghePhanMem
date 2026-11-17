using System.Diagnostics;

namespace AppAdmin.HaTang;

// Yeu cau 1 tren app: moi lan bam nut / chon dong, cac lenh goi BLL deu chay qua day.
//   - dem so cau SQL cua thao tac (HeThongBLL.BatDauDemSql) va lay vi tri File.cs:dong cua cau cuoi trong DAL
//   - bao len thanh trang thai cua form chinh (su kien DaXong)
//   - loi nghiep vu (LoiNghiepVu) hien MessageBox tieng Viet, loi khac hien thong bao loi he thong
public static class ThaoTac
{
    public sealed record KetQua(string Ten, int SoCau, string? ViTri, long ThoiGianMs, bool ThanhCong);

    public static event Action<KetQua>? DaXong;

    public static KetQua? Cuoi { get; private set; }

    // Chay va hien hop thoai neu loi. Tra ve true neu thanh cong.
    public static bool Chay(string ten, Action lam) => ChayTrong(ten, lam, hienLoi: true, out _);

    public static bool Chay<T>(string ten, Func<T> lam, out T ketQua)
    {
        T kq = default!;
        var ok = ChayTrong(ten, () => kq = lam(), hienLoi: true, out _);
        ketQua = kq;
        return ok;
    }

    // Chay im lang: loi nghiep vu tra ve qua 'loi' de giao dien hien tai cho (vd tinh tien khi dang go)
    public static bool Thu<T>(string ten, Func<T> lam, out T ketQua, out string? loi)
    {
        T kq = default!;
        var ok = ChayTrong(ten, () => kq = lam(), hienLoi: false, out loi);
        ketQua = kq;
        return ok;
    }

    private static bool ChayTrong(string ten, Action lam, bool hienLoi, out string? loi)
    {
        loi = null;
        var conTro = Cursor.Current;
        Cursor.Current = Cursors.WaitCursor;
        var dongHo = Stopwatch.StartNew();
        var dem = HeThongBLL.BatDauDemSql();
        var thanhCong = false;
        try
        {
            lam();
            thanhCong = true;
        }
        catch (LoiNghiepVu e)
        {
            loi = e.Message;
            if (hienLoi) MessageBox.Show(e.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception e)
        {
            loi = DichLoi(e, out var laNghiepVu);
            if (hienLoi)
                MessageBox.Show(loi, laNghiepVu ? "Không thực hiện được" : "Lỗi hệ thống", MessageBoxButtons.OK,
                    laNghiepVu ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }
        finally
        {
            dongHo.Stop();
            var cau = dem.CauCuoi;
            Cuoi = new KetQua(ten, dem.SoCau, cau == null ? null : $"{cau.File}:{cau.Dong} {cau.HamGoi}()",
                dongHo.ElapsedMilliseconds, thanhCong);
            dem.Dispose();
            Cursor.Current = conTro;
            DaXong?.Invoke(Cuoi);
        }
        return thanhCong;
    }

    // Cung quy uoc voi WebApi/HaTang/BoLocLoi.cs. Doc so loi SqlException qua reflection
    // de lop giao dien khong phai tham chieu thu vien SQL (chi DAL moi dung).
    private static string DichLoi(Exception e, out bool laNghiepVu)
    {
        laNghiepVu = false;
        var goc = e is AggregateException { InnerException: { } trong } ? trong : e;
        if (goc.GetType().Name != "SqlException")
            return "Lỗi hệ thống, vui lòng thử lại.\n\nChi tiết: " + goc.Message;

        var so = goc.GetType().GetProperty("Number")?.GetValue(goc) as int? ?? 0;
        switch (so)
        {
            case >= 50000:                       // THROW trong trigger / thu tuc cua CSDL: thong bao tieng Viet
                laNghiepVu = true;
                return goc.Message;
            case 547:
                laNghiepVu = true;
                return "Dữ liệu đang được dùng ở nơi khác hoặc vi phạm ràng buộc.";
            case 2627 or 2601:
                laNghiepVu = true;
                return "Dữ liệu bị trùng.";
            case -1 or 2 or 53 or 4060 or 18456 or 10061:
                return "Không kết nối được cơ sở dữ liệu. Kiểm tra SQL Server đã chạy và chuỗi kết nối "
                       + "(nút \"Cấu hình kết nối\" ở màn hình đăng nhập).\n\nChi tiết: " + goc.Message;
            default:
                return "Lỗi cơ sở dữ liệu.\n\nChi tiết: " + goc.Message;
        }
    }
}
