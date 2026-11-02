using DAL;
using DTO;

namespace BLL;

// Lop giao dien (WebApi, app admin) khong goi thang DAL: cau hinh ket noi va nhat ky SQL di qua day
public static class HeThongBLL
{
    public static void CauHinh(string chuoiKetNoi, bool inSqlRaConsole = false)
    {
        KetNoi.ChuoiKetNoi = chuoiKetNoi;
        NhatKySql.InRaConsole = inSqlRaConsole;
    }

    public static List<NhatKySqlDTO> NhatKySqlGanNhat(int soLuong = 100) => NhatKySql.LayGanNhat(soLuong);
    public static void XoaNhatKySql() => NhatKySql.Xoa();
    public static bool GhiNhatKySql { get => NhatKySql.BatGhi; set => NhatKySql.BatGhi = value; }

    // Dem so cau SQL cua 1 thao tac (1 request web / 1 lan bam nut tren app)
    public static NhatKySql.BoDem BatDauDemSql() => NhatKySql.BatDauDem();
}
