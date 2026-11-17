using System.Text.Json;
using System.Text.Json.Nodes;

namespace AppAdmin.HaTang;

// Doc / ghi appsettings.json canh file .exe. Cung ten khoa voi WebApi/appsettings.json de de doi chieu.
public static class CauHinhApp
{
    private static readonly string DuongDan = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    private static JsonObject _goc = new();

    public static string ChuoiKetNoi
    {
        get => Lay("ConnectionStrings", "QLBanThoiTrang")
               ?? "Server=localhost;Database=QL_BanThoiTrang;Trusted_Connection=True;TrustServerCertificate=True";
        set => Dat("ConnectionStrings", "QLBanThoiTrang", value);
    }

    public static string TenCuaHang => Lay("CuaHang", "Ten") ?? "UNIQLO";
    public static string DiaChiCuaHang => Lay("CuaHang", "DiaChi") ?? "";
    public static string SdtCuaHang => Lay("CuaHang", "Sdt") ?? "";
    public static bool InSqlRaConsole => _goc["NhatKySql"]?["InRaConsole"]?.GetValue<bool>() ?? false;

    public static string ThuMucXuat
    {
        get
        {
            var s = Lay("App", "ThuMucXuat");
            return string.IsNullOrWhiteSpace(s)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QLBanThoiTrang")
                : s;
        }
    }

    public static string TenDangNhapCuoi
    {
        get => Lay("App", "TenDangNhapCuoi") ?? "";
        set => Dat("App", "TenDangNhapCuoi", value);
    }

    public static void Doc()
    {
        try
        {
            if (File.Exists(DuongDan))
                _goc = JsonNode.Parse(File.ReadAllText(DuongDan), documentOptions: new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
                }) as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            _goc = new JsonObject();      // file hong thi dung mac dinh, luc luu se ghi lai file moi
        }
    }

    public static void Luu() =>
        File.WriteAllText(DuongDan, _goc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    // Nap cau hinh vao BLL: chuoi ket noi, nhat ky SQL, thong tin in tren chung tu PDF
    public static void ApDung()
    {
        HeThongBLL.CauHinh(ChuoiKetNoi, InSqlRaConsole);
        ThongTinCuaHang.Ten = TenCuaHang;
        ThongTinCuaHang.DiaChi = DiaChiCuaHang;
        ThongTinCuaHang.Sdt = SdtCuaHang;
    }

    private static string? Lay(string nhom, string khoa) => _goc[nhom]?[khoa]?.GetValue<string>();

    private static void Dat(string nhom, string khoa, string giaTri)
    {
        if (_goc[nhom] is not JsonObject o)
        {
            o = new JsonObject();
            _goc[nhom] = o;
        }
        o[khoa] = giaTri;
    }
}

// Nguoi dang dang nhap app (chi tai khoan ADMIN vao duoc, xem TaiKhoanBLL.DangNhap)
public static class Phien
{
    public static PhienDangNhapDTO? Hien { get; set; }

    public static string MaTk => Hien?.MaTk ?? throw new LoiNghiepVu("Chưa đăng nhập");

    // Cac thao tac lap chung tu (phieu nhap, hoa don, duyet don...) can ma nhan vien cua admin
    public static string MaNv => Hien?.MaNv ?? throw new LoiNghiepVu("Tài khoản admin này chưa gắn với nhân viên nào");

    public static string HoTen => Hien?.HoTen ?? "";
}
