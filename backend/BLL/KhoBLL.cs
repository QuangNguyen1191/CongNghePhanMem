using DAL;
using DTO;

namespace BLL;

public class PhieuNhapBLL
{
    private readonly PhieuNhapDAL _dal = new();
    private readonly NhaCungCapDAL _nccDal = new();
    private readonly SanPhamDAL _spDal = new();

    // Lap phieu nhap: cong ton kho tung dong, tinh tong - trong 1 giao dich
    public PhieuNhapDTO Lap(string maNcc, string maNv, List<ChiTietPhieuNhapDTO> dongs)
    {
        QuyTac.KiemTra(dongs.Count > 0, "Phiếu nhập chưa có sản phẩm nào");
        var gop = dongs.GroupBy(d => (d.MaSp.Trim().ToUpper(), d.DonGiaNhap))
            .Select(g => new ChiTietPhieuNhapDTO { MaSp = g.Key.Item1, DonGiaNhap = g.Key.DonGiaNhap, SoLuong = g.Sum(d => d.SoLuong) })
            .ToList();
        QuyTac.KiemTra(gop.Select(d => d.MaSp).Distinct().Count() == gop.Count, "Một sản phẩm không được có 2 đơn giá nhập trong cùng phiếu");

        var ma = DbHelper.GiaoDich(tx =>
        {
            var ncc = QuyTac.CanCo(_nccDal.LayTheoMa(maNcc, tx), "Không tìm thấy nhà cung cấp");
            QuyTac.KiemTra(ncc.TrangThai == "HOP_TAC", "Nhà cung cấp đã ngừng hợp tác");
            var p = new PhieuNhapDTO
            {
                MaPhieuNhap = DbHelper.SinhMa("PHIEU_NHAP", "ma_phieu_nhap", "PN", 5, tx), MaNcc = maNcc, MaNv = maNv,
                NgayNhap = DateTime.Today
            };
            foreach (var d in gop)
            {
                QuyTac.KiemTra(d.SoLuong > 0, "Số lượng nhập phải lớn hơn 0");
                QuyTac.KiemTra(d.DonGiaNhap > 0, "Đơn giá nhập phải lớn hơn 0");
                QuyTac.CanCo(_spDal.LayTheoMa(d.MaSp, tx), $"Không tìm thấy sản phẩm {d.MaSp}");
                d.MaPhieuNhap = p.MaPhieuNhap;
                d.ThanhTien = d.SoLuong * d.DonGiaNhap;
                p.ChiTiets.Add(d);
            }
            p.TongTien = p.ChiTiets.Sum(d => d.ThanhTien);
            _dal.Them(p, tx);
            foreach (var d in p.ChiTiets) _spDal.CapNhatTon(d.MaSp, d.SoLuong, tx);
            return p.MaPhieuNhap;
        });
        ThongKeBLL.XoaBoNhoDem();
        return LayTheoMa(ma);
    }

    public PhieuNhapDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy phiếu nhập");

    public List<ChiTietPhieuNhapDTO> LayChiTiet(string ma) => _dal.LayChiTiet(ma);

    public List<PhieuNhapDTO> LayDanhSach(DateTime? tuNgay = null, DateTime? denNgay = null, string? maNcc = null) =>
        _dal.LayDanhSach(tuNgay, denNgay, QuyTac.RongThanhNull(maNcc));
}

public class DoiTraBLL
{
    private readonly DoiTraDAL _dal = new();
    private readonly HoaDonDAL _hdDal = new();
    private readonly SanPhamDAL _spDal = new();

