using System.Data;
using DTO;

namespace DAL;

public class TaiKhoanDAL
{
    private const string Cot = @"t.ma_tk, t.ten_dang_nhap, t.mat_khau, t.vai_tro, t.trang_thai, t.ngay_tao,
        n.ma_nv, k.ma_kh, COALESCE(n.ho_ten, k.ho_ten) AS ho_ten";
    private const string Tu = @"FROM TAI_KHOAN t
        LEFT JOIN NHAN_VIEN n ON n.ma_tk = t.ma_tk
        LEFT JOIN KHACH_HANG k ON k.ma_tk = t.ma_tk";

    public TaiKhoanDTO? LayTheoTenDangNhap(string tenDangNhap) =>
        DbHelper.QueryFirstOrDefault<TaiKhoanDTO>($"SELECT {Cot} {Tu} WHERE t.ten_dang_nhap = @tenDangNhap", new { tenDangNhap });

    public TaiKhoanDTO? LayTheoMa(string ma) =>
        DbHelper.QueryFirstOrDefault<TaiKhoanDTO>($"SELECT {Cot} {Tu} WHERE t.ma_tk = @ma", new { ma });

    // Man hinh quan ly tai khoan: moi tai khoan kem ma NV hoac ma KH
    public List<TaiKhoanDTO> LayTatCa() =>
        DbHelper.Query<TaiKhoanDTO>($"SELECT {Cot} {Tu} ORDER BY t.vai_tro, t.ma_tk");

    public bool TonTaiTenDangNhap(string tenDangNhap) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM TAI_KHOAN WHERE ten_dang_nhap = @tenDangNhap", new { tenDangNhap }) > 0;

    public void Them(TaiKhoanDTO t, IDbTransaction tx) =>
        DbHelper.Execute(@"INSERT INTO TAI_KHOAN (ma_tk, ten_dang_nhap, mat_khau, vai_tro, trang_thai)
            VALUES (@MaTk, @TenDangNhap, @MatKhau, @VaiTro, @TrangThai)", t, tx);

    public int DoiMatKhau(string ma, string matKhauHash) =>
        DbHelper.Execute("UPDATE TAI_KHOAN SET mat_khau = @matKhauHash WHERE ma_tk = @ma", new { ma, matKhauHash });

    public int DoiTrangThai(string ma, string trangThai, IDbTransaction? tx = null) =>
        DbHelper.Execute("UPDATE TAI_KHOAN SET trang_thai = @trangThai WHERE ma_tk = @ma", new { ma, trangThai }, tx);
}

public class NhanVienDAL
{
    private const string Cot = @"n.ma_nv, n.ma_tk, n.ho_ten, n.chuc_vu, n.ngay_vao_lam, n.luong, n.sdt, n.email, n.trang_thai,
        t.ten_dang_nhap, t.vai_tro";

    public List<NhanVienDTO> LayTatCa() =>
        DbHelper.Query<NhanVienDTO>($"SELECT {Cot} FROM NHAN_VIEN n JOIN TAI_KHOAN t ON t.ma_tk = n.ma_tk ORDER BY n.ma_nv");

    public NhanVienDTO? LayTheoMa(string ma) =>
        DbHelper.QueryFirstOrDefault<NhanVienDTO>(
            $"SELECT {Cot} FROM NHAN_VIEN n JOIN TAI_KHOAN t ON t.ma_tk = n.ma_tk WHERE n.ma_nv = @ma", new { ma });

    public bool TrungEmail(string email, string? boQuaMa = null) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM NHAN_VIEN WHERE email = @email AND ma_nv <> ISNULL(@boQuaMa, '')",
            new { email, boQuaMa }) > 0;

