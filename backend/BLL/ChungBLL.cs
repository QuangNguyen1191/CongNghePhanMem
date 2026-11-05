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

    // ---- v3: khach vang lai (so lieu nhom tu dat, sua o day)
    public const int OtpSoChuSo = 6;
    public const int OtpSoPhutHetHan = 5;            // ma OTP het han sau 5 phut
    public const int OtpSoLanSaiToiDa = 5;           // sai 5 lan phai gui ma moi
    public const int OtpGiayChoGuiLai = 60;          // moi SDT gui toi da 1 ma / 60 giay
    public const int OtpToiDaMoiNgay = 5;            // moi SDT toi da 5 ma / 24 gio
    public const int OtpToiDaMoiIpMoiGio = 10;       // mac dinh, doi duoc trong appsettings (Otp:ToiDaMoiIpMoiGio)
    public const int PhienVangLaiPhut = 30;          // phien khach vang lai (sau khi xac thuc OTP) dung duoc 30 phut
    public const int VangLaiToiDaDonDangCho = 2;     // moi SDT vang lai toi da 2 don dang cho xac nhan
    public const int VangLaiToiDaMoiSanPham = 5;     // moi SKU toi da 5 cai / don vang lai
    public const decimal VangLaiCodToiDa = 5_000_000; // don vang lai tra COD toi da 5 trieu
    public const int SoLanBomChanCod = 2;            // SDT co tu 2 lan bom hang thi khong duoc chon COD

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

    // v3: bo khoang trang / dau cham / gach, doi +84 thanh 0
    public static string? ChuanHoaSdt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var so = new string(s.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (so.StartsWith("+84")) so = "0" + so[3..];
        else if (so.StartsWith("84") && so.Length == 11) so = "0" + so[2..];
        return so;
    }

    // So di dong Viet Nam: 10 so, dau 03 / 05 / 07 / 08 / 09
    public static bool LaSdtDiDong(string s) =>
        s.Length == 10 && s.All(char.IsDigit) && s[0] == '0' && "35789".Contains(s[1]);
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
