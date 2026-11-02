using System.Globalization;
using Dapper;
using DAL;
using DTO;

namespace BLL;

// Yeu cau 2 + 6: tim kiem nang cao theo nhieu dieu kien (=, <>, >, <, >=, <=, LIKE, IN), ghep AND (giao) hoac OR (hop).
// Moi cach tim deu co 2 phien ban:
//   Tim...(dk)        : truy van moi, BLL ghep menh de WHERE (chi tu danh sach truong cho phep, gia tri luon la tham so)
//   Loc...(ds, dk)    : tai su dung danh sach dang hien tren man hinh, loc trong bo nho, khong chay them cau SQL nao
public class TimKiemBLL
{
    private enum Kieu { Chuoi, So, Ngay }

    private sealed record Truong<T>(string Ten, string Sql, Kieu Kieu, Func<T, object?> Lay);

    private static readonly string[] ToanTuChoPhep = { "=", "<>", ">", "<", ">=", "<=", "LIKE", "IN" };

    // ------------------------------------------------------------------ danh sach truong duoc tim
    private static readonly List<Truong<HoaDonDTO>> TruongHoaDon = new()
    {
        new("ma_hoa_don", "h.ma_hoa_don", Kieu.Chuoi, x => x.MaHoaDon),
        new("ngay_lap", "h.ngay_lap", Kieu.Ngay, x => x.NgayLap),
        new("tong_tien", "h.tong_tien", Kieu.So, x => x.TongTien),
        new("tien_giam", "h.tien_giam", Kieu.So, x => x.TienGiam),
        new("trang_thai", "h.trang_thai", Kieu.Chuoi, x => x.TrangThai),
        new("phuong_thuc_tt", "h.phuong_thuc_tt", Kieu.Chuoi, x => x.PhuongThucTt),
        new("ma_nv", "h.ma_nv", Kieu.Chuoi, x => x.MaNv),
        new("ten_nhan_vien", "n.ho_ten", Kieu.Chuoi, x => x.TenNhanVien),
        new("ma_kh", "h.ma_kh", Kieu.Chuoi, x => x.MaKh),
        new("ten_khach_hang", "k.ho_ten", Kieu.Chuoi, x => x.TenKhachHang),
        new("tinh_thanh", "k.tinh_thanh", Kieu.Chuoi, x => x.TinhThanhKhach),
    };

    private static readonly List<Truong<KhachHangDTO>> TruongKhachHang = new()
    {
        new("ma_kh", "ma_kh", Kieu.Chuoi, x => x.MaKh),
        new("ho_ten", "ho_ten", Kieu.Chuoi, x => x.HoTen),
        new("sdt", "sdt", Kieu.Chuoi, x => x.Sdt),
        new("email", "email", Kieu.Chuoi, x => x.Email),
        new("ngay_sinh", "ngay_sinh", Kieu.Ngay, x => x.NgaySinh),
        new("hang_thanh_vien", "hang_thanh_vien", Kieu.Chuoi, x => x.HangThanhVien),
        new("diem_tich_luy", "diem_tich_luy", Kieu.So, x => x.DiemTichLuy),
        new("dia_chi", "dia_chi", Kieu.Chuoi, x => x.DiaChi),
        new("tinh_thanh", "tinh_thanh", Kieu.Chuoi, x => x.TinhThanh),
    };

    private static readonly List<Truong<SanPhamDTO>> TruongSanPham = new()
    {
        new("ma_sp", "s.ma_sp", Kieu.Chuoi, x => x.MaSp),
        new("ten_sp", "s.ten_sp", Kieu.Chuoi, x => x.TenSp),
        new("ma_loai_sp", "s.ma_loai_sp", Kieu.Chuoi, x => x.MaLoaiSp),
        new("ten_loai_sp", "l.ten_loai_sp", Kieu.Chuoi, x => x.TenLoaiSp),
        new("chat_lieu", "s.chat_lieu", Kieu.Chuoi, x => x.ChatLieu),
        new("don_gia", "s.don_gia", Kieu.So, x => x.DonGia),
        new("so_luong", "s.so_luong", Kieu.So, x => x.SoLuong),
        new("trang_thai", "s.trang_thai", Kieu.Chuoi, x => x.TrangThai),
        new("kich_co", "s.kich_co", Kieu.Chuoi, x => x.KichCo),
        new("mau_sac", "s.mau_sac", Kieu.Chuoi, x => x.MauSac),
    };

    public static IReadOnlyList<string> CacTruongHoaDon => TruongHoaDon.Select(t => t.Ten).ToList();
    public static IReadOnlyList<string> CacTruongKhachHang => TruongKhachHang.Select(t => t.Ten).ToList();
    public static IReadOnlyList<string> CacTruongSanPham => TruongSanPham.Select(t => t.Ten).ToList();
    public static IReadOnlyList<string> CacToanTu => ToanTuChoPhep;

    // ------------------------------------------------------------------ truy van moi
    public List<HoaDonDTO> TimHoaDon(TimKiemNangCaoDTO dk)
    {
        var (where, ts) = GhepWhere(dk, TruongHoaDon);
        return new HoaDonDAL().TimNangCao(where, ts);
    }

    public List<KhachHangDTO> TimKhachHang(TimKiemNangCaoDTO dk)
    {
        var (where, ts) = GhepWhere(dk, TruongKhachHang);
        return new KhachHangDAL().TimNangCao(where, ts);
    }

    public List<SanPhamDTO> TimSanPham(TimKiemNangCaoDTO dk)
    {
        var (where, ts) = GhepWhere(dk, TruongSanPham);
        return new SanPhamDAL().TimNangCao(where, ts);
    }

