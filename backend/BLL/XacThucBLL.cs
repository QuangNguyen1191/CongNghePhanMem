using System.Security.Cryptography;
using System.Text;
using DAL;
using DTO;

namespace BLL;

// v3: Gui ma OTP. Do an khong co tong dai SMS nen mac dinh dung ban GIA LAP (in ra console WebApi).
// Muon gui that (email / SMS) chi can viet 1 lop khac cai IGuiOtp roi gan vao XacThucBLL.BoGui.
public interface IGuiOtp
{
    void Gui(string sdt, string ma, string mucDich);
}

public class GuiOtpGiaLap : IGuiOtp
{
    public void Gui(string sdt, string ma, string mucDich) =>
        Console.WriteLine($"[OTP gia lap] {DateTime.Now:HH:mm:ss} SDT {sdt} ({mucDich}): {ma}");
}

// Xac thuc so dien thoai cua khach vang lai bang ma OTP 6 so.
// Xac thuc xong, WebApi cap "phien khach vang lai" (JWT ngan han) dung de dat hang / tra cuu don / tao tai khoan nhanh.
public class XacThucBLL
{
    public static IGuiOtp BoGui { get; set; } = new GuiOtpGiaLap();
    public static bool HienMaDemo { get; set; }                          // tra ma ve cho web hien (chi dung khi demo)
    public static int ToiDaMoiIpMoiGio { get; set; } = QuyTac.OtpToiDaMoiIpMoiGio;
    public static Func<DateTime> BayGio { get; set; } = () => DateTime.Now;

    private readonly MaOtpDAL _dal = new();
    private readonly KhachHangDAL _khDal = new();
    private readonly DonDatHangDAL _donDal = new();

    public KetQuaGuiOtpDTO GuiOtp(GuiOtpDTO yc, string? ip)
    {
        var sdt = KiemTraSdt(yc.Sdt);
        QuyTac.KiemTra(yc.MucDich is "MUA_HANG" or "TRA_CUU" or "DANG_KY", "Mục đích gửi mã không hợp lệ");
        var bayGio = BayGio();

        var lanCuoi = _dal.LanGuiCuoi(sdt);
        if (lanCuoi != null)
        {
            var conCho = QuyTac.OtpGiayChoGuiLai - (int)(bayGio - lanCuoi.Value).TotalSeconds;
            QuyTac.KiemTra(conCho <= 0, $"Vui lòng đợi {conCho} giây trước khi gửi lại mã");
        }
        QuyTac.KiemTra(_dal.DemTheoSdt(sdt, bayGio.AddDays(-1)) < QuyTac.OtpToiDaMoiNgay,
            $"Số điện thoại này đã nhận {QuyTac.OtpToiDaMoiNgay} mã trong 24 giờ qua, vui lòng thử lại sau");
        if (ip != null)
            QuyTac.KiemTra(_dal.DemTheoIp(ip, bayGio.AddHours(-1)) < ToiDaMoiIpMoiGio,
                "Bạn đã yêu cầu quá nhiều mã xác thực, vui lòng thử lại sau 1 giờ");

        var ma = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _dal.VoHieuMaCu(sdt);
        _dal.Them(new MaOtpDTO
        {
            Sdt = sdt, MucDich = yc.MucDich, MaBam = Bam(sdt, ma), NgayTao = bayGio,
            HanDung = bayGio.AddMinutes(QuyTac.OtpSoPhutHetHan), DiaChiIp = ip
        });
        BoGui.Gui(sdt, ma, yc.MucDich);
        return new KetQuaGuiOtpDTO
        {
            Sdt = sdt, HetHanSauGiay = QuyTac.OtpSoPhutHetHan * 60, GuiLaiSauGiay = QuyTac.OtpGiayChoGuiLai,
            MaDemo = HienMaDemo ? ma : null
        };
    }

    // Dung ma: tra ve thong tin phien (WebApi gan token). Sai: tang so lan sai, qua 5 lan thi phai gui ma moi.
    public PhienVangLaiDTO XacThuc(XacThucOtpDTO yc)
    {
        var sdt = KiemTraSdt(yc.Sdt);
        var ma = (yc.Ma ?? "").Trim();
        QuyTac.KiemTra(ma.Length == QuyTac.OtpSoChuSo && ma.All(char.IsDigit), "Mã xác thực gồm 6 chữ số");

        var o = _dal.LayConHieuLuc(sdt) ?? throw new LoiNghiepVu("Chưa có mã xác thực cho số này, vui lòng bấm gửi mã");
        QuyTac.KiemTra(o.HanDung > BayGio(), "Mã xác thực đã hết hạn, vui lòng gửi mã mới");
        QuyTac.KiemTra(o.SoLanSai < QuyTac.OtpSoLanSaiToiDa,
            $"Bạn đã nhập sai {QuyTac.OtpSoLanSaiToiDa} lần, vui lòng gửi mã mới");
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(o.MaBam), Encoding.ASCII.GetBytes(Bam(sdt, ma))))
        {
            _dal.TangSoLanSai(o.MaOtp);
            var con = QuyTac.OtpSoLanSaiToiDa - o.SoLanSai - 1;
            throw new LoiNghiepVu(con > 0 ? $"Mã xác thực không đúng, bạn còn {con} lần thử" : "Mã xác thực không đúng, vui lòng gửi mã mới");
        }
        QuyTac.KiemTra(_dal.DanhDauDaDung(o.MaOtp), "Mã xác thực đã được sử dụng, vui lòng gửi mã mới");
        _khDal.DanhDauDaXacThuc(sdt);
        return ThongTinPhien(sdt);
    }

    // Thong tin da biet cua SDT (de dien san form). Chi goi sau khi SDT da xac thuc.
    public PhienVangLaiDTO ThongTinPhien(string sdt)
    {
        var kh = _khDal.LayTheoSdt(sdt);
        return new PhienVangLaiDTO
        {
            Sdt = sdt, HetHan = BayGio().AddMinutes(QuyTac.PhienVangLaiPhut), CoTaiKhoan = kh?.MaTk != null,
            HoTen = kh?.HoTen, Email = kh?.Email, DiaChi = kh?.DiaChi,
            SoDonCu = kh == null ? 0 : _donDal.LayTheoKhach(kh.MaKh).Count
        };
    }

    public static string KiemTraSdt(string? sdt)
    {
        var s = QuyTac.ChuanHoaSdt(sdt);
        QuyTac.KiemTra(s != null, "Vui lòng nhập số điện thoại");
        QuyTac.KiemTra(QuyTac.LaSdtDiDong(s!), "Số điện thoại di động không hợp lệ (10 số, bắt đầu bằng 03, 05, 07, 08 hoặc 09)");
        return s!;
    }

    private static string Bam(string sdt, string ma) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{sdt}:{ma}")));
}
