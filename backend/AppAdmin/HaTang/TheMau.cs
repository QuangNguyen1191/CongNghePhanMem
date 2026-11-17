using DTO;
using D = AppAdmin.HaTang.TheXemNhanh.Dong;
using ND = AppAdmin.HaTang.TheXemNhanh.NoiDung;

namespace AppAdmin.HaTang;

// Mau the xem nhanh (re chuot) cho tung loai du lieu. Luoi<T> tu gan mau theo kieu T,
// nen moi bang hien don hang / hoa don / san pham ... trong app deu co the, ke ca bang phu
// (hoa don cua khach, phieu nhap cua NCC, ket qua tim kiem, tong quan). Man hinh muon the khac thi gan lai Luoi.XemNhanh.
// Chi dung du lieu da co tren dong: re chuot khong chay cau SQL nao.
public static class TheMau
{
    private static readonly Dictionary<Type, Delegate> Mau = new()
    {
        [typeof(DonDatHangDTO)] = (Func<DonDatHangDTO, ND?>)DonHang,
        [typeof(HoaDonDTO)] = (Func<HoaDonDTO, ND?>)HoaDon,
        [typeof(DoiTraDTO)] = (Func<DoiTraDTO, ND?>)DoiTra,
        [typeof(HoanTienDTO)] = (Func<HoanTienDTO, ND?>)HoanTien,
        [typeof(SanPhamDTO)] = (Func<SanPhamDTO, ND?>)SanPham,
        [typeof(NhaCungCapDTO)] = (Func<NhaCungCapDTO, ND?>)NhaCungCap,
        [typeof(PhieuNhapDTO)] = (Func<PhieuNhapDTO, ND?>)PhieuNhap,
        [typeof(KhuyenMaiDTO)] = (Func<KhuyenMaiDTO, ND?>)KhuyenMai,
        [typeof(KhachHangDTO)] = (Func<KhachHangDTO, ND?>)KhachHang,
        [typeof(NhanVienDTO)] = (Func<NhanVienDTO, ND?>)NhanVien,
        [typeof(NhatKySqlDTO)] = (Func<NhatKySqlDTO, ND?>)NhatKySql,
    };

    public static Func<T, ND?>? MacDinh<T>() => Mau.TryGetValue(typeof(T), out var f) ? (Func<T, ND?>)f : null;

    // ------------------------------------------------------------------ ban hang
    public static ND DonHang(DonDatHangDTO d)
    {
        var dongs = new List<D>
        {
            new("Ngày đặt", GiaoDien.GioVn(d.NgayDat)),
            new("Khách hàng", $"{d.MaKh} · {d.TenKhachHang}"),
            new("Người nhận", $"{d.TenNguoiNhan ?? d.TenKhachHang} · {d.SdtNhan}"),
            new("Giao tới", d.DiaChiGiao),
            new("Nguồn", Nhan.Ten(Nhan.NguonDon, d.Nguon), Nhan.Mau(Nhan.NguonDon, d.Nguon)),
            new("Thanh toán", Nhan.Ten(Nhan.PhuongThuc, d.PhuongThucTt)),
        };
        if (d.TienGiam > 0) dongs.Add(new("Giảm giá", $"{GiaoDien.Tien(d.TienGiam)}{(d.MaVoucher != null ? $" (voucher {d.MaVoucher})" : "")}"));
        dongs.Add(new("Tổng tiền", GiaoDien.Tien(d.TongTien), GiaoDien.ChuChinh));
        if (d.MaHoaDon != null) dongs.Add(new("Hóa đơn", d.MaHoaDon));
        if (d.TenNhanVien != null) dongs.Add(new("Nhân viên", d.TenNhanVien));
        if (d.YeuCauHuy != null)
            dongs.Add(new("Xin hủy", $"{Nhan.Ten(Nhan.YeuCauHuy, d.YeuCauHuy)}{(d.LyDoHuy != null ? $": {d.LyDoHuy}" : "")}", Nhan.Mau(Nhan.YeuCauHuy, d.YeuCauHuy)));
        else if (d.LyDoHuy != null) dongs.Add(new("Lý do hủy", d.LyDoHuy, GiaoDien.Do));
        return new ND($"Đơn {d.MaDon}", d.ChiTiets.Count > 0 ? $"{d.ChiTiets.Count} sản phẩm" : null, dongs, Nhan.Mau(Nhan.TrangThaiDon, d.TrangThai), Nhan.Ten(Nhan.TrangThaiDon, d.TrangThai));
    }

