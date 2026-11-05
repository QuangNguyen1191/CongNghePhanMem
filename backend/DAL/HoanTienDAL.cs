using System.Data;
using DTO;

namespace DAL;

// v3: bang HOAN_TIEN - don da tra truoc bi huy, cho ke toan / thu ngan hoan tien cho khach
public class HoanTienDAL
{
    private const string Chon = @"SELECT ht.*, k.ho_ten AS ten_khach_hang, d.sdt_nhan, n.ho_ten AS ten_nhan_vien
        FROM HOAN_TIEN ht
        JOIN DON_DAT_HANG d ON d.ma_don = ht.ma_don
        JOIN KHACH_HANG k ON k.ma_kh = d.ma_kh
        LEFT JOIN NHAN_VIEN n ON n.ma_nv = ht.ma_nv";

    public void Them(HoanTienDTO h, IDbTransaction tx) =>
        DbHelper.Execute(@"INSERT INTO HOAN_TIEN (ma_hoan_tien, ma_don, ma_hoa_don, so_tien, phuong_thuc, ghi_chu)
            VALUES (@MaHoanTien, @MaDon, @MaHoaDon, @SoTien, @PhuongThuc, @GhiChu)", h, tx);

    public List<HoanTienDTO> LayDanhSach(string? trangThai) =>
        DbHelper.Query<HoanTienDTO>($"{Chon} WHERE (@trangThai IS NULL OR ht.trang_thai = @trangThai) ORDER BY ht.ngay_tao DESC",
            new { trangThai });

    public HoanTienDTO? LayTheoMa(string ma) =>
        DbHelper.QueryFirstOrDefault<HoanTienDTO>($"{Chon} WHERE ht.ma_hoan_tien = @ma", new { ma });

    public bool DanhDauDaHoan(string ma, string maNv, string? ghiChu, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE HOAN_TIEN SET trang_thai = 'DA_HOAN', ngay_hoan = SYSDATETIME(), ma_nv = @maNv,
                ghi_chu = COALESCE(@ghiChu, ghi_chu)
            WHERE ma_hoan_tien = @ma AND trang_thai = 'CHO_HOAN'", new { ma, maNv, ghiChu }, tx) == 1;
}
