namespace DTO;

// Bang KHUYEN_MAI
public class KhuyenMaiDTO
{
    public string MaKm { get; set; } = "";
    public string TenKm { get; set; } = "";
    public string HinhThuc { get; set; } = "";     // SAN_PHAM | HOA_DON | VOUCHER
    public decimal? PhanTramGiamGia { get; set; }
    public DateTime NgayBd { get; set; }
    public DateTime NgayKt { get; set; }
    public List<CtKmSanPhamDTO> SanPhams { get; set; } = new();
    public List<CtKmHoaDonDTO> MucHoaDons { get; set; } = new();
    public List<CtKmVoucherDTO> Vouchers { get; set; } = new();
}

public class CtKmSanPhamDTO
{
    public string MaKm { get; set; } = "";
    public string MaSp { get; set; } = "";
    public decimal PhanTramGiamGia { get; set; }
}

public class CtKmHoaDonDTO
{
    public string MaKm { get; set; } = "";
    public decimal MucApDungHoaDon { get; set; }
    public decimal PhanTramGiamGia { get; set; }
}

public class CtKmVoucherDTO
{
    public string MaKm { get; set; } = "";
    public string MaVoucher { get; set; } = "";
    public decimal PhanTramGiamGia { get; set; }
}