    // Gui yeu cau doi / tra. maKhGui != null: khach gui tu web (chi duoc doi tra hoa don cua minh).
    public DoiTraDTO GuiYeuCau(string maHoaDon, string loai, string? lyDo, List<ChiTietDoiTraDTO> dongs, string? maKhGui)
    {
        QuyTac.KiemTra(loai is "DOI" or "TRA", "Loại phải là DOI hoặc TRA");
        QuyTac.KiemTra(dongs.Count > 0 && dongs.All(d => d.SoLuong > 0), "Chọn sản phẩm và số lượng cần đổi / trả");
        QuyTac.KiemTra(dongs.Select(d => d.MaSp).Distinct().Count() == dongs.Count, "Sản phẩm bị lặp");

        var ma = DbHelper.GiaoDich(tx =>
        {
            var hd = QuyTac.CanCo(_hdDal.LayTheoMa(maHoaDon, tx), "Không tìm thấy hóa đơn");
            QuyTac.KiemTra(hd.TrangThai == "DA_THANH_TOAN", "Chỉ đổi trả được hóa đơn đã thanh toán");
            if (maKhGui != null) QuyTac.KiemTra(hd.MaKh == maKhGui, "Hóa đơn không phải của bạn");
            QuyTac.KiemTra((DateTime.Today - hd.NgayLap.Date).TotalDays <= QuyTac.SoNgayDoiTra,
                $"Đã quá {QuyTac.SoNgayDoiTra} ngày kể từ ngày mua");

            foreach (var d in dongs)
            {
                var dongHd = hd.ChiTiets.FirstOrDefault(c => c.MaSp == d.MaSp);
                QuyTac.KiemTra(dongHd != null, $"Sản phẩm {d.MaSp} không có trong hóa đơn");
                var conLai = dongHd!.SoLuong - _dal.SoLuongDaDoiTra(maHoaDon, d.MaSp, tx);
                QuyTac.KiemTra(d.SoLuong <= conLai, $"{dongHd.TenSp}: chỉ còn {conLai} cái được đổi / trả");
                if (loai == "DOI")
                {
                    var spMoi = QuyTac.CanCo(_spDal.LayTheoMa(d.MaSpDoi ?? "", tx), "Chọn sản phẩm muốn đổi sang");
                    QuyTac.KiemTra(spMoi.TenSp == dongHd.TenSp && spMoi.MaSp != d.MaSp,
                        "Chỉ đổi sang size / màu khác của cùng mẫu sản phẩm");
                    QuyTac.KiemTra(spMoi.TrangThai == "DANG_BAN", "Sản phẩm muốn đổi đã ngừng bán");
                }
                else d.MaSpDoi = null;
            }

            var dt = new DoiTraDTO
            {
                MaDoiTra = DbHelper.SinhMa("DOI_TRA_HANG", "ma_doi_tra", "DT", 5, tx), MaHoaDon = maHoaDon, Loai = loai,
                LyDo = QuyTac.RongThanhNull(lyDo), NgayDoiTra = DateTime.Today, ChiTiets = dongs
            };
            dt.ChiTiets.ForEach(c => c.MaDoiTra = dt.MaDoiTra);
            _dal.Them(dt, tx);
            return dt.MaDoiTra;
        });
        return LayTheoMa(ma);
    }

    public DoiTraDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy phiếu đổi trả");

    public List<DoiTraDTO> LayDanhSach(string? trangThai = null, string? maKh = null) =>
        _dal.LayDanhSach(QuyTac.RongThanhNull(trangThai), QuyTac.RongThanhNull(maKh));

    // Duyet: dong y cho doi / tra, cho khach mang hang toi
    public void Duyet(string ma, string maNv) =>
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.DoiTrangThai(ma, "CHO_DUYET", "DA_DUYET", maNv, 0, null, tx)),
            "Chỉ duyệt được phiếu đang chờ duyệt");

    public void TuChoi(string ma, string maNv) =>
        QuyTac.KiemTra(DbHelper.GiaoDich(tx => _dal.DoiTrangThai(ma, "CHO_DUYET", "TU_CHOI", maNv, 0, null, tx)),
            "Chỉ từ chối được phiếu đang chờ duyệt");

    // Hoan tat: nhan hang ve kho; DOI thi xuat SP moi, TRA thi hoan tien theo gia thuc tra (da tru giam gia hoa don)
    public DoiTraDTO HoanTat(string ma, string maNv)
    {
        DbHelper.GiaoDich(tx =>
        {
            var dt = QuyTac.CanCo(_dal.LayTheoMa(ma, tx), "Không tìm thấy phiếu đổi trả");
            QuyTac.KiemTra(dt.TrangThai is "CHO_DUYET" or "DA_DUYET", "Phiếu đã được xử lý");
            var hd = _hdDal.LayTheoMa(dt.MaHoaDon, tx)!;
            var tyLeThucTra = hd.TongTien / (hd.TongTien + hd.TienGiam);
            decimal tienHoan = 0;
            foreach (var c in dt.ChiTiets)
            {
                _spDal.CapNhatTon(c.MaSp, c.SoLuong, tx);
                if (dt.Loai == "DOI")
                    QuyTac.KiemTra(_spDal.CapNhatTon(c.MaSpDoi!, -c.SoLuong, tx), $"{c.TenSpDoi} không đủ hàng để đổi");
                else
                    tienHoan += Math.Round(hd.ChiTiets.First(h => h.MaSp == c.MaSp).DonGiaBan * c.SoLuong * tyLeThucTra, 0);
            }
            QuyTac.KiemTra(_dal.DoiTrangThai(ma, dt.TrangThai, "HOAN_TAT", maNv, tienHoan,
                dt.Loai == "TRA" ? DateTime.Today : null, tx), "Phiếu vừa được người khác cập nhật");
        });
        ThongKeBLL.XoaBoNhoDem();
        return LayTheoMa(ma);
    }
}
