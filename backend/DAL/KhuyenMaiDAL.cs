using System.Data;
using DTO;

namespace DAL;

public class KhuyenMaiDAL
{
    public List<KhuyenMaiDTO> LayTatCa() =>
        DbHelper.Query<KhuyenMaiDTO>("SELECT * FROM KHUYEN_MAI ORDER BY ngay_bd DESC");

    public KhuyenMaiDTO? LayTheoMa(string ma)
    {
        var km = DbHelper.QueryFirstOrDefault<KhuyenMaiDTO>("SELECT * FROM KHUYEN_MAI WHERE ma_km = @ma", new { ma });
        if (km == null) return null;
        km.SanPhams = DbHelper.Query<CtKmSanPhamDTO>("SELECT * FROM CT_KM_SAN_PHAM WHERE ma_km = @ma", new { ma });
        km.MucHoaDons = DbHelper.Query<CtKmHoaDonDTO>(
            "SELECT * FROM CT_KM_HOA_DON WHERE ma_km = @ma ORDER BY muc_ap_dung_hoa_don", new { ma });
        km.Vouchers = DbHelper.Query<CtKmVoucherDTO>("SELECT * FROM CT_KM_VOUCHER WHERE ma_km = @ma", new { ma });
        km.ThanhVienMoi = DbHelper.QueryFirstOrDefault<CtKmThanhVienMoiDTO>(
            "SELECT * FROM CT_KM_THANH_VIEN_MOI WHERE ma_km = @ma", new { ma });
        return km;
    }

    // Khuyen mai dang chay vao ngay chi dinh
    public List<KhuyenMaiDTO> LayDangChay(DateTime ngay) =>
        DbHelper.Query<KhuyenMaiDTO>("SELECT * FROM KHUYEN_MAI WHERE @ngay BETWEEN ngay_bd AND ngay_kt ORDER BY ngay_kt",
            new { ngay = ngay.Date });

