using DAL;
using DTO;

namespace BLL;

public class LoaiSpBLL
{
    private readonly LoaiSpDAL _dal = new();

    public List<LoaiSpDTO> LayTatCa() => _dal.LayTatCa();

    public LoaiSpDTO Them(string ten)
    {
        ten = ten.Trim();
        QuyTac.KiemTra(ten.Length > 0, "Vui lòng nhập tên loại sản phẩm");
        QuyTac.KiemTra(!_dal.TrungTen(ten), "Tên loại sản phẩm đã tồn tại");
        return DbHelper.GiaoDich(tx =>
        {
            var l = new LoaiSpDTO { MaLoaiSp = DbHelper.SinhMa("LOAI_SP", "ma_loai_sp", "LSP", 2, tx), TenLoaiSp = ten };
            _dal.Them(l, tx);
            return l;
        });
    }

    public void Sua(LoaiSpDTO l)
    {
        l.TenLoaiSp = l.TenLoaiSp.Trim();
        QuyTac.KiemTra(l.TenLoaiSp.Length > 0, "Vui lòng nhập tên loại sản phẩm");
        QuyTac.KiemTra(!_dal.TrungTen(l.TenLoaiSp, l.MaLoaiSp), "Tên loại sản phẩm đã tồn tại");
        QuyTac.CanTimThay(_dal.Sua(l) == 1, "Không tìm thấy loại sản phẩm");
    }

    public void Xoa(string ma)
    {
        QuyTac.KiemTra(_dal.DemSanPham(ma) == 0, "Loại này còn sản phẩm, không xóa được");
        QuyTac.CanTimThay(_dal.Xoa(ma) == 1, "Không tìm thấy loại sản phẩm");
    }
}

public class SanPhamBLL
{
    private readonly SanPhamDAL _dal = new();
    private readonly LoaiSpDAL _loaiDal = new();
    private readonly KhuyenMaiDAL _kmDal = new();

    // Che do "tai su dung": giao dien goi 1 lan, sau do tu loc tren danh sach nay moi lan go phim
    public List<SanPhamDTO> LayTatCa(bool chiDangBan = false) => _dal.LayTatCa(chiDangBan);

    // Che do "truy van moi": moi lan go tu khoa la 1 cau SELECT ... LIKE moi
    public List<SanPhamDTO> TimKiem(string tuKhoa, string? maLoaiSp = null, bool chiDangBan = false) =>
        _dal.TimTheoTuKhoa(tuKhoa.Trim(), QuyTac.RongThanhNull(maLoaiSp), chiDangBan);

    public SanPhamDTO LayTheoMa(string ma) => QuyTac.CanCo(_dal.LayTheoMa(ma), "Không tìm thấy sản phẩm");

    public List<SanPhamDTO> LayCacBienThe(string ma) => _dal.LayCungTen(LayTheoMa(ma).TenSp);

    public List<SanPhamDTO> LayTonThap() => _dal.LayTonThap(QuyTac.NguongTonThap);

    // Gia ban hom nay sau khuyen mai san pham (ma SP -> gia)
    public Dictionary<string, decimal> GiaKhuyenMaiHomNay()
    {
        var giam = _kmDal.LayGiamSanPhamDangChay(DateTime.Today)
            .GroupBy(c => c.MaSp).ToDictionary(g => g.Key, g => g.Max(c => c.PhanTramGiamGia));
        return _dal.LayTatCa(true).Where(s => giam.ContainsKey(s.MaSp))
            .ToDictionary(s => s.MaSp, s => Math.Round(s.DonGia * (100 - giam[s.MaSp]) / 100, 0));
    }

    public SanPhamDTO Them(SanPhamDTO s) => ThemNhieu(new List<SanPhamDTO> { s })[0];

    // Tao nhanh cac size - mau cua cung 1 mau san pham, moi bien the 1 ma SP
    public List<SanPhamDTO> ThemNhieu(List<SanPhamDTO> ds)
    {
        foreach (var s in ds) KiemTra(s);
        return DbHelper.GiaoDich(tx =>
        {
            foreach (var s in ds)
            {
                s.MaSp = DbHelper.SinhMa("SAN_PHAM", "ma_sp", "SP", 3, tx);
                s.SoLuong = 0;                     // ton kho chi tang qua phieu nhap
                _dal.Them(s, tx);
            }
            return ds;
        });
    }

    public void Sua(SanPhamDTO s)
    {
        KiemTra(s);
        QuyTac.CanTimThay(_dal.Sua(s) == 1, "Không tìm thấy sản phẩm");
    }

    public void NgungBan(string ma, bool ngung) =>
        QuyTac.CanTimThay(_dal.DoiTrangThai(ma, ngung ? "NGUNG_BAN" : "DANG_BAN") == 1, "Không tìm thấy sản phẩm");

    private void KiemTra(SanPhamDTO s)
    {
        s.TenSp = s.TenSp.Trim();
        QuyTac.KiemTra(s.TenSp.Length > 0, "Vui lòng nhập tên sản phẩm");
        QuyTac.KiemTra(s.DonGia > 0, "Đơn giá phải lớn hơn 0");
        QuyTac.KiemTra(_loaiDal.LayTheoMa(s.MaLoaiSp) != null, "Loại sản phẩm không tồn tại");
        QuyTac.KiemTra(s.TrangThai is "DANG_BAN" or "NGUNG_BAN", "Trạng thái sản phẩm không hợp lệ");
        s.KichCo = QuyTac.RongThanhNull(s.KichCo)?.ToUpper();
        s.MauSac = QuyTac.RongThanhNull(s.MauSac);
    }
}

public class NhaCungCapBLL
{
    private readonly NhaCungCapDAL _dal = new();

    public List<NhaCungCapDTO> LayTatCa() => _dal.LayTatCa();

    public NhaCungCapDTO Them(NhaCungCapDTO n)
    {
        KiemTra(n);
        return DbHelper.GiaoDich(tx =>
        {
            n.MaNcc = DbHelper.SinhMa("NHA_CUNG_CAP", "ma_ncc", "NCC", 2, tx);
            _dal.Them(n, tx);
            return n;
        });
    }

    public void Sua(NhaCungCapDTO n)
    {
        KiemTra(n);
        QuyTac.CanTimThay(_dal.Sua(n) == 1, "Không tìm thấy nhà cung cấp");
    }

    // Da co phieu nhap thi chi chuyen sang ngung hop tac, khong xoa
    public void Xoa(string ma)
    {
        QuyTac.KiemTra(!_dal.CoPhieuNhap(ma), "Nhà cung cấp đã có phiếu nhập, chỉ chuyển sang ngừng hợp tác được");
        QuyTac.CanTimThay(_dal.Xoa(ma) == 1, "Không tìm thấy nhà cung cấp");
    }

    private static void KiemTra(NhaCungCapDTO n)
    {
        n.TenNcc = n.TenNcc.Trim();
        QuyTac.KiemTra(n.TenNcc.Length > 0, "Vui lòng nhập tên nhà cung cấp");
        QuyTac.KiemTra(n.TrangThai is "HOP_TAC" or "NGUNG_HOP_TAC", "Trạng thái không hợp lệ");
        n.MaSoThue = QuyTac.RongThanhNull(n.MaSoThue);
        n.Email = QuyTac.RongThanhNull(n.Email);
    }
}
