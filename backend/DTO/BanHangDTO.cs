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
    public List<ChiTietDonDatHangDTO> ChiTiets { get; set; } = new();
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
}
