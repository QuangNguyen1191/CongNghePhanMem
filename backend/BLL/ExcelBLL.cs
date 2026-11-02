using System.Globalization;
using ClosedXML.Excel;
using DTO;

namespace BLL;

// Yeu cau 7: ghi danh sach / bao cao ra Excel va doc du lieu tu Excel vao CSDL.
// Doc theo 2 buoc: Doc...() kiem tra tung dong, tra ve dong hop le + loi de xem truoc;
// Nhap...() moi ghi vao CSDL qua cac BLL nghiep vu (khong ghi thang xuong DAL).
public class ExcelBLL
{
    private sealed record Cot<T>(string TieuDe, Func<T, object?> Lay, string? DinhDang = null);

    private const string DinhDangTien = "#,##0";
    private const string DinhDangNgay = "dd/MM/yyyy";

    // ================================================================== GHI
    public byte[] XuatSanPham(IEnumerable<SanPhamDTO> ds) => Xuat("Sản phẩm", "DANH SÁCH SẢN PHẨM", ds, new List<Cot<SanPhamDTO>>
    {
        new("Mã SP", x => x.MaSp), new("Tên sản phẩm", x => x.TenSp), new("Loại", x => x.TenLoaiSp ?? x.MaLoaiSp),
        new("Kích cỡ", x => x.KichCo), new("Màu sắc", x => x.MauSac), new("Chất liệu", x => x.ChatLieu),
        new("Đơn giá", x => x.DonGia, DinhDangTien), new("Tồn kho", x => x.SoLuong), new("Trạng thái", x => x.TrangThai)
    });

    public byte[] XuatKhachHang(IEnumerable<KhachHangDTO> ds) => Xuat("Khách hàng", "DANH SÁCH KHÁCH HÀNG", ds, new List<Cot<KhachHangDTO>>
    {
        new("Mã KH", x => x.MaKh), new("Họ tên", x => x.HoTen), new("SĐT", x => x.Sdt), new("Email", x => x.Email),
        new("Ngày sinh", x => x.NgaySinh, DinhDangNgay), new("Địa chỉ", x => x.DiaChi), new("Tỉnh thành", x => x.TinhThanh),
        new("Hạng", x => x.HangThanhVien), new("Điểm", x => x.DiemTichLuy), new("Có tài khoản", x => x.MaTk != null ? "Có" : "Không")
    });

    public byte[] XuatNhaCungCap(IEnumerable<NhaCungCapDTO> ds) => Xuat("Nhà cung cấp", "DANH SÁCH NHÀ CUNG CẤP", ds, new List<Cot<NhaCungCapDTO>>
    {
        new("Mã NCC", x => x.MaNcc), new("Tên nhà cung cấp", x => x.TenNcc), new("Địa chỉ", x => x.DiaChi),
        new("SĐT", x => x.Sdt), new("Email", x => x.Email), new("Mã số thuế", x => x.MaSoThue), new("Trạng thái", x => x.TrangThai)
    });

    public byte[] XuatNhanVien(IEnumerable<NhanVienDTO> ds) => Xuat("Nhân viên", "DANH SÁCH NHÂN VIÊN", ds, new List<Cot<NhanVienDTO>>
    {
        new("Mã NV", x => x.MaNv), new("Họ tên", x => x.HoTen), new("Chức vụ", x => x.ChucVu), new("SĐT", x => x.Sdt),
        new("Email", x => x.Email), new("Ngày vào làm", x => x.NgayVaoLam, DinhDangNgay), new("Lương", x => x.Luong, DinhDangTien),
        new("Tên đăng nhập", x => x.TenDangNhap), new("Vai trò", x => x.VaiTro), new("Trạng thái", x => x.TrangThai)
    });

