using System.Data;
using DTO;

namespace DAL;

public class LoaiSpDAL
{
    public List<LoaiSpDTO> LayTatCa() =>
        DbHelper.Query<LoaiSpDTO>("SELECT ma_loai_sp, ten_loai_sp FROM LOAI_SP ORDER BY ma_loai_sp");

    public LoaiSpDTO? LayTheoMa(string ma) =>
        DbHelper.QueryFirstOrDefault<LoaiSpDTO>("SELECT ma_loai_sp, ten_loai_sp FROM LOAI_SP WHERE ma_loai_sp = @ma", new { ma });

    public bool TrungTen(string ten, string? boQuaMa = null) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM LOAI_SP WHERE ten_loai_sp = @ten AND ma_loai_sp <> ISNULL(@boQuaMa, '')",
            new { ten, boQuaMa }) > 0;

    public int DemSanPham(string ma) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM SAN_PHAM WHERE ma_loai_sp = @ma", new { ma });

    public void Them(LoaiSpDTO l, IDbTransaction tx) =>
        DbHelper.Execute("INSERT INTO LOAI_SP (ma_loai_sp, ten_loai_sp) VALUES (@MaLoaiSp, @TenLoaiSp)", l, tx);

    public int Sua(LoaiSpDTO l) =>
        DbHelper.Execute("UPDATE LOAI_SP SET ten_loai_sp = @TenLoaiSp WHERE ma_loai_sp = @MaLoaiSp", l);

    public int Xoa(string ma) =>
        DbHelper.Execute("DELETE FROM LOAI_SP WHERE ma_loai_sp = @ma", new { ma });
}

public class SanPhamDAL
{
    private const string Cot = @"s.ma_sp, s.ten_sp, s.ma_loai_sp, s.chat_lieu, s.mo_ta, s.don_gia, s.so_luong,
        s.trang_thai, s.hinh_anh, s.kich_co, s.mau_sac, l.ten_loai_sp";

