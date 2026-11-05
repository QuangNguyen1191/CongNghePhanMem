using System.Data;
using DTO;

namespace DAL;

// v3: Bang MA_OTP - ma xac thuc so dien thoai cua khach vang lai
public class MaOtpDAL
{
    public long Them(MaOtpDTO o) =>
        DbHelper.ExecuteScalar<long>(@"INSERT INTO MA_OTP (sdt, muc_dich, ma_bam, ngay_tao, han_dung, so_lan_sai, da_dung, dia_chi_ip)
            OUTPUT INSERTED.ma_otp
            VALUES (@Sdt, @MucDich, @MaBam, @NgayTao, @HanDung, 0, 0, @DiaChiIp)", o);

    // Ma moi nhat chua dung / chua bi vo hieu cua 1 SDT
    public MaOtpDTO? LayConHieuLuc(string sdt) =>
        DbHelper.QueryFirstOrDefault<MaOtpDTO>(@"SELECT TOP 1 * FROM MA_OTP
            WHERE sdt = @sdt AND da_dung = 0 ORDER BY ngay_tao DESC, ma_otp DESC", new { sdt });

    public DateTime? LanGuiCuoi(string sdt) =>
        DbHelper.ExecuteScalar<DateTime?>("SELECT MAX(ngay_tao) FROM MA_OTP WHERE sdt = @sdt", new { sdt });

    public int DemTheoSdt(string sdt, DateTime tu) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM MA_OTP WHERE sdt = @sdt AND ngay_tao >= @tu", new { sdt, tu });

    public int DemTheoIp(string ip, DateTime tu) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM MA_OTP WHERE dia_chi_ip = @ip AND ngay_tao >= @tu", new { ip, tu });

    // Gui ma moi thi cac ma cu cua SDT het hieu luc
    public int VoHieuMaCu(string sdt) =>
        DbHelper.Execute("UPDATE MA_OTP SET da_dung = 1 WHERE sdt = @sdt AND da_dung = 0", new { sdt });

    public int TangSoLanSai(long ma) =>
        DbHelper.Execute("UPDATE MA_OTP SET so_lan_sai = so_lan_sai + 1 WHERE ma_otp = @ma", new { ma });

    // Chi 1 request dung duoc ma (2 request cung luc thi request sau nhan 0 dong)
    public bool DanhDauDaDung(long ma) =>
        DbHelper.Execute("UPDATE MA_OTP SET da_dung = 1 WHERE ma_otp = @ma AND da_dung = 0", new { ma }) == 1;
}
