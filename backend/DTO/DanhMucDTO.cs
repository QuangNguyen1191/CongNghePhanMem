namespace DTO;

// Bang LOAI_SP
public class LoaiSpDTO
{
    public string MaLoaiSp { get; set; } = "";
    public string TenLoaiSp { get; set; } = "";
}

// Bang SAN_PHAM (moi size - mau la 1 dong / 1 SKU)
public class SanPhamDTO
{
    public string MaSp { get; set; } = "";
    public string TenSp { get; set; } = "";
    public string MaLoaiSp { get; set; } = "";
    public string? ChatLieu { get; set; }
    public string? MoTa { get; set; }
    public decimal DonGia { get; set; }
    public int SoLuong { get; set; }               // ton kho hien tai
    public string TrangThai { get; set; } = "DANG_BAN";
    public string? HinhAnh { get; set; }
    public string? KichCo { get; set; }
    public string? MauSac { get; set; }
    public string? TenLoaiSp { get; set; }         // join LOAI_SP, chi de hien thi
}

// Bang NHA_CUNG_CAP
public class NhaCungCapDTO
{
    public string MaNcc { get; set; } = "";
    public string TenNcc { get; set; } = "";
    public string? DiaChi { get; set; }
    public string? Sdt { get; set; }
    public string? Email { get; set; }
    public string? MaSoThue { get; set; }
    public string TrangThai { get; set; } = "HOP_TAC";
}
