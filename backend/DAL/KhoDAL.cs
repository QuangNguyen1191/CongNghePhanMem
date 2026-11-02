using System.Data;
using DTO;

namespace DAL;

public class PhieuNhapDAL
{
    private const string Cot = @"p.*, c.ten_ncc, n.ho_ten AS ten_nhan_vien";
    private const string Tu = @"FROM PHIEU_NHAP p JOIN NHA_CUNG_CAP c ON c.ma_ncc = p.ma_ncc JOIN NHAN_VIEN n ON n.ma_nv = p.ma_nv";

    public void Them(PhieuNhapDTO p, IDbTransaction tx)
    {
        DbHelper.Execute(@"INSERT INTO PHIEU_NHAP (ma_phieu_nhap, ma_ncc, ma_nv, ngay_nhap, tong_tien)
            VALUES (@MaPhieuNhap, @MaNcc, @MaNv, @NgayNhap, @TongTien)", p, tx);
        foreach (var c in p.ChiTiets)
            DbHelper.Execute(@"INSERT INTO CHI_TIET_PHIEU_NHAP (ma_phieu_nhap, ma_sp, so_luong, don_gia_nhap, thanh_tien)
                VALUES (@MaPhieuNhap, @MaSp, @SoLuong, @DonGiaNhap, @ThanhTien)", c, tx);
    }

    public PhieuNhapDTO? LayTheoMa(string ma)
    {
        var p = DbHelper.QueryFirstOrDefault<PhieuNhapDTO>($"SELECT {Cot} {Tu} WHERE p.ma_phieu_nhap = @ma", new { ma });
        if (p != null) p.ChiTiets = LayChiTiet(ma);
        return p;
    }

    public List<ChiTietPhieuNhapDTO> LayChiTiet(string ma) =>
        DbHelper.Query<ChiTietPhieuNhapDTO>(@"SELECT c.*, s.ten_sp, s.kich_co, s.mau_sac
            FROM CHI_TIET_PHIEU_NHAP c JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp WHERE c.ma_phieu_nhap = @ma", new { ma });

    public List<PhieuNhapDTO> LayDanhSach(DateTime? tuNgay, DateTime? denNgay, string? maNcc) =>
        DbHelper.Query<PhieuNhapDTO>($@"SELECT {Cot} {Tu}
            WHERE (@tuNgay IS NULL OR p.ngay_nhap >= @tuNgay) AND (@denNgay IS NULL OR p.ngay_nhap <= @denNgay)
              AND (@maNcc IS NULL OR p.ma_ncc = @maNcc)
            ORDER BY p.ngay_nhap DESC, p.ma_phieu_nhap DESC", new { tuNgay, denNgay, maNcc });
}

public class DoiTraDAL
{
    private const string Cot = @"d.*, h.ma_kh, k.ho_ten AS ten_khach_hang, n.ho_ten AS ten_nhan_vien";
    private const string Tu = @"FROM DOI_TRA_HANG d JOIN HOA_DON h ON h.ma_hoa_don = d.ma_hoa_don
        LEFT JOIN KHACH_HANG k ON k.ma_kh = h.ma_kh LEFT JOIN NHAN_VIEN n ON n.ma_nv = d.ma_nv";

    public void Them(DoiTraDTO d, IDbTransaction tx)
    {
        DbHelper.Execute(@"INSERT INTO DOI_TRA_HANG (ma_doi_tra, ma_hoa_don, ma_nv, loai, ly_do, ngay_doi_tra, trang_thai,
                tien_hoan, ngay_hoan)
            VALUES (@MaDoiTra, @MaHoaDon, @MaNv, @Loai, @LyDo, @NgayDoiTra, 'CHO_DUYET', 0, NULL)", d, tx);
        foreach (var c in d.ChiTiets)
            DbHelper.Execute(@"INSERT INTO CHI_TIET_DOI_TRA (ma_doi_tra, ma_sp, so_luong, ma_sp_doi)
                VALUES (@MaDoiTra, @MaSp, @SoLuong, @MaSpDoi)", c, tx);
    }

    public DoiTraDTO? LayTheoMa(string ma, IDbTransaction? tx = null)
    {
        var d = DbHelper.QueryFirstOrDefault<DoiTraDTO>($"SELECT {Cot} {Tu} WHERE d.ma_doi_tra = @ma", new { ma }, tx);
        if (d != null) d.ChiTiets = LayChiTiet(ma, tx);
        return d;
    }

    public List<ChiTietDoiTraDTO> LayChiTiet(string ma, IDbTransaction? tx = null) =>
        DbHelper.Query<ChiTietDoiTraDTO>(@"SELECT c.*, s.ten_sp, sd.ten_sp + ISNULL(' - ' + sd.mau_sac, '')
                + ISNULL(' - ' + sd.kich_co, '') AS ten_sp_doi
            FROM CHI_TIET_DOI_TRA c JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp LEFT JOIN SAN_PHAM sd ON sd.ma_sp = c.ma_sp_doi
            WHERE c.ma_doi_tra = @ma", new { ma }, tx);

    public List<DoiTraDTO> LayDanhSach(string? trangThai, string? maKh) =>
        DbHelper.Query<DoiTraDTO>($@"SELECT {Cot} {Tu}
            WHERE (@trangThai IS NULL OR d.trang_thai = @trangThai) AND (@maKh IS NULL OR h.ma_kh = @maKh)
            ORDER BY d.ngay_doi_tra DESC, d.ma_doi_tra DESC", new { trangThai, maKh });

    // So luong 1 SP cua hoa don da / dang doi tra (khong tinh phieu bi tu choi) de chan doi tra vuot so da mua
    public int SoLuongDaDoiTra(string maHoaDon, string maSp, IDbTransaction tx) =>
        DbHelper.ExecuteScalar<int?>(@"SELECT SUM(c.so_luong) FROM CHI_TIET_DOI_TRA c
            JOIN DOI_TRA_HANG d ON d.ma_doi_tra = c.ma_doi_tra
            WHERE d.ma_hoa_don = @maHoaDon AND c.ma_sp = @maSp AND d.trang_thai <> 'TU_CHOI'",
            new { maHoaDon, maSp }, tx) ?? 0;

    // Chi doi khi trang thai hien tai dung la tuTrangThai (tranh 2 nhan vien cung xu ly 1 phieu)
    public bool DoiTrangThai(string ma, string tuTrangThai, string sangTrangThai, string maNv, decimal tienHoan,
        DateTime? ngayHoan, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE DOI_TRA_HANG SET trang_thai = @sangTrangThai, ma_nv = @maNv, tien_hoan = @tienHoan,
                ngay_hoan = @ngayHoan
            WHERE ma_doi_tra = @ma AND trang_thai = @tuTrangThai",
            new { ma, tuTrangThai, sangTrangThai, maNv, tienHoan, ngayHoan }, tx) == 1;
}
