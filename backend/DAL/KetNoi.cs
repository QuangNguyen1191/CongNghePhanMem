using Microsoft.Data.SqlClient;

namespace DAL;

// Chuoi ket noi dung chung cho ca app admin va WebApi (doc tu appsettings / App.config)
public static class KetNoi
{
    public static string ChuoiKetNoi { get; set; } =
        "Server=localhost;Database=QL_BanThoiTrang;Trusted_Connection=True;TrustServerCertificate=True";

    public static SqlConnection Mo()
    {
        var cn = new SqlConnection(ChuoiKetNoi);
        cn.Open();
        return cn;
    }
}
