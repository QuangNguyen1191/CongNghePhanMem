using System.Data;
using DTO;

namespace DAL;

public class DonDatHangDAL
{
    private const string Cot = @"d.*, k.ho_ten AS ten_khach_hang, n.ho_ten AS ten_nhan_vien, h.ma_hoa_don";
    private const string Tu = @"FROM DON_DAT_HANG d
        JOIN KHACH_HANG k ON k.ma_kh = d.ma_kh
        LEFT JOIN NHAN_VIEN n ON n.ma_nv = d.ma_nv
        LEFT JOIN HOA_DON h ON h.ma_don = d.ma_don";

    public void Them(DonDatHangDTO d, IDbTransaction tx)
    {
        DbHelper.Execute(@"INSERT INTO DON_DAT_HANG (ma_don, ma_kh, ma_nv, ngay_dat, trang_thai, dia_chi_giao, sdt_nhan,
                phuong_thuc_tt, ma_voucher, tong_tien, ten_nguoi_nhan, tien_giam)
            VALUES (@MaDon, @MaKh, NULL, @NgayDat, 'CHO_XAC_NHAN', @DiaChiGiao, @SdtNhan, @PhuongThucTt, @MaVoucher,
                @TongTien, @TenNguoiNhan, @TienGiam)", d, tx);
        foreach (var c in d.ChiTiets)
            DbHelper.Execute(@"INSERT INTO CHI_TIET_DON_DAT_HANG (ma_don, ma_sp, so_luong, don_gia, thanh_tien)
                VALUES (@MaDon, @MaSp, @SoLuong, @DonGia, @ThanhTien)", c, tx);
    }

    public DonDatHangDTO? LayTheoMa(string ma, IDbTransaction? tx = null)
    {
        var d = DbHelper.QueryFirstOrDefault<DonDatHangDTO>($"SELECT {Cot} {Tu} WHERE d.ma_don = @ma", new { ma }, tx);
        if (d != null) d.ChiTiets = LayChiTiet(ma, tx);
        return d;
    }

    public List<ChiTietDonDatHangDTO> LayChiTiet(string ma, IDbTransaction? tx = null) =>
        DbHelper.Query<ChiTietDonDatHangDTO>(@"SELECT c.*, s.ten_sp, s.kich_co, s.mau_sac
            FROM CHI_TIET_DON_DAT_HANG c JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp WHERE c.ma_don = @ma", new { ma }, tx);

    public List<DonDatHangDTO> LayTheoKhach(string maKh) =>
        DbHelper.Query<DonDatHangDTO>($"SELECT {Cot} {Tu} WHERE d.ma_kh = @maKh ORDER BY d.ngay_dat DESC", new { maKh });

    public List<DonDatHangDTO> LayDanhSach(string? trangThai, DateTime? tuNgay, DateTime? denNgay) =>
        DbHelper.Query<DonDatHangDTO>($@"SELECT {Cot} {Tu}
            WHERE (@trangThai IS NULL OR d.trang_thai = @trangThai)
              AND (@tuNgay IS NULL OR d.ngay_dat >= @tuNgay)
              AND (@denNgay IS NULL OR d.ngay_dat < DATEADD(DAY, 1, @denNgay))
            ORDER BY d.ngay_dat DESC", new { trangThai, tuNgay, denNgay });

    // Chi doi khi trang thai hien tai dung nhu mong doi (tranh 2 nhan vien cung xu ly 1 don)
    public bool DoiTrangThai(string ma, string tuTrangThai, string sangTrangThai, string? maNv, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE DON_DAT_HANG SET trang_thai = @sangTrangThai, ma_nv = COALESCE(@maNv, ma_nv)
            WHERE ma_don = @ma AND trang_thai = @tuTrangThai", new { ma, tuTrangThai, sangTrangThai, maNv }, tx) == 1;
}
