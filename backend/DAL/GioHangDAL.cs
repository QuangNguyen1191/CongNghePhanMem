using System.Data;
using DTO;

namespace DAL;

public class GioHangDAL
{
    public GioHangDTO? LayTheoKhach(string maKh, IDbTransaction? tx = null)
    {
        var gio = DbHelper.QueryFirstOrDefault<GioHangDTO>("SELECT * FROM GIO_HANG WHERE ma_kh = @maKh", new { maKh }, tx);
        if (gio == null) return null;
        gio.ChiTiets = DbHelper.Query<ChiTietGioHangDTO>(@"SELECT c.ma_gio_hang, c.ma_sp, c.so_luong, s.ten_sp, s.kich_co, s.mau_sac,
                s.hinh_anh, s.don_gia, s.so_luong AS ton_kho
            FROM CHI_TIET_GIO_HANG c JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp
            WHERE c.ma_gio_hang = @MaGioHang ORDER BY s.ten_sp", gio, tx);
        return gio;
    }

    public void TaoGio(string maGioHang, string maKh, IDbTransaction tx) =>
        DbHelper.Execute("INSERT INTO GIO_HANG (ma_gio_hang, ma_kh) VALUES (@maGioHang, @maKh)", new { maGioHang, maKh }, tx);

    public int SoLuongTrongGio(string maGioHang, string maSp) =>
        DbHelper.ExecuteScalar<int?>("SELECT so_luong FROM CHI_TIET_GIO_HANG WHERE ma_gio_hang = @maGioHang AND ma_sp = @maSp",
            new { maGioHang, maSp }) ?? 0;

    // Them moi hoac dat lai so luong cua 1 dong
    public void DatSoLuong(string maGioHang, string maSp, int soLuong) =>
        DbHelper.Execute(@"UPDATE CHI_TIET_GIO_HANG SET so_luong = @soLuong WHERE ma_gio_hang = @maGioHang AND ma_sp = @maSp;
            IF @@ROWCOUNT = 0
                INSERT INTO CHI_TIET_GIO_HANG (ma_gio_hang, ma_sp, so_luong) VALUES (@maGioHang, @maSp, @soLuong);
            UPDATE GIO_HANG SET ngay_cap_nhat = SYSDATETIME() WHERE ma_gio_hang = @maGioHang;",
            new { maGioHang, maSp, soLuong });

    public int XoaDong(string maGioHang, string maSp) =>
        DbHelper.Execute("DELETE FROM CHI_TIET_GIO_HANG WHERE ma_gio_hang = @maGioHang AND ma_sp = @maSp", new { maGioHang, maSp });

    public int XoaHet(string maGioHang, IDbTransaction? tx = null) =>
        DbHelper.Execute("DELETE FROM CHI_TIET_GIO_HANG WHERE ma_gio_hang = @maGioHang", new { maGioHang }, tx);
}