    public byte[] XuatHoaDon(IEnumerable<HoaDonDTO> ds) => Xuat("Hóa đơn", "DANH SÁCH HÓA ĐƠN", ds, new List<Cot<HoaDonDTO>>
    {
        new("Mã HĐ", x => x.MaHoaDon), new("Ngày lập", x => x.NgayLap, DinhDangNgay), new("Khách hàng", x => x.TenKhachHang ?? "Khách vãng lai"),
        new("Nhân viên", x => x.TenNhanVien), new("Mã đơn", x => x.MaDon), new("Thanh toán", x => x.PhuongThucTt),
        new("Tiền giảm", x => x.TienGiam, DinhDangTien), new("Tổng tiền", x => x.TongTien, DinhDangTien), new("Trạng thái", x => x.TrangThai)
    });

    public byte[] XuatPhieuNhap(IEnumerable<PhieuNhapDTO> ds) => Xuat("Phiếu nhập", "DANH SÁCH PHIẾU NHẬP", ds, new List<Cot<PhieuNhapDTO>>
    {
        new("Mã phiếu", x => x.MaPhieuNhap), new("Ngày nhập", x => x.NgayNhap, DinhDangNgay), new("Nhà cung cấp", x => x.TenNcc),
        new("Nhân viên", x => x.TenNhanVien), new("Tổng tiền", x => x.TongTien, DinhDangTien)
    });

