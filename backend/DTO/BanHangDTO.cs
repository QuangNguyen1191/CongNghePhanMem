namespace DTO;

// Bang GIO_HANG + CHI_TIET_GIO_HANG
public class GioHangDTO
{
    public string MaGioHang { get; set; } = "";
    public string MaKh { get; set; } = "";
    public DateTime NgayCapNhat { get; set; }
    public List<ChiTietGioHangDTO> ChiTiets { get; set; } = new();
    public decimal TamTinh => ChiTiets.Sum(c => c.ThanhTien);
}

public class ChiTietGioHangDTO
{
    public string MaGioHang { get; set; } = "";
    public string MaSp { get; set; } = "";
    public int SoLuong { get; set; }
    // join SAN_PHAM, gia da tinh khuyen mai san pham neu co
    public string? TenSp { get; set; }
    public string? KichCo { get; set; }
    public string? MauSac { get; set; }
    public string? HinhAnh { get; set; }
    public decimal DonGia { get; set; }
    public int TonKho { get; set; }
    public decimal ThanhTien => DonGia * SoLuong;
}

// Bang DON_DAT_HANG + CHI_TIET_DON_DAT_HANG
public class DonDatHangDTO
{
    public string MaDon { get; set; } = "";
    public string MaKh { get; set; } = "";
    public string? MaNv { get; set; }
    public DateTime NgayDat { get; set; }
    public string TrangThai { get; set; } = "CHO_XAC_NHAN";
    public string DiaChiGiao { get; set; } = "";
    public string SdtNhan { get; set; } = "";
    public string PhuongThucTt { get; set; } = "COD";
    public string? MaVoucher { get; set; }
    public decimal TongTien { get; set; }
    public string? TenNguoiNhan { get; set; }
    public decimal TienGiam { get; set; }
    public string? TenKhachHang { get; set; }
    public string? TenNhanVien { get; set; }
    public string? MaHoaDon { get; set; }
    public string Nguon { get; set; } = "TAI_KHOAN";   // v3: TAI_KHOAN | VANG_LAI
    public string? EmailNhan { get; set; }             // v3
    public string? MaKm { get; set; }                  // v3: khuyen mai da ap dung luc dat
    // v3: huy don / yeu cau huy (khach xin huy don da xac nhan, nhan vien duyet)
    public string? YeuCauHuy { get; set; }             // null | CHO_DUYET | DA_DUYET | TU_CHOI
    public string? LyDoHuy { get; set; }
    public DateTime? NgayYeuCauHuy { get; set; }
    public string? PhanHoiHuy { get; set; }            // ly do cua hang tu choi yeu cau huy
    public DateTime? NgayHuy { get; set; }
    public string? NguoiHuy { get; set; }              // KHACH | NHAN_VIEN
    public string? TrangThaiHoanTien { get; set; }     // CHO_HOAN | DA_HOAN (don da tra truoc bi huy)
    public decimal? SoTienHoan { get; set; }
    public List<ChiTietDonDatHangDTO> ChiTiets { get; set; } = new();
}

// Body chung cho cac thao tac can ly do (yeu cau huy, tu choi yeu cau huy, nhan vien huy, da hoan tien)
public class LyDoDTO
{
    public string? LyDo { get; set; }
}

// v3: bang HOAN_TIEN - don da thanh toan truoc (chuyen khoan / online) bi huy thi phai hoan tien
public class HoanTienDTO
{
    public string MaHoanTien { get; set; } = "";
    public string MaDon { get; set; } = "";
    public string MaHoaDon { get; set; } = "";
    public decimal SoTien { get; set; }
    public string PhuongThuc { get; set; } = "";
    public string TrangThai { get; set; } = "CHO_HOAN";   // CHO_HOAN | DA_HOAN
    public DateTime NgayTao { get; set; }
    public DateTime? NgayHoan { get; set; }
    public string? MaNv { get; set; }
    public string? GhiChu { get; set; }
    public string? TenKhachHang { get; set; }
    public string? SdtNhan { get; set; }
    public string? TenNhanVien { get; set; }
}

public class ChiTietDonDatHangDTO
{
    public string MaDon { get; set; } = "";
    public string MaSp { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public string? TenSp { get; set; }
    public string? KichCo { get; set; }
    public string? MauSac { get; set; }
}

// Bang HOA_DON + CHI_TIET_HOA_DON
public class HoaDonDTO
{
    public string MaHoaDon { get; set; } = "";
    public string? MaDon { get; set; }
    public string? MaKh { get; set; }
    public string MaNv { get; set; } = "";
    public DateTime NgayLap { get; set; }
    public string TrangThai { get; set; } = "CHO_THANH_TOAN";
    public string? MaKm { get; set; }
    public decimal TienGiam { get; set; }
    public string PhuongThucTt { get; set; } = "TIEN_MAT";
    public decimal TongTien { get; set; }
    public string? TenKhachHang { get; set; }
    public string? TenNhanVien { get; set; }
    public string? TenKm { get; set; }
    public string? TinhThanhKhach { get; set; }
    public List<ChiTietHoaDonDTO> ChiTiets { get; set; } = new();
}

public class ChiTietHoaDonDTO
{
    public string MaHoaDon { get; set; } = "";
    public string MaSp { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGiaBan { get; set; }
    public decimal ThanhTien { get; set; }
    public string? TenSp { get; set; }
    public string? KichCo { get; set; }
    public string? MauSac { get; set; }
}

// Yeu cau tao don / ban tai quay: chi gui ma SP + so luong, gia do BLL tinh
public class DongHangDTO
{
    public string MaSp { get; set; } = "";
    public int SoLuong { get; set; }
}

public class DatHangDTO
{
    public string DiaChiGiao { get; set; } = "";
    public string SdtNhan { get; set; } = "";
    public string? TenNguoiNhan { get; set; }
    public string PhuongThucTt { get; set; } = "COD";
    public string? MaVoucher { get; set; }
}

public class BanTaiQuayDTO
{
    public string? MaKh { get; set; }               // NULL: khach vang lai
    public string PhuongThucTt { get; set; } = "TIEN_MAT";
    public string? MaVoucher { get; set; }
    public List<DongHangDTO> DongHangs { get; set; } = new();
}

// Ket qua tinh tien (dung chung cho gio hang, dat hang, ban tai quay)
public class BangTinhTienDTO
{
    public List<ChiTietHoaDonDTO> Dongs { get; set; } = new();
    public decimal TamTinh { get; set; }
    public decimal TienGiam { get; set; }
    public decimal TongTien { get; set; }
    public string? MaKm { get; set; }
    public string? GhiChuKm { get; set; }
    // v3: muc giam tren ca don dang ap dung: VOUCHER | HOA_DON | THANH_VIEN_MOI | null (khong giam)
    public string? LoaiGiam { get; set; }
    public string? MaVoucher { get; set; }   // v3: voucher thuc su duoc ap dung (null neu uu dai khac co loi hon)
}
