using System.Data;
using DTO;

namespace DAL;

public class DonDatHangDAL
{
    private const string Cot = @"d.*, k.ho_ten AS ten_khach_hang, n.ho_ten AS ten_nhan_vien, h.ma_hoa_don,
        ht.trang_thai AS trang_thai_hoan_tien, ht.so_tien AS so_tien_hoan";
    private const string BoDonHuy = "('DA_HUY', 'TU_CHOI')";
    private const string Tu = @"FROM DON_DAT_HANG d
        JOIN KHACH_HANG k ON k.ma_kh = d.ma_kh
        LEFT JOIN NHAN_VIEN n ON n.ma_nv = d.ma_nv
        LEFT JOIN HOA_DON h ON h.ma_don = d.ma_don
        LEFT JOIN HOAN_TIEN ht ON ht.ma_don = d.ma_don";

    public void Them(DonDatHangDTO d, IDbTransaction tx)
    {
        DbHelper.Execute(@"INSERT INTO DON_DAT_HANG (ma_don, ma_kh, ma_nv, ngay_dat, trang_thai, dia_chi_giao, sdt_nhan,
                phuong_thuc_tt, ma_voucher, tong_tien, ten_nguoi_nhan, tien_giam, nguon, email_nhan, ma_km)
            VALUES (@MaDon, @MaKh, NULL, @NgayDat, 'CHO_XAC_NHAN', @DiaChiGiao, @SdtNhan, @PhuongThucTt, @MaVoucher,
                @TongTien, @TenNguoiNhan, @TienGiam, @Nguon, @EmailNhan, @MaKm)", d, tx);
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

    public List<DonDatHangDTO> LayDanhSach(string? trangThai, DateTime? tuNgay, DateTime? denNgay, string? nguon = null,
        string? yeuCauHuy = null) =>
        DbHelper.Query<DonDatHangDTO>($@"SELECT {Cot} {Tu}
            WHERE (@trangThai IS NULL OR d.trang_thai = @trangThai)
              AND (@tuNgay IS NULL OR d.ngay_dat >= @tuNgay)
              AND (@denNgay IS NULL OR d.ngay_dat < DATEADD(DAY, 1, @denNgay))
              AND (@nguon IS NULL OR d.nguon = @nguon)
              AND (@yeuCauHuy IS NULL OR d.yeu_cau_huy = @yeuCauHuy)
            ORDER BY d.ngay_dat DESC", new { trangThai, tuNgay, denNgay, nguon, yeuCauHuy });

    // v3: so don vang lai dang cho xac nhan cua 1 khach (chong dat don ao hang loat)
    public int DemDonVangLaiDangCho(string maKh, IDbTransaction tx) =>
        DbHelper.ExecuteScalar<int>(@"SELECT COUNT(*) FROM DON_DAT_HANG
            WHERE ma_kh = @maKh AND nguon = 'VANG_LAI' AND trang_thai = 'CHO_XAC_NHAN'", new { maKh }, tx);

    // v3: so don da dat bang tai khoan (khong tinh don huy / bi tu choi) - de tinh luot uu dai thanh vien moi
    public int DemDonTaiKhoan(string maKh, IDbTransaction? tx = null) =>
        DbHelper.ExecuteScalar<int>($@"SELECT COUNT(*) FROM DON_DAT_HANG
            WHERE ma_kh = @maKh AND nguon = 'TAI_KHOAN' AND trang_thai NOT IN {BoDonHuy}", new { maKh }, tx);

    // v3: lich su don cua 1 khach cho web staff danh gia rui ro.
    // Bom hang = don da duoc xac nhan (co hoa don) nhung sau do bi huy (khach khong nhan hang).
    // Khach xin huy truoc khi giao va cua hang dong y (yeu_cau_huy = DA_DUYET) thi khong tinh.
    public RuiRoKhachDTO DemLichSu(string maKh, IDbTransaction? tx = null) =>
        DbHelper.QueryFirstOrDefault<RuiRoKhachDTO>(@"SELECT
                ISNULL(SUM(CASE WHEN d.trang_thai = 'HOAN_TAT' THEN 1 ELSE 0 END), 0) AS so_don_hoan_tat,
                ISNULL(SUM(CASE WHEN d.trang_thai = 'CHO_XAC_NHAN' THEN 1 ELSE 0 END), 0) AS so_don_dang_cho,
                ISNULL(SUM(CASE WHEN d.trang_thai = 'TU_CHOI' THEN 1 ELSE 0 END), 0) AS so_don_bi_tu_choi,
                ISNULL(SUM(CASE WHEN d.trang_thai = 'DA_HUY' AND h.ma_hoa_don IS NOT NULL AND ISNULL(d.yeu_cau_huy, '') <> 'DA_DUYET' THEN 1 ELSE 0 END), 0) AS so_lan_bom_hang
            FROM DON_DAT_HANG d LEFT JOIN HOA_DON h ON h.ma_don = d.ma_don
            WHERE d.ma_kh = @maKh", new { maKh }, tx)!;

    // v3: ghi thoi diem / nguoi huy (goi ngay sau khi doi trang thai sang DA_HUY)
    public void GhiHuy(string ma, string nguoiHuy, string? lyDo, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE DON_DAT_HANG SET ngay_huy = SYSDATETIME(), nguoi_huy = @nguoiHuy,
                ly_do_huy = COALESCE(@lyDo, ly_do_huy)
            WHERE ma_don = @ma", new { ma, nguoiHuy, lyDo }, tx);

    // v3: khach xin huy don da xac nhan (chi 1 lan, chi khi chua giao)
    public bool GuiYeuCauHuy(string ma, string lyDo, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE DON_DAT_HANG SET yeu_cau_huy = 'CHO_DUYET', ly_do_huy = @lyDo, ngay_yeu_cau_huy = SYSDATETIME()
            WHERE ma_don = @ma AND trang_thai = 'DA_XAC_NHAN' AND yeu_cau_huy IS NULL", new { ma, lyDo }, tx) == 1;

    // v3: nhan vien duyet (DA_DUYET) / tu choi (TU_CHOI, kem phan hoi) yeu cau huy dang cho
    public bool XuLyYeuCauHuy(string ma, string sang, string? phanHoi, string maNv, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE DON_DAT_HANG SET yeu_cau_huy = @sang, phan_hoi_huy = @phanHoi, ma_nv = @maNv
            WHERE ma_don = @ma AND yeu_cau_huy = 'CHO_DUYET'", new { ma, sang, phanHoi, maNv }, tx) == 1;

    // Chi doi khi trang thai hien tai dung nhu mong doi (tranh 2 nhan vien cung xu ly 1 don)
    public bool DoiTrangThai(string ma, string tuTrangThai, string sangTrangThai, string? maNv, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE DON_DAT_HANG SET trang_thai = @sangTrangThai, ma_nv = COALESCE(@maNv, ma_nv)
            WHERE ma_don = @ma AND trang_thai = @tuTrangThai", new { ma, tuTrangThai, sangTrangThai, maNv }, tx) == 1;
}
