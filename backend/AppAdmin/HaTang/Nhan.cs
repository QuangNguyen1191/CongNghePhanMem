namespace AppAdmin.HaTang;

// Nhan tieng Viet + mau cho cac ma trang thai trong CSDL (giong web-staff/src/lib/nhan.tsx).
// Mau chu luon di kem chu, khong chi dua vao mau.
public static class Nhan
{
    public sealed record Kieu(string Chu, Color Mau);

    private static Kieu K(string chu, Color mau) => new(chu, mau);

    public static readonly Dictionary<string, Kieu> TrangThaiDon = new()
    {
        ["CHO_XAC_NHAN"] = K("Chờ xác nhận", GiaoDien.Vang),
        ["DA_XAC_NHAN"] = K("Đã xác nhận", GiaoDien.XanhDuong),
        ["DANG_GIAO"] = K("Đang giao", GiaoDien.XanhDuong),
        ["HOAN_TAT"] = K("Hoàn tất", GiaoDien.XanhLa),
        ["DA_HUY"] = K("Đã hủy", GiaoDien.Xam),
        ["TU_CHOI"] = K("Từ chối", GiaoDien.Do),
    };

    public static readonly Dictionary<string, Kieu> TrangThaiHoaDon = new()
    {
        ["CHO_THANH_TOAN"] = K("Chờ thanh toán", GiaoDien.Vang),
        ["DA_THANH_TOAN"] = K("Đã thanh toán", GiaoDien.XanhLa),
        ["DA_HUY"] = K("Đã hủy", GiaoDien.Xam),
    };

    public static readonly Dictionary<string, Kieu> TrangThaiDoiTra = new()
    {
        ["CHO_DUYET"] = K("Chờ duyệt", GiaoDien.Vang),
        ["DA_DUYET"] = K("Đã duyệt", GiaoDien.XanhDuong),
        ["TU_CHOI"] = K("Từ chối", GiaoDien.Do),
        ["HOAN_TAT"] = K("Hoàn tất", GiaoDien.XanhLa),
    };

    public static readonly Dictionary<string, Kieu> TrangThaiHoanTien = new()
    {
        ["CHO_HOAN"] = K("Chờ hoàn tiền", GiaoDien.Vang),
        ["DA_HOAN"] = K("Đã hoàn", GiaoDien.XanhLa),
    };

    public static readonly Dictionary<string, Kieu> YeuCauHuy = new()
    {
        ["CHO_DUYET"] = K("Khách xin hủy", GiaoDien.Do),
        ["DA_DUYET"] = K("Đã duyệt hủy", GiaoDien.Xam),
        ["TU_CHOI"] = K("Từ chối hủy", GiaoDien.Vang),
    };

    public static readonly Dictionary<string, Kieu> RuiRo = new()
    {
        ["THAP"] = K("Rủi ro thấp", GiaoDien.XanhLa),
        ["TRUNG_BINH"] = K("Rủi ro trung bình", GiaoDien.Vang),
        ["CAO"] = K("Rủi ro cao", GiaoDien.Do),
    };

    public static readonly Dictionary<string, Kieu> HangThanhVien = new()
    {
        ["DONG"] = K("Đồng", Color.FromArgb(0x9A, 0x55, 0x1E)),
        ["BAC"] = K("Bạc", GiaoDien.Xam),
        ["VANG"] = K("Vàng", GiaoDien.Vang),
        ["KIM_CUONG"] = K("Kim cương", GiaoDien.XanhDuong),
    };

    public static readonly Dictionary<string, Kieu> TrangThaiSanPham = new()
    {
        ["DANG_BAN"] = K("Đang bán", GiaoDien.XanhLa),
        ["NGUNG_BAN"] = K("Ngừng bán", GiaoDien.Xam),
    };

    public static readonly Dictionary<string, Kieu> TrangThaiNcc = new()
    {
        ["HOP_TAC"] = K("Hợp tác", GiaoDien.XanhLa),
        ["NGUNG_HOP_TAC"] = K("Ngừng hợp tác", GiaoDien.Xam),
    };

    public static readonly Dictionary<string, Kieu> TrangThaiNhanVien = new()
    {
        ["DANG_LAM"] = K("Đang làm", GiaoDien.XanhLa),
        ["NGHI_VIEC"] = K("Nghỉ việc", GiaoDien.Xam),
    };

    public static readonly Dictionary<string, Kieu> TrangThaiTaiKhoan = new()
    {
        ["HOAT_DONG"] = K("Hoạt động", GiaoDien.XanhLa),
        ["KHOA"] = K("Đã khóa", GiaoDien.Do),
    };

    public static readonly Dictionary<string, Kieu> VaiTro = new()
    {
        ["ADMIN"] = K("Quản trị (ADMIN)", GiaoDien.Do),
        ["NHAN_VIEN"] = K("Nhân viên", GiaoDien.XanhDuong),
        ["KHACH_HANG"] = K("Khách hàng", GiaoDien.ChuChinh),
    };

    public static readonly Dictionary<string, Kieu> HinhThucKm = new()
    {
        ["SAN_PHAM"] = K("Giảm theo sản phẩm", GiaoDien.ChuChinh),
        ["HOA_DON"] = K("Giảm theo hóa đơn", GiaoDien.ChuChinh),
        ["VOUCHER"] = K("Mã voucher", GiaoDien.ChuChinh),
        ["THANH_VIEN_MOI"] = K("Ưu đãi thành viên mới", GiaoDien.ChuChinh),
    };

    public static readonly Dictionary<string, Kieu> NguonDon = new()
    {
        ["TAI_KHOAN"] = K("Tài khoản", GiaoDien.ChuChinh),
        ["VANG_LAI"] = K("Vãng lai", GiaoDien.Vang),
    };

    public static readonly Dictionary<string, string> PhuongThuc = new()
    {
        ["TIEN_MAT"] = "Tiền mặt",
        ["COD"] = "COD",
        ["CHUYEN_KHOAN"] = "Chuyển khoản",
        ["ONLINE"] = "Thanh toán online",
    };

    public static readonly Dictionary<string, string> LoaiDoiTra = new()
    {
        ["DOI"] = "Đổi size / màu",
        ["TRA"] = "Trả hàng",
    };

    public static string Ten(Dictionary<string, Kieu> bang, string? ma) =>
        ma == null ? "" : bang.TryGetValue(ma, out var k) ? k.Chu : ma;

    public static Color? Mau(Dictionary<string, Kieu> bang, string? ma) =>
        ma != null && bang.TryGetValue(ma, out var k) ? k.Mau : null;

    public static string Ten(Dictionary<string, string> bang, string? ma) =>
        ma == null ? "" : bang.TryGetValue(ma, out var t) ? t : ma;

    // Danh sach cho ComboBox: "Tat ca" + cac muc cua bang
    public static IEnumerable<Muc> CacMuc(Dictionary<string, Kieu> bang, string? tatCa = "Tất cả")
    {
        if (tatCa != null) yield return new Muc(null, tatCa);
        foreach (var (ma, k) in bang) yield return new Muc(ma, k.Chu);
    }

    public static IEnumerable<Muc> CacMuc(Dictionary<string, string> bang, string? tatCa = "Tất cả")
    {
        if (tatCa != null) yield return new Muc(null, tatCa);
        foreach (var (ma, t) in bang) yield return new Muc(ma, t);
    }
}
