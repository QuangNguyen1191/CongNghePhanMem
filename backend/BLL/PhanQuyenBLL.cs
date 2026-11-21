using System.Globalization;
using System.Text;

namespace BLL;

// Phan quyen theo CHUC VU cua nhan vien (cot NHAN_VIEN.chuc_vu), ben trong vai tro NHAN_VIEN.
//   Vai tro (KHACH_HANG / NHAN_VIEN / ADMIN) quyet dinh dang nhap o dau va nhom chuc nang lon;
//   nhom quyen quyet dinh nhan vien nao lam duoc viec nao: thu ngan khong lap phieu nhap, thu kho khong ban hang...
// WebApi kiem tra nhom quyen o moi API nhan vien (CanQuyenAttribute), web staff an menu theo cung danh sach nay.
// ADMIN co tat ca nhom quyen.
public static class NhomQuyen
{
    public const string BanHang = "ban-hang";        // ban tai quay, huy hoa don tai quay
    public const string DonOnline = "don-online";    // xac nhan / giao / hoan tat / huy don online
    public const string Kho = "kho";                 // nhap hang, nha cung cap

    public static readonly IReadOnlyList<string> TatCa = new[] { BanHang, DonOnline, Kho };

    public static readonly IReadOnlyDictionary<string, string> Ten = new Dictionary<string, string>
    {
        [BanHang] = "bán tại quầy", [DonOnline] = "xử lý đơn online", [Kho] = "kho / nhập hàng",
    };

    // Quy tac theo tu khoa trong chuc vu (khong phan biet dau, hoa thuong):
    //   "quan ly" / "giam doc" -> tat ca | "thu ngan" / "ban hang tai quay" -> ban-hang | "online" -> don-online | "kho" -> kho
    // Mot chuc vu co the co nhieu nhom (vd "Thu ngân kiêm thủ kho").
    // Chuc vu khong khop tu khoa nao -> KHONG co nhom nao (mac dinh an toan): chi xem tong quan / san pham, cho admin dat dung chuc vu.
    public static List<string> TheoChucVu(string vaiTro, string? chucVu)
    {
        if (vaiTro == "ADMIN") return TatCa.ToList();
        if (vaiTro != "NHAN_VIEN") return new();
        var cv = BoDau(chucVu ?? "");
        if (cv.Contains("quan ly") || cv.Contains("giam doc")) return TatCa.ToList();
        var ds = new List<string>();
        if (cv.Contains("thu ngan") || cv.Contains("ban hang tai quay")) ds.Add(BanHang);
        if (cv.Contains("online")) ds.Add(DonOnline);
        if (cv.Contains("kho")) ds.Add(Kho);
        return ds;
    }

    // Giong boDau() cua web staff: bo dau tieng Viet, d -> d, chu thuong
    private static string BoDau(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();
    }
}
