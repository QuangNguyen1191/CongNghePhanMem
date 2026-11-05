namespace DTO;

// ===== v3: khach vang lai mua hang khong can dang ky, xac thuc so dien thoai bang OTP =====

// Bang MA_OTP
public class MaOtpDTO
{
    public long MaOtp { get; set; }
    public string Sdt { get; set; } = "";
    public string MucDich { get; set; } = "MUA_HANG";   // MUA_HANG | TRA_CUU | DANG_KY
    public string MaBam { get; set; } = "";
    public DateTime NgayTao { get; set; }
    public DateTime HanDung { get; set; }
    public int SoLanSai { get; set; }
    public bool DaDung { get; set; }
    public string? DiaChiIp { get; set; }
}

public class GuiOtpDTO
{
    public string Sdt { get; set; } = "";
    public string MucDich { get; set; } = "MUA_HANG";
}

public class KetQuaGuiOtpDTO
{
    public string Sdt { get; set; } = "";
    public int HetHanSauGiay { get; set; }
    public int GuiLaiSauGiay { get; set; }
    public string? MaDemo { get; set; }       // chi co khi bat che do demo (OTP gia lap, khong co tong dai SMS)
}

public class XacThucOtpDTO
{
    public string Sdt { get; set; } = "";
    public string Ma { get; set; } = "";
}

// Ket qua xac thuc OTP: phien khach vang lai (JWT ngan han, vai tro VANG_LAI) + thong tin da biet cua SDT de dien san
public class PhienVangLaiDTO
{
    public string Sdt { get; set; } = "";
    public string? Token { get; set; }        // WebApi gan
    public DateTime HetHan { get; set; }
    public bool CoTaiKhoan { get; set; }      // SDT da gan voi 1 tai khoan -> goi y dang nhap
    public string? HoTen { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public int SoDonCu { get; set; }
}

// Dat hang khong can tai khoan: gio hang nam o trinh duyet nen gui kem danh sach dong hang
public class DatHangVangLaiDTO
{
    public string HoTen { get; set; } = "";
    public string? Email { get; set; }
    public string DiaChiGiao { get; set; } = "";
    public string PhuongThucTt { get; set; } = "COD";
    public string? MaVoucher { get; set; }
    public List<DongHangDTO> DongHangs { get; set; } = new();
}

// Cho web staff: lich su cua 1 SDT de danh gia don vang lai truoc khi xac nhan
public class RuiRoKhachDTO
{
    public string Sdt { get; set; } = "";
    public string? MaKh { get; set; }
    public string? HoTen { get; set; }
    public bool CoTaiKhoan { get; set; }
    public bool SdtDaXacThuc { get; set; }
    public int SoDonHoanTat { get; set; }
    public int SoDonDangCho { get; set; }      // CHO_XAC_NHAN
    public int SoDonBiTuChoi { get; set; }     // TU_CHOI
    public int SoLanBomHang { get; set; }      // da xac nhan (co hoa don) roi bi huy: khach khong nhan hang
    public bool DuocDatCod { get; set; }
    public string MucRuiRo { get; set; } = "THAP";   // THAP | TRUNG_BINH | CAO
    public string GhiChu { get; set; } = "";
}
