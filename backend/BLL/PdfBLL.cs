using System.Globalization;
using DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BLL;

// Thong tin in o dau moi chung tu. WebApi / app admin gan tu file cau hinh luc khoi dong.
public static class ThongTinCuaHang
{
    public static string Ten { get; set; } = "CỬA HÀNG THỜI TRANG";
    public static string DiaChi { get; set; } = "";
    public static string Sdt { get; set; } = "";
}

// Yeu cau 8: xuat PDF cho hoa don, don dat hang, phieu nhap, phieu doi tra va bao cao thong ke (in duoc).
public class PdfBLL
{
    private const string Font = "DejaVu Sans";
    private static readonly CultureInfo Vn = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly object _khoa = new();
    private static bool _daKhoiTao;

    public PdfBLL() => KhoiTao();

    // Nhung font co dau tieng Viet vao BLL, khong phu thuoc may chay co cai font hay khong
    private static void KhoiTao()
    {
        lock (_khoa)
        {
            if (_daKhoiTao) return;
            QuestPDF.Settings.License = LicenseType.Community;
            var asm = typeof(PdfBLL).Assembly;
            foreach (var ten in asm.GetManifestResourceNames().Where(n => n.EndsWith(".ttf")))
            {
                using var s = asm.GetManifestResourceStream(ten)!;
                QuestPDF.Drawing.FontManager.RegisterFont(s);
            }
            _daKhoiTao = true;
        }
    }

    // ================================================================== HOA DON
    public byte[] HoaDon(HoaDonDTO h) => ChungTu($"HÓA ĐƠN BÁN HÀNG", h.MaHoaDon, col =>
    {
        ThongTin(col, ("Ngày lập", h.NgayLap.ToString("dd/MM/yyyy")), ("Khách hàng", h.TenKhachHang ?? "Khách vãng lai"),
            ("Nhân viên", h.TenNhanVien ?? h.MaNv), ("Thanh toán", TenThanhToan(h.PhuongThucTt)),
            ("Mã đơn online", h.MaDon ?? "-"), ("Trạng thái", TenTrangThai(h.TrangThai)));
        BangHang(col, h.ChiTiets.Select(c => (c.MaSp, TenSp(c.TenSp, c.MauSac, c.KichCo), c.SoLuong, c.DonGiaBan, c.ThanhTien)));
        var tienHang = h.ChiTiets.Sum(c => c.ThanhTien);
        Tong(col, ("Tiền hàng", tienHang), ("Giảm giá" + (h.TenKm != null ? $" ({h.TenKm})" : ""), -h.TienGiam),
            ("TỔNG THANH TOÁN", h.TongTien));
        KyTen(col, "Khách hàng", "Nhân viên lập");
    });

    // ================================================================== DON DAT HANG (phieu giao hang)
    public byte[] DonDatHang(DonDatHangDTO d) => ChungTu("ĐƠN ĐẶT HÀNG", d.MaDon, col =>
    {
        ThongTin(col, ("Ngày đặt", d.NgayDat.ToString("dd/MM/yyyy HH:mm")), ("Khách hàng", d.TenKhachHang ?? d.MaKh),
            ("Người nhận", d.TenNguoiNhan ?? d.TenKhachHang ?? ""), ("SĐT nhận", d.SdtNhan),
            ("Địa chỉ giao", d.DiaChiGiao), ("Thanh toán", TenThanhToan(d.PhuongThucTt)),
            ("Voucher", d.MaVoucher ?? "-"), ("Trạng thái", TenTrangThai(d.TrangThai)));
        BangHang(col, d.ChiTiets.Select(c => (c.MaSp, TenSp(c.TenSp, c.MauSac, c.KichCo), c.SoLuong, c.DonGia, c.ThanhTien)));
        Tong(col, ("Tiền hàng", d.ChiTiets.Sum(c => c.ThanhTien)), ("Giảm giá", -d.TienGiam), ("TỔNG THANH TOÁN", d.TongTien));
        KyTen(col, "Người nhận", "Nhân viên giao");
    });

    // ================================================================== PHIEU NHAP
    public byte[] PhieuNhap(PhieuNhapDTO p) => ChungTu("PHIẾU NHẬP KHO", p.MaPhieuNhap, col =>
    {
        ThongTin(col, ("Ngày nhập", p.NgayNhap.ToString("dd/MM/yyyy")), ("Nhà cung cấp", p.TenNcc ?? p.MaNcc),
            ("Nhân viên", p.TenNhanVien ?? p.MaNv));
        BangHang(col, p.ChiTiets.Select(c => (c.MaSp, TenSp(c.TenSp, c.MauSac, c.KichCo), c.SoLuong, c.DonGiaNhap, c.ThanhTien)),
            "Đơn giá nhập");
        Tong(col, ("TỔNG TIỀN NHẬP", p.TongTien));
        KyTen(col, "Người giao hàng", "Thủ kho");
    });

