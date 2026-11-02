using System.Data;
using DAL;
using DTO;

namespace BLL;

public class KhuyenMaiBLL
{
    private readonly KhuyenMaiDAL _dal = new();
    private readonly SanPhamDAL _spDal = new();

    public List<KhuyenMaiDTO> LayTatCa() => _dal.LayTatCa();

    public List<KhuyenMaiDTO> LayDangChay() => _dal.LayDangChay(DateTime.Today);

    public KhuyenMaiDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy khuyến mãi");

    public KhuyenMaiDTO Them(KhuyenMaiDTO k)
    {
        KiemTra(k);
        DbHelper.GiaoDich(tx =>
        {
            k.MaKm = DbHelper.SinhMa("KHUYEN_MAI", "ma_km", "KM", 2, tx);
            _dal.Them(k, tx);
        });
        return LayTheoMa(k.MaKm);
    }

    public void Sua(KhuyenMaiDTO k)
    {
        LayTheoMa(k.MaKm);
        KiemTra(k);
        DbHelper.GiaoDich(tx => _dal.Sua(k, tx));
    }

    // Ket thuc ngay: KM khong con ap dung tu hom nay. KM da het han tu truoc thi giu nguyen.
    public void KetThuc(string ma)
    {
        LayTheoMa(ma);
        _dal.KetThuc(ma, DateTime.Today.AddDays(-1));
    }

    private void KiemTra(KhuyenMaiDTO k)
    {
        k.TenKm = k.TenKm.Trim();
        QuyTac.KiemTra(k.TenKm.Length > 0, "Vui lòng nhập tên chương trình");
        QuyTac.KiemTra(k.NgayKt.Date >= k.NgayBd.Date, "Ngày kết thúc phải sau ngày bắt đầu");
        QuyTac.KiemTra(k.PhanTramGiamGia is null or >= 0 and <= 100, "Phần trăm giảm phải từ 0 đến 100");
        switch (k.HinhThuc)
        {
            case "SAN_PHAM":
                QuyTac.KiemTra(k.SanPhams.Count > 0, "Chọn ít nhất 1 sản phẩm được giảm giá");
                foreach (var c in k.SanPhams)
                {
                    QuyTac.KiemTra(_spDal.LayTheoMa(c.MaSp) != null, $"Sản phẩm {c.MaSp} không tồn tại");
                    QuyTac.KiemTra(c.PhanTramGiamGia is > 0 and <= 100, "Phần trăm giảm phải từ 0 đến 100");
                }
                QuyTac.KiemTra(k.SanPhams.Select(c => c.MaSp).Distinct().Count() == k.SanPhams.Count, "Sản phẩm bị lặp");
                break;
            case "HOA_DON":
                QuyTac.KiemTra(k.MucHoaDons.Count > 0, "Nhập ít nhất 1 mức áp dụng hóa đơn");
                QuyTac.KiemTra(k.MucHoaDons.All(m => m.MucApDungHoaDon >= 0 && m.PhanTramGiamGia is > 0 and <= 100),
                    "Mức áp dụng hoặc phần trăm giảm không hợp lệ");
                break;
            case "VOUCHER":
                QuyTac.KiemTra(k.Vouchers.Count > 0, "Nhập ít nhất 1 mã voucher");
                foreach (var v in k.Vouchers)
                {
                    v.MaVoucher = v.MaVoucher.Trim().ToUpper();
                    QuyTac.KiemTra(v.MaVoucher.Length > 0, "Mã voucher không được trống");
                    QuyTac.KiemTra(!_dal.TrungVoucher(v.MaVoucher, k.MaKm ?? ""), $"Mã voucher {v.MaVoucher} đã tồn tại");
                    QuyTac.KiemTra(v.PhanTramGiamGia is > 0 and <= 100, "Phần trăm giảm phải từ 0 đến 100");
                }
                break;
            default:
                throw new LoiNghiepVu("Hình thức khuyến mãi phải là SAN_PHAM, HOA_DON hoặc VOUCHER");
        }
    }