    // Bang thong ke (yeu cau 5) ra Excel, dong "Tong cong" in dam
    public byte[] XuatBangThongKe(BangThongKeDTO bang)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Thống kê");
        var soCot = bang.Cots.Count + 1;
        TieuDeTrang(ws, bang.TieuDe.ToUpper(), soCot);
        ws.Cell(2, 1).Value = $"Cách tính: {(bang.CachTinh == ThongKeBLL.TaiSuDung ? "tái sử dụng danh sách" : "truy vấn mới")} - lập lúc {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(4, 1).Value = "";
        for (var c = 0; c < bang.Cots.Count; c++) ws.Cell(4, c + 2).Value = bang.Cots[c];
        DongTieuDe(ws.Range(4, 1, 4, soCot));
        var r = 5;
        foreach (var d in bang.Dongs)
        {
            ws.Cell(r, 1).Value = d.Nhan;
            for (var c = 0; c < d.GiaTris.Count; c++)
            {
                ws.Cell(r, c + 2).Value = d.GiaTris[c];
                ws.Cell(r, c + 2).Style.NumberFormat.Format = DinhDangTien;
            }
            if (d.Nhan == ThongKeBLL.TongCong) ws.Range(r, 1, r, soCot).Style.Font.Bold = true;
            r++;
        }
        KeBang(ws.Range(4, 1, Math.Max(4, r - 1), soCot));
        ws.Columns().AdjustToContents();
        return Luu(wb);
    }

    // ================================================================== FILE MAU
    private static readonly string[] CotSanPham = { "Tên sản phẩm*", "Mã loại*", "Chất liệu", "Mô tả", "Đơn giá*", "Kích cỡ", "Màu sắc", "Hình ảnh" };
    private static readonly string[] CotKhachHang = { "Họ tên*", "SĐT", "Email", "Ngày sinh", "Địa chỉ", "Tỉnh thành" };
    private static readonly string[] CotNhaCungCap = { "Tên nhà cung cấp*", "Địa chỉ", "SĐT", "Email", "Mã số thuế" };
    private static readonly string[] CotChiTietNhap = { "Mã SP*", "Số lượng*", "Đơn giá nhập*" };

    public byte[] MauSanPham() => Mau("Sản phẩm", CotSanPham,
        new object[] { "Áo thun cotton cổ tròn", "LSP01", "Cotton", "Áo thun basic", 249000, "M", "Trắng", "" });
    public byte[] MauKhachHang() => Mau("Khách hàng", CotKhachHang,
        new object[] { "Nguyễn Văn A", "0901234567", "a@gmail.com", "15/08/2000", "12 Lê Lợi, Q1", "TP Hồ Chí Minh" });
    public byte[] MauNhaCungCap() => Mau("Nhà cung cấp", CotNhaCungCap,
        new object[] { "Công ty May ABC", "KCN Tân Bình", "0281234567", "abc@may.vn", "0312345678" });
    public byte[] MauChiTietPhieuNhap() => Mau("Chi tiết nhập", CotChiTietNhap, new object[] { "SP001", 20, 120000 });

    // ================================================================== DOC
    public KetQuaDocExcelDTO<SanPhamDTO> DocSanPham(Stream file) => Doc(file, CotSanPham, d => new SanPhamDTO
    {
        TenSp = d.Chuoi(0, batBuoc: true)!, MaLoaiSp = d.Chuoi(1, batBuoc: true)!.ToUpper(), ChatLieu = d.Chuoi(2),
        MoTa = d.Chuoi(3), DonGia = d.So(4, batBuoc: true)!.Value, KichCo = d.Chuoi(5)?.ToUpper(), MauSac = d.Chuoi(6),
        HinhAnh = d.Chuoi(7), TrangThai = "DANG_BAN"
    }, s =>
    {
        QuyTac.KiemTra(s.DonGia > 0, "Đơn giá phải lớn hơn 0");
        // bao loi ngay o buoc xem truoc, khong de toi luc ghi moi hong ca lo
        QuyTac.KiemTra(_loaiSp.Value.Contains(s.MaLoaiSp), $"Không có loại sản phẩm {s.MaLoaiSp}");
    });

    private readonly Lazy<HashSet<string>> _loaiSp =
        new(() => new LoaiSpBLL().LayTatCa().Select(l => l.MaLoaiSp).ToHashSet(StringComparer.OrdinalIgnoreCase));

    public KetQuaDocExcelDTO<KhachHangDTO> DocKhachHang(Stream file) => Doc(file, CotKhachHang, d => new KhachHangDTO
    {
        HoTen = d.Chuoi(0, batBuoc: true)!, Sdt = d.Chuoi(1), Email = d.Chuoi(2), NgaySinh = d.Ngay(3),
        DiaChi = d.Chuoi(4), TinhThanh = d.Chuoi(5)
    }, k => QuyTac.KiemTra(k.Sdt == null || k.Sdt.All(char.IsDigit), "SĐT chỉ gồm chữ số"));

    public KetQuaDocExcelDTO<NhaCungCapDTO> DocNhaCungCap(Stream file) => Doc(file, CotNhaCungCap, d => new NhaCungCapDTO
    {
        TenNcc = d.Chuoi(0, batBuoc: true)!, DiaChi = d.Chuoi(1), Sdt = d.Chuoi(2), Email = d.Chuoi(3), MaSoThue = d.Chuoi(4)
    });

    public KetQuaDocExcelDTO<ChiTietPhieuNhapDTO> DocChiTietPhieuNhap(Stream file)
    {
        var sp = new SanPhamBLL().LayTatCa().ToDictionary(s => s.MaSp, StringComparer.OrdinalIgnoreCase);
        return Doc(file, CotChiTietNhap, d => new ChiTietPhieuNhapDTO
        {
            MaSp = d.Chuoi(0, batBuoc: true)!.ToUpper(), SoLuong = (int)d.So(1, batBuoc: true)!.Value,
            DonGiaNhap = d.So(2, batBuoc: true)!.Value
        }, c =>
        {
            QuyTac.KiemTra(sp.TryGetValue(c.MaSp, out var s), $"Không có sản phẩm {c.MaSp}");
            QuyTac.KiemTra(c.SoLuong > 0, "Số lượng phải lớn hơn 0");
            QuyTac.KiemTra(c.DonGiaNhap > 0, "Đơn giá nhập phải lớn hơn 0");
            c.TenSp = s!.TenSp; c.KichCo = s.KichCo; c.MauSac = s.MauSac;
            c.ThanhTien = c.SoLuong * c.DonGiaNhap;
        });
    }

    // ================================================================== NHAP VAO CSDL (sau khi nguoi dung xem truoc)
    // San pham: tat ca hoac khong (1 giao dich trong SanPhamBLL.ThemNhieu)
    public List<SanPhamDTO> NhapSanPham(List<SanPhamDTO> ds) => new SanPhamBLL().ThemNhieu(ds);

    // Khach hang / NCC: them tung dong, dong loi (vd trung SĐT) bao lai, cac dong khac van vao
    public (int SoDongDaThem, List<LoiDongExcelDTO> Lois) NhapKhachHang(List<KhachHangDTO> ds) =>
        TungDong(ds, k => new KhachHangBLL().ThemTaiQuay(k));

    public (int SoDongDaThem, List<LoiDongExcelDTO> Lois) NhapNhaCungCap(List<NhaCungCapDTO> ds) =>
        TungDong(ds, n => new NhaCungCapBLL().Them(n));

    // ================================================================== ham phu
    private static byte[] Xuat<T>(string tenSheet, string tieuDe, IEnumerable<T> ds, List<Cot<T>> cots)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(tenSheet);
        TieuDeTrang(ws, tieuDe, cots.Count);
        ws.Cell(2, 1).Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm}";
        for (var c = 0; c < cots.Count; c++) ws.Cell(4, c + 1).Value = cots[c].TieuDe;
        DongTieuDe(ws.Range(4, 1, 4, cots.Count));
        var r = 5;
        foreach (var x in ds)
        {
            for (var c = 0; c < cots.Count; c++)
            {
                var o = ws.Cell(r, c + 1);
                o.Value = GiaTriO(cots[c].Lay(x));
                if (cots[c].DinhDang != null) o.Style.NumberFormat.Format = cots[c].DinhDang;
            }
            r++;
        }
        KeBang(ws.Range(4, 1, Math.Max(4, r - 1), cots.Count));
        ws.Cell(r + 1, 1).Value = $"Tổng số dòng: {r - 5}";
        ws.SheetView.FreezeRows(4);
        ws.Columns().AdjustToContents();
        return Luu(wb);
    }

    private static byte[] Mau(string tenSheet, string[] cots, object[] viDu)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(tenSheet);
        for (var c = 0; c < cots.Length; c++)
        {
            ws.Cell(1, c + 1).Value = cots[c];
            ws.Cell(2, c + 1).Value = GiaTriO(viDu[c]);
        }
        DongTieuDe(ws.Range(1, 1, 1, cots.Length));
        ws.Cell(4, 1).Value = "Cột có dấu * là bắt buộc. Xóa dòng ví dụ trước khi nhập. Ngày ghi dạng dd/MM/yyyy.";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Columns().AdjustToContents();
        return Luu(wb);
    }

    private static KetQuaDocExcelDTO<T> Doc<T>(Stream file, string[] cotCan, Func<DongExcel, T> tao, Action<T>? kiemTra = null)
    {
        XLWorkbook wb;
        try { wb = new XLWorkbook(file); }
        catch (Exception) { throw new LoiNghiepVu("File không phải Excel (.xlsx) hợp lệ."); }
        using (wb)
        {
            var ws = wb.Worksheets.First();
            // Tim vi tri tung cot theo tieu de o dong 1 (khong phan biet hoa thuong, bo dau *)
            var cacO = ws.Row(1).CellsUsed().Select(c => (Ten: ChuanHoa(c.GetString()), Cot: c.Address.ColumnNumber)).Where(c => c.Ten.Length > 0).ToList();
            var trung = cacO.GroupBy(c => c.Ten).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            QuyTac.KiemTra(trung.Count == 0, "File có cột tiêu đề bị trùng: " + string.Join(", ", trung));
            var tieuDe = cacO.ToDictionary(c => c.Ten, c => c.Cot);
            var viTri = cotCan.Select(c => tieuDe.TryGetValue(ChuanHoa(c), out var so) ? so : 0).ToArray();
            var thieu = cotCan.Where((c, i) => viTri[i] == 0 && c.EndsWith('*')).ToList();
            QuyTac.KiemTra(thieu.Count == 0, "File thiếu cột: " + string.Join(", ", thieu) + ". Hãy dùng file mẫu.");

            var kq = new KetQuaDocExcelDTO<T>();
            var dongCuoi = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (var r = 2; r <= dongCuoi; r++)
            {
                var dong = new DongExcel(ws.Row(r), viTri, cotCan);
                if (dong.Rong) continue;
                if (r > 2 && dong.LaGhiChu) continue;
                kq.TongSoDong++;
                try
                {
                    var x = tao(dong);
                    kiemTra?.Invoke(x);
                    kq.HopLe.Add(x);
                }
                catch (LoiNghiepVu e) { kq.Lois.Add(new LoiDongExcelDTO { Dong = r, ThongBao = e.Message }); }
            }
            return kq;
        }
    }

    private static (int, List<LoiDongExcelDTO>) TungDong<T>(List<T> ds, Action<T> them)
    {
        var lois = new List<LoiDongExcelDTO>();
        var so = 0;
        for (var i = 0; i < ds.Count; i++)
        {
            try { them(ds[i]); so++; }
            catch (LoiNghiepVu e) { lois.Add(new LoiDongExcelDTO { Dong = i + 1, ThongBao = e.Message }); }
        }
        return (so, lois);
    }

    // 1 dong du lieu, doc o theo thu tu cot cua file mau
    private sealed class DongExcel
    {
        private readonly IXLRow _dong;
        private readonly int[] _viTri;
        private readonly string[] _ten;

        public DongExcel(IXLRow dong, int[] viTri, string[] ten) { _dong = dong; _viTri = viTri; _ten = ten; }

        public bool Rong => _viTri.Where(v => v > 0).All(v => _dong.Cell(v).IsEmpty());
        // Dong ghi chu cua file mau: chi co o dau tien co chu
        public bool LaGhiChu => _viTri.Skip(1).Where(v => v > 0).All(v => _dong.Cell(v).IsEmpty())
            && _dong.Cell(_viTri[0] > 0 ? _viTri[0] : 1).GetString().StartsWith("Cột có dấu");

        private IXLCell? O(int i) => _viTri[i] > 0 ? _dong.Cell(_viTri[i]) : null;
        private string Ten(int i) => _ten[i].TrimEnd('*');

        public string? Chuoi(int i, bool batBuoc = false)
        {
            var s = QuyTac.RongThanhNull(O(i)?.GetFormattedString());
            QuyTac.KiemTra(!batBuoc || s != null, $"Thiếu {Ten(i)}");
            return s;
        }

        public decimal? So(int i, bool batBuoc = false)
        {
            var o = O(i);
            if (o == null || o.IsEmpty()) { QuyTac.KiemTra(!batBuoc, $"Thiếu {Ten(i)}"); return null; }
            if (o.DataType == XLDataType.Number) return (decimal)o.GetDouble();
            var s = o.GetString().Trim().Replace(".", "").Replace(",", "").Replace("đ", "").Trim();
            QuyTac.KiemTra(decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var so),
                $"{Ten(i)} phải là số");
            return so;
        }

        public DateTime? Ngay(int i)
        {
            var o = O(i);
            if (o == null || o.IsEmpty()) return null;
            if (o.DataType == XLDataType.DateTime) return o.GetDateTime().Date;
            QuyTac.KiemTra(DateTime.TryParseExact(o.GetString().Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngay), $"{Ten(i)} phải có dạng dd/MM/yyyy");
            return ngay;
        }
    }

    private static string ChuanHoa(string s) => s.Trim().TrimEnd('*').Trim().ToLowerInvariant();

    private static XLCellValue GiaTriO(object? v) => v switch
    {
        null => Blank.Value,
        string s => s,
        DateTime d => d,
        decimal m => m,
        int n => n,
        double d => d,
        _ => v.ToString()
    };

    private static void TieuDeTrang(IXLWorksheet ws, string tieuDe, int soCot)
    {
        ws.Cell(1, 1).Value = tieuDe;
        ws.Range(1, 1, 1, Math.Max(1, soCot)).Merge().Style.Font.SetBold().Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void DongTieuDe(IXLRange r) =>
        r.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#DDE7F3"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

    private static void KeBang(IXLRange r) =>
        r.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

    private static byte[] Luu(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