    public List<CtKmSanPhamDTO> LayGiamSanPhamDangChay(DateTime ngay, IDbTransaction? tx = null) =>
        DbHelper.Query<CtKmSanPhamDTO>(@"SELECT c.ma_km, c.ma_sp, c.phan_tram_giam_gia
            FROM CT_KM_SAN_PHAM c JOIN KHUYEN_MAI k ON k.ma_km = c.ma_km
            WHERE @ngay BETWEEN k.ngay_bd AND k.ngay_kt", new { ngay = ngay.Date }, tx);

    public List<CtKmHoaDonDTO> LayMucHoaDonDangChay(DateTime ngay, IDbTransaction? tx = null) =>
        DbHelper.Query<CtKmHoaDonDTO>(@"SELECT c.ma_km, c.muc_ap_dung_hoa_don, c.phan_tram_giam_gia
            FROM CT_KM_HOA_DON c JOIN KHUYEN_MAI k ON k.ma_km = c.ma_km
            WHERE @ngay BETWEEN k.ngay_bd AND k.ngay_kt", new { ngay = ngay.Date }, tx);

    public CtKmVoucherDTO? LayVoucherDangChay(string maVoucher, DateTime ngay, IDbTransaction? tx = null) =>
        DbHelper.QueryFirstOrDefault<CtKmVoucherDTO>(@"SELECT c.ma_km, c.ma_voucher, c.phan_tram_giam_gia
            FROM CT_KM_VOUCHER c JOIN KHUYEN_MAI k ON k.ma_km = c.ma_km
            WHERE c.ma_voucher = @maVoucher AND @ngay BETWEEN k.ngay_bd AND k.ngay_kt",
            new { maVoucher, ngay = ngay.Date }, tx);

    // v3: chuong trinh uu dai thanh vien moi dang chay (thuong chi co 1)
    public List<UuDaiThanhVienMoiDTO> LayThanhVienMoiDangChay(DateTime ngay, IDbTransaction? tx = null) =>
        DbHelper.Query<UuDaiThanhVienMoiDTO>(@"SELECT k.ma_km, k.ten_km, k.ngay_kt, c.so_don_dau, c.phan_tram_giam_gia, c.giam_toi_da
            FROM CT_KM_THANH_VIEN_MOI c JOIN KHUYEN_MAI k ON k.ma_km = c.ma_km
            WHERE @ngay BETWEEN k.ngay_bd AND k.ngay_kt
            ORDER BY c.phan_tram_giam_gia DESC", new { ngay = ngay.Date }, tx);

    public void Them(KhuyenMaiDTO k, IDbTransaction tx)
    {
        DbHelper.Execute(@"INSERT INTO KHUYEN_MAI (ma_km, ten_km, hinh_thuc, phan_tram_giam_gia, ngay_bd, ngay_kt)
            VALUES (@MaKm, @TenKm, @HinhThuc, @PhanTramGiamGia, @NgayBd, @NgayKt)", k, tx);
        GhiChiTiet(k, tx);
    }

    public void Sua(KhuyenMaiDTO k, IDbTransaction tx)
    {
        DbHelper.Execute(@"UPDATE KHUYEN_MAI SET ten_km = @TenKm, hinh_thuc = @HinhThuc,
                phan_tram_giam_gia = @PhanTramGiamGia, ngay_bd = @NgayBd, ngay_kt = @NgayKt WHERE ma_km = @MaKm", k, tx);
        DbHelper.Execute("DELETE FROM CT_KM_SAN_PHAM WHERE ma_km = @MaKm", k, tx);
        DbHelper.Execute("DELETE FROM CT_KM_HOA_DON WHERE ma_km = @MaKm", k, tx);
        DbHelper.Execute("DELETE FROM CT_KM_THANH_VIEN_MOI WHERE ma_km = @MaKm", k, tx);
        // voucher da duoc dung trong don thi khong xoa duoc (khoa ngoai) -> chi them voucher moi
        GhiChiTiet(k, tx, chiThemVoucherMoi: true);
    }

    // Ket thuc som: dat ngay ket thuc la hom qua
    // ngay_kt = hom qua. KM chua bat dau / bat dau hom nay thi doi ca ngay_bd (CHECK ngay_kt >= ngay_bd)
    public int KetThuc(string ma, DateTime homQua) =>
        DbHelper.Execute(@"UPDATE KHUYEN_MAI SET ngay_kt = @homQua,
                ngay_bd = CASE WHEN ngay_bd > @homQua THEN @homQua ELSE ngay_bd END
            WHERE ma_km = @ma AND ngay_kt > @homQua", new { ma, homQua = homQua.Date });

    public bool TrungVoucher(string maVoucher, string maKm) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM CT_KM_VOUCHER WHERE ma_voucher = @maVoucher AND ma_km <> @maKm",
            new { maVoucher, maKm }) > 0;

    private static void GhiChiTiet(KhuyenMaiDTO k, IDbTransaction tx, bool chiThemVoucherMoi = false)
    {
        foreach (var c in k.SanPhams)
            DbHelper.Execute("INSERT INTO CT_KM_SAN_PHAM (ma_km, ma_sp, phan_tram_giam_gia) VALUES (@ma, @MaSp, @PhanTramGiamGia)",
                new { ma = k.MaKm, c.MaSp, c.PhanTramGiamGia }, tx);
        foreach (var c in k.MucHoaDons)
            DbHelper.Execute(@"INSERT INTO CT_KM_HOA_DON (ma_km, muc_ap_dung_hoa_don, phan_tram_giam_gia)
                VALUES (@ma, @MucApDungHoaDon, @PhanTramGiamGia)", new { ma = k.MaKm, c.MucApDungHoaDon, c.PhanTramGiamGia }, tx);
        if (k.ThanhVienMoi is { } tv)
            DbHelper.Execute(@"INSERT INTO CT_KM_THANH_VIEN_MOI (ma_km, so_don_dau, phan_tram_giam_gia, giam_toi_da)
                VALUES (@ma, @SoDonDau, @PhanTramGiamGia, @GiamToiDa)", new { ma = k.MaKm, tv.SoDonDau, tv.PhanTramGiamGia, tv.GiamToiDa }, tx);
        foreach (var c in k.Vouchers)
        {
            var sql = chiThemVoucherMoi
                ? @"IF EXISTS (SELECT 1 FROM CT_KM_VOUCHER WHERE ma_voucher = @MaVoucher)
                        UPDATE CT_KM_VOUCHER SET phan_tram_giam_gia = @PhanTramGiamGia WHERE ma_voucher = @MaVoucher
                    ELSE INSERT INTO CT_KM_VOUCHER (ma_km, ma_voucher, phan_tram_giam_gia) VALUES (@ma, @MaVoucher, @PhanTramGiamGia)"
                : "INSERT INTO CT_KM_VOUCHER (ma_km, ma_voucher, phan_tram_giam_gia) VALUES (@ma, @MaVoucher, @PhanTramGiamGia)";
            DbHelper.Execute(sql, new { ma = k.MaKm, c.MaVoucher, c.PhanTramGiamGia }, tx);
        }
    }
}
