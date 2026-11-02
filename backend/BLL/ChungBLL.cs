using DAL;

namespace BLL;

// Loi nghiep vu: thong bao tieng Viet hien thang cho nguoi dung (WebApi tra ve 400, app admin hien MessageBox)
public class LoiNghiepVu : Exception
{
    public LoiNghiepVu(string thongBao) : base(thongBao) { }
}

public class KhongTimThay : LoiNghiepVu
{
    public KhongTimThay(string thongBao) : base(thongBao) { }
}

// Cac quy uoc nghiep vu dung chung
public static class QuyTac
{
    public const int NguongTonThap = 5;             // canh bao ton kho duoi muc nay
    public const decimal TienMoiDiem = 10_000;      // 10.000d tren hoa don da thanh toan = 1 diem
    public const int SoNgayDoiTra = 30;             // duoc doi tra trong 30 ngay ke tu ngay lap hoa don
    public const int DoDaiMatKhauToiThieu = 6;

    public static string XetHang(decimal tongDaMua) => tongDaMua switch
    {
        >= 15_000_000 => "KIM_CUONG",
        >= 8_000_000 => "VANG",
        >= 3_000_000 => "BAC",
        _ => "DONG"
    };

    public static void KiemTra(bool dieuKien, string thongBao)
    {
        if (!dieuKien) throw new LoiNghiepVu(thongBao);
    }

    // Cap nhat / xoa theo ma ma khong co dong nao bi anh huong -> 404 (khong phai 400)
    public static void CanTimThay(bool timThay, string thongBao)
    {
        if (!timThay) throw new KhongTimThay(thongBao);
    }

    public static T CanCo<T>(T? doiTuong, string thongBao) where T : class =>
        doiTuong ?? throw new KhongTimThay(thongBao);

    public static string? RongThanhNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// Diem tich luy va hang tinh lai tu tong hoa don da thanh toan, goi sau moi lan hoa don doi trang thai
internal static class TichDiem
{
    private static readonly KhachHangDAL _khDal = new();

    public static void TinhLai(string? maKh, System.Data.IDbTransaction tx)
    {
        if (maKh == null) return;
        var tong = _khDal.TongDaMua(maKh, tx);
        _khDal.SuaHangDiem(maKh, QuyTac.XetHang(tong), (int)Math.Floor(tong / QuyTac.TienMoiDiem), tx);
    }
}
