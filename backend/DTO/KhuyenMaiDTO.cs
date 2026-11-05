namespace DTO;

// Bang KHUYEN_MAI
public class KhuyenMaiDTO
{
    public string MaKm { get; set; } = "";
    public string TenKm { get; set; } = "";
    public string HinhThuc { get; set; } = "";     // SAN_PHAM | HOA_DON | VOUCHER | THANH_VIEN_MOI (v3)
    public decimal? PhanTramGiamGia { get; set; }
    public DateTime NgayBd { get; set; }
    public DateTime NgayKt { get; set; }
    public List<CtKmSanPhamDTO> SanPhams { get; set; } = new();
    public List<CtKmHoaDonDTO> MucHoaDons { get; set; } = new();
    public List<CtKmVoucherDTO> Vouchers { get; set; } = new();
    public CtKmThanhVienMoiDTO? ThanhVienMoi { get; set; }     // v3
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

// v3: Bang CT_KM_THANH_VIEN_MOI - giam X% cho N don dau dat bang tai khoan
public class CtKmThanhVienMoiDTO
{
    public string MaKm { get; set; } = "";
    public int SoDonDau { get; set; }
    public decimal PhanTramGiamGia { get; set; }
    public decimal? GiamToiDa { get; set; }
}

// Uu dai thanh vien moi: thong tin chuong trinh + so luot con lai cua 1 khach (DaDung / ConLai = null khi chua dang nhap)
public class UuDaiThanhVienMoiDTO
{
    public string MaKm { get; set; } = "";
    public string TenKm { get; set; } = "";
    public int SoDonDau { get; set; }
    public decimal PhanTramGiamGia { get; set; }
    public decimal? GiamToiDa { get; set; }
    public DateTime NgayKt { get; set; }
    public int? DaDung { get; set; }
    public int? ConLai { get; set; }
}