    public static ND HoaDon(HoaDonDTO h)
    {
        var dongs = new List<D>
        {
            new("Ngày lập", GiaoDien.NgayVn(h.NgayLap)),
            new("Khách hàng", h.MaKh == null ? "Khách lẻ" : $"{h.MaKh} · {h.TenKhachHang}"),
            new("Nhân viên", $"{h.MaNv} · {h.TenNhanVien}"),
            new("Nguồn", h.MaDon == null ? "Bán tại quầy" : $"Đơn online {h.MaDon}"),
            new("Thanh toán", Nhan.Ten(Nhan.PhuongThuc, h.PhuongThucTt)),
        };
        if (h.TienGiam > 0) dongs.Add(new("Khuyến mãi", $"{h.TenKm ?? h.MaKm} · giảm {GiaoDien.Tien(h.TienGiam)}"));
        dongs.Add(new("Tổng tiền", GiaoDien.Tien(h.TongTien), GiaoDien.ChuChinh));
        return new ND($"Hóa đơn {h.MaHoaDon}", h.TinhThanhKhach, dongs,
            Nhan.Mau(Nhan.TrangThaiHoaDon, h.TrangThai), Nhan.Ten(Nhan.TrangThaiHoaDon, h.TrangThai));
    }

    public static ND DoiTra(DoiTraDTO d)
    {
        var dongs = new List<D>
        {
            new("Loại", Nhan.Ten(Nhan.LoaiDoiTra, d.Loai)),
            new("Hóa đơn gốc", d.MaHoaDon),
            new("Khách hàng", d.MaKh == null ? "Khách lẻ" : $"{d.MaKh} · {d.TenKhachHang}"),
            new("Ngày yêu cầu", GiaoDien.NgayVn(d.NgayDoiTra)),
            new("Lý do", d.LyDo),
            new("Tiền hoàn", d.TienHoan > 0 ? GiaoDien.Tien(d.TienHoan) : "Không", d.TienHoan > 0 ? GiaoDien.ChuChinh : null),
            new("Nhân viên", d.TenNhanVien),
        };
        if (d.NgayHoan != null) dongs.Add(new("Hoàn tất", GiaoDien.NgayVn(d.NgayHoan)));
        return new ND($"Phiếu {d.MaDoiTra}", null, dongs, Nhan.Mau(Nhan.TrangThaiDoiTra, d.TrangThai), Nhan.Ten(Nhan.TrangThaiDoiTra, d.TrangThai));
    }

    public static ND HoanTien(HoanTienDTO h) => new($"Hoàn tiền {h.MaHoanTien}", $"Đơn {h.MaDon} · hóa đơn {h.MaHoaDon}", new List<D>
    {
        new("Khách hàng", $"{h.TenKhachHang} · {h.SdtNhan}"),
        new("Số tiền", GiaoDien.Tien(h.SoTien), GiaoDien.ChuChinh),
        new("Phương thức", Nhan.Ten(Nhan.PhuongThuc, h.PhuongThuc)),
        new("Tạo lúc", GiaoDien.GioVn(h.NgayTao)),
        new("Đã hoàn lúc", h.NgayHoan == null ? "Chưa hoàn" : GiaoDien.GioVn(h.NgayHoan)),
        new("Người hoàn", h.TenNhanVien),
        new("Ghi chú", h.GhiChu),
    }, Nhan.Mau(Nhan.TrangThaiHoanTien, h.TrangThai), Nhan.Ten(Nhan.TrangThaiHoanTien, h.TrangThai));

    // ------------------------------------------------------------------ kho, danh muc
    public static ND SanPham(SanPhamDTO s) => new(s.TenSp, $"{s.MaSp} · {s.TenLoaiSp ?? s.MaLoaiSp}", new List<D>
    {
        new("Màu / size", $"{s.MauSac ?? "-"} / {s.KichCo ?? "-"}"),
        new("Đơn giá", GiaoDien.Tien(s.DonGia), GiaoDien.ChuChinh),
        new("Tồn kho", s.SoLuong == 0 ? "Hết hàng" : $"{s.SoLuong:N0} cái{(s.SoLuong < 5 ? " (sắp hết)" : "")}",
            s.SoLuong < 5 ? GiaoDien.Do : GiaoDien.XanhLa),
        new("Chất liệu", s.ChatLieu),
        new("Mô tả", RutGon(s.MoTa, 140)),
    }, Nhan.Mau(Nhan.TrangThaiSanPham, s.TrangThai), Nhan.Ten(Nhan.TrangThaiSanPham, s.TrangThai));

    public static ND NhaCungCap(NhaCungCapDTO n) => new(n.TenNcc, n.MaNcc, new List<D>
    {
        new("SĐT", n.Sdt),
        new("Email", n.Email),
        new("Địa chỉ", n.DiaChi),
        new("Mã số thuế", n.MaSoThue),
    }, Nhan.Mau(Nhan.TrangThaiNcc, n.TrangThai), Nhan.Ten(Nhan.TrangThaiNcc, n.TrangThai));