    // ------------------------------------------------------------------ tai su dung danh sach co san
    public List<HoaDonDTO> LocHoaDon(IEnumerable<HoaDonDTO> ds, TimKiemNangCaoDTO dk) => Loc(ds, dk, TruongHoaDon);
    public List<KhachHangDTO> LocKhachHang(IEnumerable<KhachHangDTO> ds, TimKiemNangCaoDTO dk) => Loc(ds, dk, TruongKhachHang);
    public List<SanPhamDTO> LocSanPham(IEnumerable<SanPhamDTO> ds, TimKiemNangCaoDTO dk) => Loc(ds, dk, TruongSanPham);

    // ------------------------------------------------------------------ cai dat
    private static (Truong<T> Truong, string ToanTu, List<object> GiaTris) KiemTraDieuKien<T>(DieuKienTimDTO d,
        List<Truong<T>> cacTruong)
    {
        var truong = cacTruong.FirstOrDefault(t => t.Ten.Equals(d.Truong?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new LoiNghiepVu($"Không tìm được theo trường '{d.Truong}'.");
        var toanTu = (d.ToanTu ?? "").Trim().ToUpperInvariant();
        QuyTac.KiemTra(ToanTuChoPhep.Contains(toanTu), $"Toán tử '{d.ToanTu}' không hợp lệ.");
        QuyTac.KiemTra(toanTu != "LIKE" || truong.Kieu == Kieu.Chuoi, $"Trường '{truong.Ten}' không dùng được LIKE.");
        var chuoi = toanTu == "IN"
            ? (d.GiaTri ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : new[] { (d.GiaTri ?? "").Trim() };
        QuyTac.KiemTra(chuoi.Length > 0 && chuoi.All(c => c != ""), $"Chưa nhập giá trị cho trường '{truong.Ten}'.");
        return (truong, toanTu, chuoi.Select(c => DocGiaTri(truong, c)).ToList());
    }

    private static object DocGiaTri<T>(Truong<T> t, string s) => t.Kieu switch
    {
        Kieu.So => decimal.TryParse(s.Replace(".", "").Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture,
                out var so) || decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out so)
            ? so : throw new LoiNghiepVu($"Giá trị '{s}' của trường '{t.Ten}' phải là số."),
        Kieu.Ngay => DateTime.TryParseExact(s, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var ngay)
            ? ngay : throw new LoiNghiepVu($"Giá trị '{s}' của trường '{t.Ten}' phải là ngày (dd/MM/yyyy)."),
        _ => s
    };

    private static (string, DynamicParameters) GhepWhere<T>(TimKiemNangCaoDTO dk, List<Truong<T>> cacTruong)
    {
        QuyTac.KiemTra(dk.DieuKiens.Count > 0, "Cần ít nhất 1 điều kiện tìm kiếm.");
        QuyTac.KiemTra(dk.DieuKiens.Count <= 10, "Tối đa 10 điều kiện.");
        var ts = new DynamicParameters();
        var phan = new List<string>();
        var i = 0;
        foreach (var d in dk.DieuKiens)
        {
            var (truong, toanTu, giaTris) = KiemTraDieuKien(d, cacTruong);
            if (toanTu == "IN")
            {
                var ten = giaTris.Select(g => { var p = $"p{i++}"; ts.Add(p, g); return "@" + p; }).ToList();
                phan.Add($"{truong.Sql} IN ({string.Join(", ", ten)})");
            }
            else
            {
                var p = $"p{i++}";
                ts.Add(p, toanTu == "LIKE" ? $"%{giaTris[0]}%" : giaTris[0]);
                phan.Add($"{truong.Sql} {toanTu} @{p}");
            }
        }
        return ("(" + string.Join(dk.KhopTatCa ? " AND " : " OR ", phan) + ")", ts);
    }

    private static List<T> Loc<T>(IEnumerable<T> ds, TimKiemNangCaoDTO dk, List<Truong<T>> cacTruong)
    {
        QuyTac.KiemTra(dk.DieuKiens.Count > 0, "Cần ít nhất 1 điều kiện tìm kiếm.");
        var dks = dk.DieuKiens.Select(d => KiemTraDieuKien(d, cacTruong)).ToList();
        bool Khop(T x, (Truong<T> Truong, string ToanTu, List<object> GiaTris) d)
        {
            var v = d.Truong.Lay(x);
            if (v == null) return false;   // giong SQL: so sanh voi NULL luon sai
            if (d.ToanTu == "LIKE")
                return v.ToString()!.Contains((string)d.GiaTris[0], StringComparison.CurrentCultureIgnoreCase);
            if (d.ToanTu == "IN") return d.GiaTris.Any(g => SoSanh(v, g) == 0);
            var ss = SoSanh(v, d.GiaTris[0]);
            return d.ToanTu switch
            {
                "=" => ss == 0, "<>" => ss != 0, ">" => ss > 0, "<" => ss < 0, ">=" => ss >= 0, _ => ss <= 0
            };
        }
        return ds.Where(x => dk.KhopTatCa ? dks.All(d => Khop(x, d)) : dks.Any(d => Khop(x, d))).ToList();
    }

    private static int SoSanh(object v, object g) => (v, g) switch
    {
        (DateTime a, DateTime b) => a.Date.CompareTo(b.Date),
        (string a, string b) => string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase),
        _ => Convert.ToDecimal(v).CompareTo(Convert.ToDecimal(g))
    };
}
