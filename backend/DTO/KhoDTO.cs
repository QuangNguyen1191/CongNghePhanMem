namespace DTO;

// Bang PHIEU_NHAP + CHI_TIET_PHIEU_NHAP
public class PhieuNhapDTO
{
    public string MaPhieuNhap { get; set; } = "";
    public string MaNcc { get; set; } = "";
    public string MaNv { get; set; } = "";
    public DateTime NgayNhap { get; set; }
    public decimal TongTien { get; set; }
    public string? TenNcc { get; set; }
    public string? TenNhanVien { get; set; }
    public List<ChiTietPhieuNhapDTO> ChiTiets { get; set; } = new();
}

public class ChiTietPhieuNhapDTO
{
    public string MaPhieuNhap { get; set; } = "";
    public string MaSp { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGiaNhap { get; set; }
    public decimal ThanhTien { get; set; }
    public string? TenSp { get; set; }
    public string? KichCo { get; set; }
    public string? MauSac { get; set; }
}

// Bang DOI_TRA_HANG + CHI_TIET_DOI_TRA
public class DoiTraDTO
{
    public string MaDoiTra { get; set; } = "";
    public string MaHoaDon { get; set; } = "";
    public string? MaNv { get; set; }
    public string Loai { get; set; } = "TRA";       // DOI | TRA
    public string? LyDo { get; set; }
    public DateTime NgayDoiTra { get; set; }
    public string TrangThai { get; set; } = "CHO_DUYET";
    public decimal TienHoan { get; set; }
    public DateTime? NgayHoan { get; set; }
    public string? MaKh { get; set; }
    public string? TenKhachHang { get; set; }
    public string? TenNhanVien { get; set; }
    public List<ChiTietDoiTraDTO> ChiTiets { get; set; } = new();
}

public class ChiTietDoiTraDTO
{
    public string MaDoiTra { get; set; } = "";
    public string MaSp { get; set; } = "";
    public int SoLuong { get; set; }
    public string? MaSpDoi { get; set; }
    public string? TenSp { get; set; }
    public string? TenSpDoi { get; set; }
}