    public static ND PhieuNhap(PhieuNhapDTO p) => new($"Phiếu nhập {p.MaPhieuNhap}", null, new List<D>
    {
        new("Ngày nhập", GiaoDien.NgayVn(p.NgayNhap)),
        new("Nhà cung cấp", $"{p.MaNcc} · {p.TenNcc}"),
        new("Người lập", $"{p.MaNv} · {p.TenNhanVien}"),
        new("Số dòng hàng", p.ChiTiets.Count > 0 ? p.ChiTiets.Count.ToString() : null),
        new("Tổng tiền", GiaoDien.Tien(p.TongTien), GiaoDien.ChuChinh),
    }.Where(d => d.GiaTri != null).ToList(), GiaoDien.XanhDuong, "Nhập kho");

    public static ND KhuyenMai(KhuyenMaiDTO k)
    {
        var homNay = DateTime.Today;
        var (tinhTrang, mau) = homNay < k.NgayBd.Date ? ("Sắp diễn ra", GiaoDien.XanhDuong)
            : homNay > k.NgayKt.Date ? ("Đã kết thúc", GiaoDien.Xam) : ("Đang chạy", GiaoDien.XanhLa);
        var dongs = new List<D>
        {
            new("Hình thức", Nhan.Ten(Nhan.HinhThucKm, k.HinhThuc)),
            new("Thời gian", $"{GiaoDien.NgayVn(k.NgayBd)} - {GiaoDien.NgayVn(k.NgayKt)}"),
        };
        if (tinhTrang == "Đang chạy") dongs.Add(new("Còn lại", $"{(k.NgayKt.Date - homNay).Days + 1} ngày"));
        if (k.PhanTramGiamGia is > 0) dongs.Add(new("Giảm hiển thị", $"{k.PhanTramGiamGia:0.##}%"));
        if (k.Vouchers.Count > 0) dongs.Add(new("Mã voucher", string.Join(", ", k.Vouchers.Select(v => v.MaVoucher))));
        if (k.SanPhams.Count > 0) dongs.Add(new("Áp dụng", $"{k.SanPhams.Count} sản phẩm"));
        if (k.MucHoaDons.Count > 0) dongs.Add(new("Áp dụng", $"{k.MucHoaDons.Count} mức hóa đơn"));
        return new ND(k.TenKm, k.MaKm, dongs, mau, tinhTrang);
    }

    // ------------------------------------------------------------------ con nguoi
    public static ND KhachHang(KhachHangDTO k) => new(k.HoTen, $"{k.MaKh} · {(k.MaTk == null ? "chưa có tài khoản" : $"tài khoản {k.MaTk}")}", new List<D>
    {
        new("SĐT", k.Sdt == null ? null : k.Sdt + (k.SdtDaXacThuc ? "  (đã xác thực)" : "")),
        new("Email", k.Email),
        new("Ngày sinh", GiaoDien.NgayVn(k.NgaySinh)),
        new("Điểm tích lũy", $"{k.DiemTichLuy:N0} điểm"),
        new("Địa chỉ", string.Join(", ", new[] { k.DiaChi, k.TinhThanh }.Where(s => !string.IsNullOrWhiteSpace(s)))),
    }, Nhan.Mau(Nhan.HangThanhVien, k.HangThanhVien), Nhan.Ten(Nhan.HangThanhVien, k.HangThanhVien));

    public static ND NhanVien(NhanVienDTO n) => new(n.HoTen, $"{n.MaNv} · {n.ChucVu}", new List<D>
    {
        new("SĐT", n.Sdt),
        new("Email", n.Email),
        new("Ngày vào làm", GiaoDien.NgayVn(n.NgayVaoLam)),
        new("Lương", GiaoDien.Tien(n.Luong)),
        new("Tài khoản", n.TenDangNhap == null ? null : $"@{n.TenDangNhap} · {Nhan.Ten(Nhan.VaiTro, n.VaiTro)}"),
    }, Nhan.Mau(Nhan.TrangThaiNhanVien, n.TrangThai), Nhan.Ten(Nhan.TrangThaiNhanVien, n.TrangThai));

    // ------------------------------------------------------------------ he thong
    public static ND NhatKySql(NhatKySqlDTO n) => new($"{n.File}:{n.Dong}", $"{n.HamGoi}() · {GiaoDien.GioVn(n.ThoiDiem)}", new List<D>
    {
        new("Câu SQL", RutGon(GopDong(n.CauSql), 160)),
        new("Tham số", string.IsNullOrWhiteSpace(n.ThamSo) ? "(không có)" : RutGon(n.ThamSo, 120)),
        new("Thời gian", $"{n.ThoiGianMs} ms · {n.SoDong:N0} dòng"),
    }, n.ThoiGianMs >= 100 ? GiaoDien.Vang : GiaoDien.XanhLa, $"{n.ThoiGianMs} ms");

    private static string GopDong(string s) => string.Join(" ", s.Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));

    private static string? RutGon(string? s, int toiDa) => s == null || s.Length <= toiDa ? s : s[..toiDa].TrimEnd() + "…";
}
