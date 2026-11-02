using System.Data;
using Dapper;
using DTO;

namespace DAL;

public class HoaDonDAL
{
    private const string Cot = @"h.*, k.ho_ten AS ten_khach_hang, k.tinh_thanh AS tinh_thanh_khach, n.ho_ten AS ten_nhan_vien, m.ten_km";
    private const string Tu = @"FROM HOA_DON h
        JOIN NHAN_VIEN n ON n.ma_nv = h.ma_nv
        LEFT JOIN KHACH_HANG k ON k.ma_kh = h.ma_kh
        LEFT JOIN KHUYEN_MAI m ON m.ma_km = h.ma_km";

    public void Them(HoaDonDTO h, IDbTransaction tx)
    {
        DbHelper.Execute(@"INSERT INTO HOA_DON (ma_hoa_don, ma_don, ma_kh, ma_nv, ngay_lap, trang_thai, ma_km, tien_giam,
                phuong_thuc_tt, tong_tien)
            VALUES (@MaHoaDon, @MaDon, @MaKh, @MaNv, @NgayLap, @TrangThai, @MaKm, @TienGiam, @PhuongThucTt, @TongTien)", h, tx);
        foreach (var c in h.ChiTiets)
            DbHelper.Execute(@"INSERT INTO CHI_TIET_HOA_DON (ma_hoa_don, ma_sp, so_luong, don_gia_ban, thanh_tien)
                VALUES (@MaHoaDon, @MaSp, @SoLuong, @DonGiaBan, @ThanhTien)", c, tx);
    }

    public HoaDonDTO? LayTheoMa(string ma, IDbTransaction? tx = null)
    {
        var h = DbHelper.QueryFirstOrDefault<HoaDonDTO>($"SELECT {Cot} {Tu} WHERE h.ma_hoa_don = @ma", new { ma }, tx);
        if (h != null) h.ChiTiets = LayChiTiet(ma, tx);
        return h;
    }

    // Main-sub form (yeu cau 4): bam vao hoa don nao thi chi chay cau nay de lay phan chi tiet ben phai
    public List<ChiTietHoaDonDTO> LayChiTiet(string ma, IDbTransaction? tx = null) =>
        DbHelper.Query<ChiTietHoaDonDTO>(@"SELECT c.*, s.ten_sp, s.kich_co, s.mau_sac
            FROM CHI_TIET_HOA_DON c JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp WHERE c.ma_hoa_don = @ma ORDER BY s.ten_sp",
            new { ma }, tx);

    public HoaDonDTO? LayTheoDon(string maDon, IDbTransaction? tx = null) =>
        DbHelper.QueryFirstOrDefault<HoaDonDTO>($"SELECT {Cot} {Tu} WHERE h.ma_don = @maDon", new { maDon }, tx);

    public List<HoaDonDTO> LayDanhSach(DateTime? tuNgay, DateTime? denNgay, string? trangThai, string? maNv, string? maKh) =>
        DbHelper.Query<HoaDonDTO>($@"SELECT {Cot} {Tu}
            WHERE (@tuNgay IS NULL OR h.ngay_lap >= @tuNgay) AND (@denNgay IS NULL OR h.ngay_lap <= @denNgay)
              AND (@trangThai IS NULL OR h.trang_thai = @trangThai)
              AND (@maNv IS NULL OR h.ma_nv = @maNv) AND (@maKh IS NULL OR h.ma_kh = @maKh)
            ORDER BY h.ngay_lap DESC, h.ma_hoa_don DESC", new { tuNgay, denNgay, trangThai, maNv, maKh });

    // Tim kiem nang cao (yeu cau 6): WHERE do BLL ghep tu danh sach truong + toan tu cho phep
    public List<HoaDonDTO> TimNangCao(string where, DynamicParameters thamSo) =>
        DbHelper.Query<HoaDonDTO>($"SELECT {Cot} {Tu} WHERE {where} ORDER BY h.tong_tien DESC", thamSo);

    public bool DoiTrangThai(string ma, string tuTrangThai, string sangTrangThai, IDbTransaction tx) =>
        DbHelper.Execute("UPDATE HOA_DON SET trang_thai = @sangTrangThai WHERE ma_hoa_don = @ma AND trang_thai = @tuTrangThai",
            new { ma, tuTrangThai, sangTrangThai }, tx) == 1;

    // Du lieu tho cho thong ke "tai su dung": tai 1 lan, BLL tu gom nhom
    public List<HoaDonDTO> LayDaThanhToan(DateTime? tuNgay = null, DateTime? denNgay = null) =>
        DbHelper.Query<HoaDonDTO>($@"SELECT {Cot} {Tu}
            WHERE h.trang_thai = 'DA_THANH_TOAN'
              AND (@tuNgay IS NULL OR h.ngay_lap >= @tuNgay) AND (@denNgay IS NULL OR h.ngay_lap <= @denNgay)",
            new { tuNgay, denNgay });

    public List<ChiTietHoaDonDTO> LayChiTietDaThanhToan() =>
        DbHelper.Query<ChiTietHoaDonDTO>(@"SELECT c.*, s.ten_sp, s.kich_co, s.mau_sac
            FROM CHI_TIET_HOA_DON c JOIN HOA_DON h ON h.ma_hoa_don = c.ma_hoa_don
            JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp WHERE h.trang_thai = 'DA_THANH_TOAN'");
}
