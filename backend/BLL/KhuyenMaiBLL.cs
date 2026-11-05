using System.Data;
using DAL;
using DTO;

namespace BLL;

public class KhuyenMaiBLL
{
    private readonly KhuyenMaiDAL _dal = new();
    private readonly SanPhamDAL _spDal = new();
    private readonly KhachHangDAL _khDal = new();
    private readonly DonDatHangDAL _donDal = new();

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
            case "THANH_VIEN_MOI":
                var tv = k.ThanhVienMoi ?? throw new LoiNghiepVu("Nhập số đơn đầu và phần trăm giảm cho ưu đãi thành viên mới");
                QuyTac.KiemTra(tv.SoDonDau is >= 1 and <= 10, "Số đơn đầu được giảm phải từ 1 đến 10");
                QuyTac.KiemTra(tv.PhanTramGiamGia is > 0 and <= 100, "Phần trăm giảm phải từ 0 đến 100");
                QuyTac.KiemTra(tv.GiamToiDa is null or > 0, "Mức giảm tối đa phải lớn hơn 0");
                k.PhanTramGiamGia = tv.PhanTramGiamGia;
                break;
            default:
                throw new LoiNghiepVu("Hình thức khuyến mãi phải là SAN_PHAM, HOA_DON, VOUCHER hoặc THANH_VIEN_MOI");
        }
    }

    // v3: Uu dai thanh vien moi dang chay. Truyen ma KH thi tinh them so luot da dung / con lai cua khach do
    // (khach chua co tai khoan thi DaDung / ConLai = null: chua duoc huong, chi de quang ba).
    public UuDaiThanhVienMoiDTO? LayUuDaiThanhVienMoi(string? maKh = null, DateTime? ngay = null, IDbTransaction? tx = null)
    {
        var ud = _dal.LayThanhVienMoiDangChay(ngay ?? DateTime.Today, tx).FirstOrDefault();
        if (ud == null || maKh == null) return ud;
        var kh = _khDal.LayTheoMa(maKh, tx);
        if (kh?.MaTk == null) return ud;
        ud.DaDung = _donDal.DemDonTaiKhoan(maKh, tx);
        ud.ConLai = Math.Max(0, ud.SoDonDau - ud.DaDung.Value);
        return ud;
    }

    // Tinh tien cho 1 danh sach dong hang tai ngay chi dinh. Dung chung cho gio hang, dat hang, ban tai quay.
    //   1. Gia tung dong = don gia - % khuyen mai san pham lon nhat dang chay
    //   2. Giam tren ca hoa don: neu khach nhap voucher hop le thi dung voucher,
    //      khong thi lay muc khuyen mai hoa don cao nhat ma tam tinh dat duoc. Khong cong don 2 loai.
    //   3. (v3) maKhThanhVien: khach co tai khoan con luot uu dai thanh vien moi -> neu muc nay giam nhieu hon
    //      muc o buoc 2 thi dung muc nay thay the (van khong cong don).
    public BangTinhTienDTO TinhTien(List<DongHangDTO> dongHangs, DateTime ngay, string? maVoucher,
        IDbTransaction? tx = null, bool kiemTraTon = true, string? maKhThanhVien = null)
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
            bang.LoaiGiam = "VOUCHER";
            bang.MaVoucher = v.MaVoucher;
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
                bang.LoaiGiam = "HOA_DON";
            }
        }
        if (maKhThanhVien != null && LayUuDaiThanhVienMoi(maKhThanhVien, ngay, tx) is { ConLai: > 0 } ud)
        {
            var giam = Math.Round(bang.TamTinh * ud.PhanTramGiamGia / 100, 0);
            if (ud.GiamToiDa is { } toiDa) giam = Math.Min(giam, toiDa);
            if (giam > bang.TienGiam)
            {
                var luot = ud.DaDung!.Value + 1;
                bang.GhiChuKm = $"Ưu đãi thành viên mới giảm {ud.PhanTramGiamGia:0.##}%"
                    + (ud.GiamToiDa != null ? $" (tối đa {ud.GiamToiDa:N0}đ)" : "") + $", đơn thứ {luot}/{ud.SoDonDau}"
                    + (maVoucher != null ? $". Mức này có lợi hơn mã {maVoucher} nên được áp dụng" : "");
                bang.TienGiam = giam;
                bang.MaKm = ud.MaKm;
                bang.LoaiGiam = "THANH_VIEN_MOI";
                bang.MaVoucher = null;
            }
        }
        bang.MaKm ??= maKmSanPham;
        bang.TongTien = bang.TamTinh - bang.TienGiam;
        return bang;
    }

    internal static string TenDayDu(SanPhamDTO s) =>
        s.TenSp + (s.MauSac != null ? $" - {s.MauSac}" : "") + (s.KichCo != null ? $" - {s.KichCo}" : "");
}
