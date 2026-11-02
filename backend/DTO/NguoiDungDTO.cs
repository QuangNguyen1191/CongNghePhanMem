namespace DTO;

// Bang TAI_KHOAN
public class TaiKhoanDTO
{
    public string MaTk { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public string MatKhau { get; set; } = "";      // chuoi hash, khong bao gio tra ra API
    public string VaiTro { get; set; } = "";       // ADMIN | NHAN_VIEN | KHACH_HANG
    public string TrangThai { get; set; } = "HOAT_DONG";
    public DateTime NgayTao { get; set; }
    // join NHAN_VIEN / KHACH_HANG de man hinh quan ly tai khoan hien ma NV / KH
    public string? MaNv { get; set; }
    public string? MaKh { get; set; }
    public string? HoTen { get; set; }
}

// Bang NHAN_VIEN
public class NhanVienDTO
{
    public string MaNv { get; set; } = "";
    public string MaTk { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string? ChucVu { get; set; }
    public DateTime? NgayVaoLam { get; set; }
    public decimal? Luong { get; set; }
    public string? Sdt { get; set; }
    public string Email { get; set; } = "";
    public string TrangThai { get; set; } = "DANG_LAM";
    public string? TenDangNhap { get; set; }
    public string? VaiTro { get; set; }
}

// Bang KHACH_HANG
public class KhachHangDTO
{
    public string MaKh { get; set; } = "";
    public string? MaTk { get; set; }              // NULL: khach mua tai quay
    public string HoTen { get; set; } = "";
    public string? Sdt { get; set; }
    public string? Email { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string HangThanhVien { get; set; } = "DONG";
    public int DiemTichLuy { get; set; }
    public string? DiaChi { get; set; }
    public string? TinhThanh { get; set; }
}

// Thong tin dang nhap tra ve cho giao dien
public class PhienDangNhapDTO
{
    public string MaTk { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public string? MaNv { get; set; }
    public string? MaKh { get; set; }
    public string HoTen { get; set; } = "";
    public string? ChucVu { get; set; }
    public string? Token { get; set; }             // WebApi gan JWT vao day
}

// Noi dang nhap: moi vai tro chi vao dung phan mem cua minh
public enum NoiDangNhap { WebBanHang, WebStaff, AppAdmin }