    public void Them(NhanVienDTO n, IDbTransaction tx) =>
        DbHelper.Execute(@"INSERT INTO NHAN_VIEN (ma_nv, ma_tk, ho_ten, chuc_vu, ngay_vao_lam, luong, sdt, email, trang_thai)
            VALUES (@MaNv, @MaTk, @HoTen, @ChucVu, @NgayVaoLam, @Luong, @Sdt, @Email, @TrangThai)", n, tx);

    public int Sua(NhanVienDTO n) =>
        DbHelper.Execute(@"UPDATE NHAN_VIEN SET ho_ten = @HoTen, chuc_vu = @ChucVu, ngay_vao_lam = @NgayVaoLam,
                luong = @Luong, sdt = @Sdt, email = @Email WHERE ma_nv = @MaNv", n);

    public int DoiTrangThai(string ma, string trangThai, IDbTransaction tx) =>
        DbHelper.Execute("UPDATE NHAN_VIEN SET trang_thai = @trangThai WHERE ma_nv = @ma", new { ma, trangThai }, tx);
}

public class KhachHangDAL
{
    private const string Cot = @"ma_kh, ma_tk, ho_ten, sdt, email, ngay_sinh, hang_thanh_vien, diem_tich_luy, dia_chi, tinh_thanh";

    public List<KhachHangDTO> LayTatCa() =>
        DbHelper.Query<KhachHangDTO>($"SELECT {Cot} FROM KHACH_HANG ORDER BY ma_kh");

    public KhachHangDTO? LayTheoMa(string ma, IDbTransaction? tx = null) =>
        DbHelper.QueryFirstOrDefault<KhachHangDTO>($"SELECT {Cot} FROM KHACH_HANG WHERE ma_kh = @ma", new { ma }, tx);

    public KhachHangDTO? LayTheoSdt(string sdt) =>
        DbHelper.QueryFirstOrDefault<KhachHangDTO>($"SELECT {Cot} FROM KHACH_HANG WHERE sdt = @sdt", new { sdt });

    public bool TrungEmail(string email, string? boQuaMa = null) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM KHACH_HANG WHERE email = @email AND ma_kh <> ISNULL(@boQuaMa, '')",
            new { email, boQuaMa }) > 0;

    public void Them(KhachHangDTO k, IDbTransaction tx) =>
        DbHelper.Execute(@"INSERT INTO KHACH_HANG (ma_kh, ma_tk, ho_ten, sdt, email, ngay_sinh, hang_thanh_vien, diem_tich_luy,
                dia_chi, tinh_thanh)
            VALUES (@MaKh, @MaTk, @HoTen, @Sdt, @Email, @NgaySinh, 'DONG', 0, @DiaChi, @TinhThanh)", k, tx);

    public int Sua(KhachHangDTO k) =>
        DbHelper.Execute(@"UPDATE KHACH_HANG SET ho_ten = @HoTen, sdt = @Sdt, email = @Email, ngay_sinh = @NgaySinh,
                dia_chi = @DiaChi, tinh_thanh = @TinhThanh WHERE ma_kh = @MaKh", k);

    // Khach mua tai quay dang ky tai khoan sau: gan tai khoan vao ho so cu, giu lich su mua
    public int GanTaiKhoan(KhachHangDTO k, string maTk, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE KHACH_HANG SET ma_tk = @maTk, ho_ten = @HoTen, email = @Email, ngay_sinh = @NgaySinh,
                dia_chi = @DiaChi, tinh_thanh = @TinhThanh WHERE ma_kh = @MaKh AND ma_tk IS NULL",
            new { maTk, k.HoTen, k.Email, k.NgaySinh, k.DiaChi, k.TinhThanh, k.MaKh }, tx);

    public int SuaHangDiem(string ma, string hang, int diem, IDbTransaction? tx = null) =>
        DbHelper.Execute("UPDATE KHACH_HANG SET hang_thanh_vien = @hang, diem_tich_luy = @diem WHERE ma_kh = @ma",
            new { ma, hang, diem }, tx);

    public int CongDiem(string ma, int diem, IDbTransaction tx) =>
        DbHelper.Execute("UPDATE KHACH_HANG SET diem_tich_luy = diem_tich_luy + @diem WHERE ma_kh = @ma", new { ma, diem }, tx);

    // Tong tien da mua (hoa don da thanh toan) de xet hang thanh vien
    public decimal TongDaMua(string ma, IDbTransaction tx) =>
        DbHelper.ExecuteScalar<decimal?>(@"SELECT SUM(tong_tien) FROM HOA_DON
            WHERE ma_kh = @ma AND trang_thai = 'DA_THANH_TOAN'", new { ma }, tx) ?? 0;

    // Yeu cau 6 - HOP: khach o 1 trong cac tinh thanh (UNION tung tinh)
    public List<KhachHangDTO> TheoTinhThanhHop(string tinhA, string tinhB) =>
        DbHelper.Query<KhachHangDTO>($@"SELECT {Cot} FROM KHACH_HANG WHERE tinh_thanh = @tinhA
            UNION
            SELECT {Cot} FROM KHACH_HANG WHERE tinh_thanh = @tinhB
            ORDER BY ma_kh", new { tinhA, tinhB });

    // Yeu cau 6 - GIAO: khach o tinh A va da tung co don giao toi tinh B
    public List<KhachHangDTO> OTinhACoDonGiaoTinhB(string tinhA, string tinhB) =>
        DbHelper.Query<KhachHangDTO>($@"SELECT {Cot} FROM KHACH_HANG WHERE ma_kh IN (
                SELECT ma_kh FROM KHACH_HANG WHERE tinh_thanh = @tinhA
                INTERSECT
                SELECT ma_kh FROM DON_DAT_HANG WHERE dia_chi_giao LIKE @mauB)
            ORDER BY ma_kh", new { tinhA, mauB = "%" + tinhB + "%" });

    // Yeu cau 6 - GIAO: khach vua mua tai quay vua dat hang online
    public List<KhachHangDTO> VuaMuaTaiQuayVuaDatOnline() =>
        DbHelper.Query<KhachHangDTO>($@"SELECT {Cot} FROM KHACH_HANG WHERE ma_kh IN (
                SELECT ma_kh FROM HOA_DON WHERE ma_don IS NULL AND ma_kh IS NOT NULL
                INTERSECT
                SELECT ma_kh FROM DON_DAT_HANG)
            ORDER BY ma_kh");

    // Tim kiem nang cao: menh de WHERE do BLL ghep tu danh sach truong cho phep, gia tri luon la tham so
    public List<KhachHangDTO> TimNangCao(string where, Dapper.DynamicParameters thamSo) =>
        DbHelper.Query<KhachHangDTO>($"SELECT {Cot} FROM KHACH_HANG WHERE {where} ORDER BY ma_kh", thamSo);
}