    // Tinh tien cho 1 danh sach dong hang tai ngay chi dinh. Dung chung cho gio hang, dat hang, ban tai quay.
    //   1. Gia tung dong = don gia - % khuyen mai san pham lon nhat dang chay
    //   2. Giam tren ca hoa don: neu khach nhap voucher hop le thi dung voucher,
    //      khong thi lay muc khuyen mai hoa don cao nhat ma tam tinh dat duoc. Khong cong don 2 loai.
    public BangTinhTienDTO TinhTien(List<DongHangDTO> dongHangs, DateTime ngay, string? maVoucher,
        IDbTransaction? tx = null, bool kiemTraTon = true)
    {
        QuyTac.KiemTra(dongHangs.Count > 0, "Chưa có sản phẩm nào");
        var gop = dongHangs.GroupBy(d => d.MaSp.Trim().ToUpper())
            .Select(g => new DongHangDTO { MaSp = g.Key, SoLuong = g.Sum(d => d.SoLuong) }).ToList();
        var giamSp = _dal.LayGiamSanPhamDangChay(ngay, tx);
        var bang = new BangTinhTienDTO();
        string? maKmSanPham = null;

        foreach (var d in gop)
        {
            QuyTac.KiemTra(d.SoLuong > 0, "Số lượng phải lớn hơn 0");
            var sp = QuyTac.CanCo(_spDal.LayTheoMa(d.MaSp, tx), $"Không tìm thấy sản phẩm {d.MaSp}");
            QuyTac.KiemTra(sp.TrangThai == "DANG_BAN", $"Sản phẩm {sp.TenSp} đã ngừng bán");
            if (kiemTraTon)
                QuyTac.KiemTra(sp.SoLuong >= d.SoLuong, $"{TenDayDu(sp)} chỉ còn {sp.SoLuong} cái");
            var km = giamSp.Where(c => c.MaSp == sp.MaSp).MaxBy(c => c.PhanTramGiamGia);
            var gia = km == null ? sp.DonGia : Math.Round(sp.DonGia * (100 - km.PhanTramGiamGia) / 100, 0);
            if (km != null) maKmSanPham ??= km.MaKm;
            bang.Dongs.Add(new ChiTietHoaDonDTO
            {
                MaSp = sp.MaSp, SoLuong = d.SoLuong, DonGiaBan = gia, ThanhTien = gia * d.SoLuong,
                TenSp = sp.TenSp, KichCo = sp.KichCo, MauSac = sp.MauSac
            });
        }
        bang.TamTinh = bang.Dongs.Sum(d => d.ThanhTien);

        maVoucher = QuyTac.RongThanhNull(maVoucher)?.ToUpper();
        if (maVoucher != null)
        {
            var v = QuyTac.CanCo(_dal.LayVoucherDangChay(maVoucher, ngay, tx), "Mã voucher không tồn tại hoặc đã hết hạn");
            bang.TienGiam = Math.Round(bang.TamTinh * v.PhanTramGiamGia / 100, 0);
            bang.MaKm = v.MaKm;
            bang.GhiChuKm = $"Voucher {v.MaVoucher} giảm {v.PhanTramGiamGia:0.##}%";
        }
        else
        {
            var muc = _dal.LayMucHoaDonDangChay(ngay, tx).Where(m => bang.TamTinh >= m.MucApDungHoaDon)
                .MaxBy(m => m.PhanTramGiamGia);
            if (muc != null)
            {
                bang.TienGiam = Math.Round(bang.TamTinh * muc.PhanTramGiamGia / 100, 0);
                bang.MaKm = muc.MaKm;
                bang.GhiChuKm = $"Hóa đơn từ {muc.MucApDungHoaDon:N0}đ giảm {muc.PhanTramGiamGia:0.##}%";
            }
        }
        bang.MaKm ??= maKmSanPham;
        bang.TongTien = bang.TamTinh - bang.TienGiam;
        return bang;
    }

    internal static string TenDayDu(SanPhamDTO s) =>
        s.TenSp + (s.MauSac != null ? $" - {s.MauSac}" : "") + (s.KichCo != null ? $" - {s.KichCo}" : "");
}