    // Tai 1 lan toan bo danh sach (che do tim kiem "tai su dung danh sach", yeu cau 2)
    public List<SanPhamDTO> LayTatCa(bool chiDangBan = false) =>
        DbHelper.Query<SanPhamDTO>($@"SELECT {Cot} FROM SAN_PHAM s JOIN LOAI_SP l ON l.ma_loai_sp = s.ma_loai_sp
            WHERE (@chiDangBan = 0 OR s.trang_thai = 'DANG_BAN')
            ORDER BY s.ten_sp, s.mau_sac, s.kich_co", new { chiDangBan });

    // Moi lan go tu khoa chay 1 cau truy van moi (che do "truy van moi", yeu cau 2)
    public List<SanPhamDTO> TimTheoTuKhoa(string tuKhoa, string? maLoaiSp, bool chiDangBan) =>
        DbHelper.Query<SanPhamDTO>($@"SELECT {Cot} FROM SAN_PHAM s JOIN LOAI_SP l ON l.ma_loai_sp = s.ma_loai_sp
            WHERE (s.ten_sp LIKE @mau OR s.ma_sp LIKE @mau OR s.mau_sac LIKE @mau)
              AND (@maLoaiSp IS NULL OR s.ma_loai_sp = @maLoaiSp)
              AND (@chiDangBan = 0 OR s.trang_thai = 'DANG_BAN')
            ORDER BY s.ten_sp, s.mau_sac, s.kich_co",
            new { mau = "%" + tuKhoa + "%", maLoaiSp, chiDangBan });

    public SanPhamDTO? LayTheoMa(string ma, IDbTransaction? tx = null) =>
        DbHelper.QueryFirstOrDefault<SanPhamDTO>(
            $"SELECT {Cot} FROM SAN_PHAM s JOIN LOAI_SP l ON l.ma_loai_sp = s.ma_loai_sp WHERE s.ma_sp = @ma", new { ma }, tx);

    // Cac size / mau cung mau san pham (web gom theo ten)
    public List<SanPhamDTO> LayCungTen(string tenSp) =>
        DbHelper.Query<SanPhamDTO>($@"SELECT {Cot} FROM SAN_PHAM s JOIN LOAI_SP l ON l.ma_loai_sp = s.ma_loai_sp
            WHERE s.ten_sp = @tenSp ORDER BY s.mau_sac, s.kich_co", new { tenSp });

    public List<SanPhamDTO> LayTonThap(int nguong) =>
        DbHelper.Query<SanPhamDTO>($@"SELECT {Cot} FROM SAN_PHAM s JOIN LOAI_SP l ON l.ma_loai_sp = s.ma_loai_sp
            WHERE s.trang_thai = 'DANG_BAN' AND s.so_luong < @nguong ORDER BY s.so_luong", new { nguong });

    // Tim kiem nang cao (yeu cau 6): WHERE do BLL ghep tu danh sach truong cho phep
    public List<SanPhamDTO> TimNangCao(string where, Dapper.DynamicParameters thamSo) =>
        DbHelper.Query<SanPhamDTO>($@"SELECT {Cot} FROM SAN_PHAM s JOIN LOAI_SP l ON l.ma_loai_sp = s.ma_loai_sp
            WHERE {where} ORDER BY s.ten_sp", thamSo);

    public void Them(SanPhamDTO s, IDbTransaction tx) =>
        DbHelper.Execute(@"INSERT INTO SAN_PHAM (ma_sp, ten_sp, ma_loai_sp, chat_lieu, mo_ta, don_gia, so_luong, trang_thai,
                hinh_anh, kich_co, mau_sac)
            VALUES (@MaSp, @TenSp, @MaLoaiSp, @ChatLieu, @MoTa, @DonGia, 0, @TrangThai, @HinhAnh, @KichCo, @MauSac)", s, tx);

    // Khong sua so_luong o day: ton kho chi doi qua nhap hang / ban hang / doi tra
    public int Sua(SanPhamDTO s) =>
        DbHelper.Execute(@"UPDATE SAN_PHAM SET ten_sp = @TenSp, ma_loai_sp = @MaLoaiSp, chat_lieu = @ChatLieu, mo_ta = @MoTa,
                don_gia = @DonGia, trang_thai = @TrangThai, hinh_anh = @HinhAnh, kich_co = @KichCo, mau_sac = @MauSac
            WHERE ma_sp = @MaSp", s);

    public int DoiTrangThai(string ma, string trangThai) =>
        DbHelper.Execute("UPDATE SAN_PHAM SET trang_thai = @trangThai WHERE ma_sp = @ma", new { ma, trangThai });

    // Cong / tru ton kho. Tra ve false neu tru qua so ton (khong cho am).
    public bool CapNhatTon(string ma, int thayDoi, IDbTransaction tx) =>
        DbHelper.Execute(@"UPDATE SAN_PHAM SET so_luong = so_luong + @thayDoi
            WHERE ma_sp = @ma AND so_luong + @thayDoi >= 0", new { ma, thayDoi }, tx) == 1;
}

public class NhaCungCapDAL
{
    public List<NhaCungCapDTO> LayTatCa() =>
        DbHelper.Query<NhaCungCapDTO>("SELECT * FROM NHA_CUNG_CAP ORDER BY ma_ncc");

    public NhaCungCapDTO? LayTheoMa(string ma, IDbTransaction? tx = null) =>
        DbHelper.QueryFirstOrDefault<NhaCungCapDTO>("SELECT * FROM NHA_CUNG_CAP WHERE ma_ncc = @ma", new { ma }, tx);

    public bool CoPhieuNhap(string ma) =>
        DbHelper.ExecuteScalar<int>("SELECT COUNT(*) FROM PHIEU_NHAP WHERE ma_ncc = @ma", new { ma }) > 0;

    public void Them(NhaCungCapDTO n, IDbTransaction tx) =>
        DbHelper.Execute(@"INSERT INTO NHA_CUNG_CAP (ma_ncc, ten_ncc, dia_chi, sdt, email, ma_so_thue, trang_thai)
            VALUES (@MaNcc, @TenNcc, @DiaChi, @Sdt, @Email, @MaSoThue, @TrangThai)", n, tx);

    public int Sua(NhaCungCapDTO n) =>
        DbHelper.Execute(@"UPDATE NHA_CUNG_CAP SET ten_ncc = @TenNcc, dia_chi = @DiaChi, sdt = @Sdt, email = @Email,
                ma_so_thue = @MaSoThue, trang_thai = @TrangThai WHERE ma_ncc = @MaNcc", n);

    public int Xoa(string ma) => DbHelper.Execute("DELETE FROM NHA_CUNG_CAP WHERE ma_ncc = @ma", new { ma });
}