    // ================================================================== PHIEU DOI TRA
    public byte[] DoiTra(DoiTraDTO d) => ChungTu(d.Loai == "DOI" ? "PHIẾU ĐỔI HÀNG" : "PHIẾU TRẢ HÀNG", d.MaDoiTra, col =>
    {
        ThongTin(col, ("Ngày yêu cầu", d.NgayDoiTra.ToString("dd/MM/yyyy")), ("Hóa đơn gốc", d.MaHoaDon),
            ("Khách hàng", d.TenKhachHang ?? "Khách vãng lai"), ("Nhân viên xử lý", d.TenNhanVien ?? "-"),
            ("Lý do", d.LyDo ?? "-"), ("Trạng thái", TenTrangThai(d.TrangThai)),
            ("Ngày hoàn tất", d.NgayHoan?.ToString("dd/MM/yyyy") ?? "-"));
        col.Item().PaddingTop(8).Table(t =>
        {
            t.ColumnsDefinition(c => { c.ConstantColumn(28); c.RelativeColumn(3); c.ConstantColumn(50); c.RelativeColumn(3); });
            t.Header(h => { foreach (var s in new[] { "STT", "Sản phẩm trả", "SL", "Đổi sang" }) h.Cell().Element(OTieuDe).Text(s); });
            var i = 1;
            foreach (var c in d.ChiTiets)
            {
                t.Cell().Element(O).AlignCenter().Text((i++).ToString());
                t.Cell().Element(O).Text($"{c.MaSp} - {c.TenSp}");
                t.Cell().Element(O).AlignCenter().Text(c.SoLuong.ToString());
                t.Cell().Element(O).Text(c.MaSpDoi != null ? $"{c.MaSpDoi} - {c.TenSpDoi}" : "-");
            }
        });
        if (d.Loai == "TRA") Tong(col, ("TIỀN HOÀN", d.TienHoan));
        KyTen(col, "Khách hàng", "Nhân viên xử lý");
    });

