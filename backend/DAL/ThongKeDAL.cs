using DTO;

namespace DAL;

// Thong ke cach "truy van moi": de SQL Server gom nhom (GROUP BY), chi tra ve ket qua da tong hop
public class ThongKeDAL
{
    public class DongGom
    {
        public int Nam { get; set; }
        public int Ky { get; set; }            // thang hoac quy, 0 neu gom theo nam
        public string Ma { get; set; } = "";   // ma NV / KH / SP / loai SP
        public string Ten { get; set; } = "";
        public decimal Tien { get; set; }
    }

    // 5a. Tong tien hoa don tu ngay A den ngay B
    public (int SoHoaDon, decimal TongTien) TongTienKhoang(DateTime tuNgay, DateTime denNgay)
    {
        var kq = DbHelper.QueryFirstOrDefault<(int, decimal?)>(@"SELECT COUNT(*), SUM(tong_tien) FROM HOA_DON
            WHERE trang_thai = 'DA_THANH_TOAN' AND ngay_lap BETWEEN @tuNgay AND @denNgay", new { tuNgay, denNgay });
        return (kq.Item1, kq.Item2 ?? 0);
    }

    // 5b. Thu theo ky. kieu: THANG | QUY | NAM. nam = null: tat ca cac nam
    public List<DongGom> ThuTheoKy(string kieu, int? nam) =>
        DbHelper.Query<DongGom>($@"SELECT nam, ky, SUM(tong_tien) AS tien FROM (
                SELECT YEAR(ngay_lap) AS nam, {BieuThucKy(kieu, "ngay_lap")} AS ky, tong_tien
                FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN' AND (@nam IS NULL OR YEAR(ngay_lap) = @nam)) x
            GROUP BY nam, ky", new { nam });

    // 5b. Chi theo ky = tien nhap hang + tien hoan doi tra
    public List<DongGom> ChiTheoKy(string kieu, int? nam) =>
        DbHelper.Query<DongGom>($@"SELECT nam, ky, SUM(tien) AS tien FROM (
                SELECT YEAR(ngay_nhap) AS nam, {BieuThucKy(kieu, "ngay_nhap")} AS ky, tong_tien AS tien FROM PHIEU_NHAP
                UNION ALL
                SELECT YEAR(ngay_hoan), {BieuThucKy(kieu, "ngay_hoan")}, tien_hoan FROM DOI_TRA_HANG
                WHERE trang_thai = 'HOAN_TAT' AND tien_hoan > 0) x
            WHERE @nam IS NULL OR nam = @nam
            GROUP BY nam, ky", new { nam });

    // 5c. Theo nhan vien va nam
    public List<DongGom> TheoNhanVienNam() =>
        DbHelper.Query<DongGom>(@"SELECT YEAR(h.ngay_lap) AS nam, 0 AS ky, h.ma_nv AS ma, n.ho_ten AS ten, SUM(h.tong_tien) AS tien
            FROM HOA_DON h JOIN NHAN_VIEN n ON n.ma_nv = h.ma_nv WHERE h.trang_thai = 'DA_THANH_TOAN'
            GROUP BY YEAR(h.ngay_lap), h.ma_nv, n.ho_ten");

    // 5d. Theo khach hang va quy (nam = null: gom theo nam)
    public List<DongGom> TheoKhachHang(int? nam) =>
        DbHelper.Query<DongGom>(@"SELECT YEAR(h.ngay_lap) AS nam,
                CASE WHEN @nam IS NULL THEN 0 ELSE DATEPART(QUARTER, h.ngay_lap) END AS ky,
                ISNULL(h.ma_kh, '') AS ma, ISNULL(k.ho_ten, N'Khách vãng lai') AS ten, SUM(h.tong_tien) AS tien
            FROM HOA_DON h LEFT JOIN KHACH_HANG k ON k.ma_kh = h.ma_kh
            WHERE h.trang_thai = 'DA_THANH_TOAN' AND (@nam IS NULL OR YEAR(h.ngay_lap) = @nam)
            GROUP BY YEAR(h.ngay_lap), CASE WHEN @nam IS NULL THEN 0 ELSE DATEPART(QUARTER, h.ngay_lap) END,
                ISNULL(h.ma_kh, ''), ISNULL(k.ho_ten, N'Khách vãng lai')", new { nam });

    // 5e. Theo san pham va quy (tien hang truoc giam gia hoa don)
    public List<DongGom> TheoSanPhamQuy(int nam) =>
        DbHelper.Query<DongGom>(@"SELECT YEAR(h.ngay_lap) AS nam, DATEPART(QUARTER, h.ngay_lap) AS ky, c.ma_sp AS ma,
                s.ten_sp + ISNULL(' - ' + s.mau_sac, '') + ISNULL(' - ' + s.kich_co, '') AS ten, SUM(c.thanh_tien) AS tien
            FROM CHI_TIET_HOA_DON c JOIN HOA_DON h ON h.ma_hoa_don = c.ma_hoa_don JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp
            WHERE h.trang_thai = 'DA_THANH_TOAN' AND YEAR(h.ngay_lap) = @nam
            GROUP BY YEAR(h.ngay_lap), DATEPART(QUARTER, h.ngay_lap), c.ma_sp, s.ten_sp, s.mau_sac, s.kich_co", new { nam });

    public List<int> CacNamCoDuLieu() =>
        DbHelper.Query<int>(@"SELECT DISTINCT YEAR(ngay_lap) FROM HOA_DON
            UNION SELECT DISTINCT YEAR(ngay_nhap) FROM PHIEU_NHAP ORDER BY 1");

    // Du lieu tho cho cach "tai su dung" phan chi
    public List<(DateTime Ngay, decimal Tien)> LayKhoanChi() =>
        DbHelper.Query<(DateTime, decimal)>(@"SELECT ngay_nhap, tong_tien FROM PHIEU_NHAP
            UNION ALL SELECT ngay_hoan, tien_hoan FROM DOI_TRA_HANG WHERE trang_thai = 'HOAN_TAT' AND tien_hoan > 0");

    private static string BieuThucKy(string kieu, string cot) => kieu switch
    {
        "THANG" => $"MONTH({cot})",
        "QUY" => $"DATEPART(QUARTER, {cot})",
        "NAM" => "0",
        _ => throw new ArgumentException("Kieu ky phai la THANG, QUY hoac NAM")
    };
}