    // ================================================================== BAO CAO THONG KE
    public byte[] BaoCao(BangThongKeDTO bang) => Document.Create(doc => doc.Page(trang =>
    {
        trang.Size(bang.Cots.Count > 4 ? PageSizes.A4.Landscape() : PageSizes.A4);
        trang.Margin(28);
        trang.DefaultTextStyle(x => x.FontFamily(Font).FontSize(9));
        trang.Header().Column(col =>
        {
            DauTrang(col);
            col.Item().PaddingTop(10).AlignCenter().Text(bang.TieuDe.ToUpper()).Bold().FontSize(14);
            col.Item().AlignCenter().Text($"Cách tính: {(bang.CachTinh == ThongKeBLL.TaiSuDung ? "tái sử dụng danh sách" : "truy vấn mới")}" +
                $" - lập lúc {DateTime.Now:dd/MM/yyyy HH:mm}").Italic().FontSize(8);
        });
        trang.Content().PaddingTop(10).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3);
                foreach (var _ in bang.Cots) c.RelativeColumn(2);
            });
            t.Header(h =>
            {
                h.Cell().Element(OTieuDe).Text("");
                foreach (var cot in bang.Cots) h.Cell().Element(OTieuDe).Text(cot);
            });
            foreach (var d in bang.Dongs)
            {
                var dam = d.Nhan == ThongKeBLL.TongCong;
                var nhan = t.Cell().Element(O).Text(d.Nhan);
                if (dam) nhan.Bold();
                foreach (var v in d.GiaTris)
                {
                    var o = t.Cell().Element(O).AlignRight().Text(Tien(v));
                    if (dam) o.Bold();
                    if (v < 0) o.FontColor(Colors.Red.Darken2);
                }
            }
        });
        trang.Footer().AlignCenter().Text(x => { x.Span("Trang "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
    })).GeneratePdf();

    // ================================================================== khung chung
    private static byte[] ChungTu(string tieuDe, string ma, Action<ColumnDescriptor> noiDung) =>
        Document.Create(doc => doc.Page(trang =>
        {
            trang.Size(PageSizes.A4);
            trang.Margin(32);
            trang.DefaultTextStyle(x => x.FontFamily(Font).FontSize(10));
            trang.Header().Column(col =>
            {
                DauTrang(col);
                col.Item().PaddingTop(12).AlignCenter().Text(tieuDe).Bold().FontSize(16);
                col.Item().AlignCenter().Text($"Số: {ma}").FontSize(10);
            });
            trang.Content().PaddingTop(10).Column(noiDung);
            trang.Footer().AlignCenter().Text(x =>
            {
                x.Span($"In lúc {DateTime.Now:dd/MM/yyyy HH:mm} - Trang ").FontSize(8);
                x.CurrentPageNumber().FontSize(8);
                x.Span(" / ").FontSize(8);
                x.TotalPages().FontSize(8);
            });
        })).GeneratePdf();

    private static void DauTrang(ColumnDescriptor col)
    {
        col.Item().Text(ThongTinCuaHang.Ten).Bold().FontSize(13);
        if (ThongTinCuaHang.DiaChi != "") col.Item().Text(ThongTinCuaHang.DiaChi).FontSize(9);
        if (ThongTinCuaHang.Sdt != "") col.Item().Text("ĐT: " + ThongTinCuaHang.Sdt).FontSize(9);
        col.Item().PaddingTop(4).LineHorizontal(0.8f);
    }

    private static void ThongTin(ColumnDescriptor col, params (string Nhan, string GiaTri)[] dongs) =>
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c => { c.ConstantColumn(100); c.RelativeColumn(); });
            foreach (var (nhan, giaTri) in dongs)
            {
                t.Cell().PaddingVertical(1).Text(nhan + ":").SemiBold();
                t.Cell().PaddingVertical(1).Text(giaTri);
            }
        });

    private static void BangHang(ColumnDescriptor col,
        IEnumerable<(string MaSp, string Ten, int SoLuong, decimal DonGia, decimal ThanhTien)> dongs, string tenDonGia = "Đơn giá")
    {
        col.Item().PaddingTop(10).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(28); c.ConstantColumn(52); c.RelativeColumn(); c.ConstantColumn(36);
                c.ConstantColumn(80); c.ConstantColumn(90);
            });
            t.Header(h =>
            {
                foreach (var s in new[] { "STT", "Mã SP", "Sản phẩm", "SL", tenDonGia, "Thành tiền" })
                    h.Cell().Element(OTieuDe).Text(s);
            });
            var i = 1;
            foreach (var d in dongs)
            {
                t.Cell().Element(O).AlignCenter().Text((i++).ToString());
                t.Cell().Element(O).Text(d.MaSp);
                t.Cell().Element(O).Text(d.Ten);
                t.Cell().Element(O).AlignCenter().Text(d.SoLuong.ToString());
                t.Cell().Element(O).AlignRight().Text(Tien(d.DonGia));
                t.Cell().Element(O).AlignRight().Text(Tien(d.ThanhTien));
            }
        });
    }

    private static void Tong(ColumnDescriptor col, params (string Nhan, decimal Tien)[] dongs) =>
        col.Item().PaddingTop(8).AlignRight().Width(280).Table(t =>
        {
            t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(100); });
            for (var i = 0; i < dongs.Length; i++)
            {
                var cuoi = i == dongs.Length - 1;
                var a = t.Cell().PaddingVertical(2).Text(dongs[i].Nhan);
                var b = t.Cell().PaddingVertical(2).AlignRight().Text(Tien(dongs[i].Tien) + " đ");
                if (cuoi) { a.Bold(); b.Bold().FontSize(12); }
            }
        });

    private static void KyTen(ColumnDescriptor col, string trai, string phai) =>
        col.Item().PaddingTop(30).Row(r =>
        {
            foreach (var ai in new[] { trai, phai })
                r.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text(ai).Bold();
                    c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                });
        });

    private static IContainer O(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(3).PaddingHorizontal(3);

    private static IContainer OTieuDe(IContainer c) =>
        c.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter()
            .DefaultTextStyle(x => x.Bold());

    private static string Tien(decimal v) => v.ToString("#,##0", Vn);

    private static string TenSp(string? ten, string? mau, string? co) =>
        (ten ?? "") + (mau != null ? " - " + mau : "") + (co != null ? " - " + co : "");

    private static string TenThanhToan(string ma) => ma switch
    {
        "TIEN_MAT" => "Tiền mặt", "CHUYEN_KHOAN" => "Chuyển khoản", "THE" => "Thẻ", "COD" => "Thanh toán khi nhận hàng",
        "ONLINE" => "Thanh toán online", _ => ma
    };

    private static string TenTrangThai(string ma) => ma switch
    {
        "CHO_THANH_TOAN" => "Chờ thanh toán", "DA_THANH_TOAN" => "Đã thanh toán", "DA_HUY" => "Đã hủy",
        "CHO_XAC_NHAN" => "Chờ xác nhận", "DA_XAC_NHAN" => "Đã xác nhận", "DANG_GIAO" => "Đang giao",
        "HOAN_TAT" => "Hoàn tất", "CHO_DUYET" => "Chờ duyệt", "DA_DUYET" => "Đã duyệt", "TU_CHOI" => "Từ chối",
        _ => ma
    };
}
