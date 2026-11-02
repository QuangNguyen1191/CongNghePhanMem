using System.Diagnostics;
using DAL;
using DTO;

namespace BLL;

// Yeu cau 3 + 5: moi bao cao deu tinh duoc theo 2 cach
//   TAI_SU_DUNG : dung lai danh sach hoa don / chi tiet / khoan chi da tai san (1 lan), gom nhom bang LINQ trong C#
//   TRUY_VAN_MOI: gui 1 cau GROUP BY moi xuong SQL Server, chi nhan ve ket qua da tong hop
// Ket qua 2 cach phai giong nhau; BangThongKeDTO ghi lai thoi gian chay va so cau SQL de so sanh.
public class ThongKeBLL
{
    public const string TaiSuDung = "TAI_SU_DUNG";
    public const string TruyVanMoi = "TRUY_VAN_MOI";
    public const string TongCong = "Tổng cộng";

    private readonly ThongKeDAL _dal = new();

    // ------------------------------------------------------------------ bo nho dem cho cach tai su dung
    private sealed class DuLieuTho
    {
        public List<HoaDonDTO> HoaDons = new();
        public List<ChiTietHoaDonDTO> ChiTiets = new();
        public List<(DateTime Ngay, decimal Tien)> KhoanChis = new();
        public DateTime LucTai;
    }

    private static DuLieuTho? _dem;
    private static readonly object _khoa = new();
    public static TimeSpan ThoiHanBoNhoDem { get; set; } = TimeSpan.FromMinutes(5);

    // Nut "Lam moi" tren man hinh thong ke, hoac goi sau khi co hoa don / phieu nhap moi
    public static void XoaBoNhoDem()
    {
        lock (_khoa) _dem = null;
    }

    public DateTime? LucTaiDuLieuTaiSuDung => _dem?.LucTai;

    private DuLieuTho LayDuLieuTho()
    {
        lock (_khoa)
        {
            if (_dem != null && DateTime.Now - _dem.LucTai < ThoiHanBoNhoDem) return _dem;
            var hdDal = new HoaDonDAL();
            _dem = new DuLieuTho
            {
                HoaDons = hdDal.LayDaThanhToan(),
                ChiTiets = hdDal.LayChiTietDaThanhToan(),
                KhoanChis = _dal.LayKhoanChi(),
                LucTai = DateTime.Now
            };
            return _dem;
        }
    }

    // ------------------------------------------------------------------ 5a. Tong tien tu ngay A den ngay B
    public BangThongKeDTO TongTienKhoang(DateTime tuNgay, DateTime denNgay, string cach)
    {
        tuNgay = tuNgay.Date;
        denNgay = denNgay.Date;
        QuyTac.KiemTra(tuNgay <= denNgay, "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        return Do($"Tổng tiền hóa đơn từ {tuNgay:dd/MM/yyyy} đến {denNgay:dd/MM/yyyy}", cach, () =>
        {
            int so; decimal tong;
            if (cach == TaiSuDung)
            {
                var ds = LayDuLieuTho().HoaDons.Where(h => h.NgayLap.Date >= tuNgay && h.NgayLap.Date <= denNgay).ToList();
                so = ds.Count;
                tong = ds.Sum(h => h.TongTien);
            }
            else (so, tong) = _dal.TongTienKhoang(tuNgay, denNgay);
            return (new List<string> { "Số hóa đơn", "Tổng tiền" },
                new List<DongThongKeDTO> { new() { Nhan = $"{tuNgay:dd/MM/yyyy} - {denNgay:dd/MM/yyyy}", GiaTris = { so, tong } } });
        });
    }

    // ------------------------------------------------------------------ 5b. Thu - chi - loi nhuan theo thang / quy / nam
    // kieu: THANG | QUY | NAM. nam = null: lay het cac nam
    public BangThongKeDTO ThuChiLoiNhuan(string kieu, int? nam, string cach)
    {
        QuyTac.KiemTra(kieu is "THANG" or "QUY" or "NAM", "Kiểu kỳ phải là THANG, QUY hoặc NAM.");
        var tenKieu = kieu switch { "THANG" => "tháng", "QUY" => "quý", _ => "năm" };
        return Do($"Thu - chi - lợi nhuận theo {tenKieu}" + (nam != null ? $" năm {nam}" : ""), cach, () =>
        {
            List<(int Nam, int Ky, decimal Tien)> thu, chi;
            if (cach == TaiSuDung)
            {
                var d = LayDuLieuTho();
                thu = d.HoaDons.Where(h => nam == null || h.NgayLap.Year == nam)
                    .GroupBy(h => (h.NgayLap.Year, Ky(kieu, h.NgayLap)))
                    .Select(g => (g.Key.Year, g.Key.Item2, g.Sum(h => h.TongTien))).ToList();
                chi = d.KhoanChis.Where(c => nam == null || c.Ngay.Year == nam)
                    .GroupBy(c => (c.Ngay.Year, Ky(kieu, c.Ngay)))
                    .Select(g => (g.Key.Year, g.Key.Item2, g.Sum(c => c.Tien))).ToList();
            }
            else
            {
                thu = _dal.ThuTheoKy(kieu, nam).Select(x => (x.Nam, x.Ky, x.Tien)).ToList();
                chi = _dal.ChiTheoKy(kieu, nam).Select(x => (x.Nam, x.Ky, x.Tien)).ToList();
            }

            var cacKy = thu.Select(x => (x.Nam, x.Ky)).Union(chi.Select(x => (x.Nam, x.Ky)))
                .OrderBy(x => x.Nam).ThenBy(x => x.Ky).ToList();
            var dongs = cacKy.Select(k =>
            {
                var t = thu.Where(x => x.Nam == k.Nam && x.Ky == k.Ky).Sum(x => x.Tien);
                var c = chi.Where(x => x.Nam == k.Nam && x.Ky == k.Ky).Sum(x => x.Tien);
                return new DongThongKeDTO { Nhan = NhanKy(kieu, k.Nam, k.Ky), GiaTris = { t, c, t - c } };
            }).ToList();
            ThemDongTong(dongs);
            return (new List<string> { "Tổng thu", "Tổng chi", "Lợi nhuận" }, dongs);
        });
    }

    // Ban dang danh sach ThuChiDTO cho bieu do
    public List<ThuChiDTO> ThuChiLoiNhuanDs(string kieu, int? nam, string cach) =>
        ThuChiLoiNhuan(kieu, nam, cach).Dongs.Where(d => d.Nhan != TongCong)
            .Select(d => new ThuChiDTO { Ky = d.Nhan, TongThu = d.GiaTris[0], TongChi = d.GiaTris[1] }).ToList();

    // ------------------------------------------------------------------ 5c. Theo nhan vien x nam (bang pivot)
    public BangThongKeDTO TheoNhanVienNam(string cach) =>
        Do("Doanh thu theo nhân viên và năm", cach, () =>
        {
            List<ThongKeDAL.DongGom> ds = cach == TaiSuDung
                ? LayDuLieuTho().HoaDons.GroupBy(h => (h.NgayLap.Year, h.MaNv, h.TenNhanVien))
                    .Select(g => new ThongKeDAL.DongGom
                    {
                        Nam = g.Key.Year, Ma = g.Key.MaNv, Ten = g.Key.TenNhanVien ?? "", Tien = g.Sum(h => h.TongTien)
                    }).ToList()
                : _dal.TheoNhanVienNam();
            return XoayBang(ds, d => d.Nam.ToString());
        });

    // ------------------------------------------------------------------ 5d. Theo khach hang x quy (1 nam) hoac x nam
    public BangThongKeDTO TheoKhachHang(int? nam, string cach) =>
        Do(nam != null ? $"Doanh thu theo khách hàng và quý năm {nam}" : "Doanh thu theo khách hàng và năm", cach, () =>
        {
            List<ThongKeDAL.DongGom> ds = cach == TaiSuDung
                ? LayDuLieuTho().HoaDons.Where(h => nam == null || h.NgayLap.Year == nam)
                    .GroupBy(h => (h.NgayLap.Year, Ky: nam == null ? 0 : Ky("QUY", h.NgayLap), Ma: h.MaKh ?? "",
                        Ten: h.MaKh == null ? "Khách vãng lai" : h.TenKhachHang ?? ""))
                    .Select(g => new ThongKeDAL.DongGom
                    {
                        Nam = g.Key.Year, Ky = g.Key.Ky, Ma = g.Key.Ma, Ten = g.Key.Ten, Tien = g.Sum(h => h.TongTien)
                    }).ToList()
                : _dal.TheoKhachHang(nam);
            return nam != null ? XoayBang(ds, d => $"Quý {d.Ky}", CacQuy) : XoayBang(ds, d => d.Nam.ToString());
        });

    // ------------------------------------------------------------------ 5e. Theo san pham x quy trong 1 nam
    public BangThongKeDTO TheoSanPhamQuy(int nam, string cach) =>
        Do($"Doanh thu theo sản phẩm và quý năm {nam}", cach, () =>
        {
            List<ThongKeDAL.DongGom> ds;
            if (cach == TaiSuDung)
            {
                var d = LayDuLieuTho();
                var ngayCuaHd = d.HoaDons.Where(h => h.NgayLap.Year == nam).ToDictionary(h => h.MaHoaDon, h => h.NgayLap);
                ds = d.ChiTiets.Where(c => ngayCuaHd.ContainsKey(c.MaHoaDon))
                    .GroupBy(c => (Ky: Ky("QUY", ngayCuaHd[c.MaHoaDon]), c.MaSp, Ten: TenSp(c.TenSp, c.MauSac, c.KichCo)))
                    .Select(g => new ThongKeDAL.DongGom
                    {
                        Nam = nam, Ky = g.Key.Ky, Ma = g.Key.MaSp, Ten = g.Key.Ten, Tien = g.Sum(c => c.ThanhTien)
                    }).ToList();
            }
            else ds = _dal.TheoSanPhamQuy(nam);
            return XoayBang(ds, x => $"Quý {x.Ky}", CacQuy);
        });

    public List<int> CacNamCoDuLieu() => _dal.CacNamCoDuLieu();

    // ------------------------------------------------------------------ ham phu
    private static readonly List<string> CacQuy = new() { "Quý 1", "Quý 2", "Quý 3", "Quý 4" };

    private static BangThongKeDTO Do(string tieuDe, string cach,
        Func<(List<string> Cots, List<DongThongKeDTO> Dongs)> tinh)
    {
        QuyTac.KiemTra(cach is TaiSuDung or TruyVanMoi, "Cách tính phải là TAI_SU_DUNG hoặc TRUY_VAN_MOI.");
        using var dem = NhatKySql.BatDauDem();
        var dongHo = Stopwatch.StartNew();
        var (cots, dongs) = tinh();
        dongHo.Stop();
        return new BangThongKeDTO
        {
            TieuDe = tieuDe, CachTinh = cach, Cots = cots, Dongs = dongs,
            ThoiGianMs = dongHo.ElapsedMilliseconds, SoCauTruyVan = dem.SoCau
        };
    }

    // Bien danh sach (ma, ten, cot, tien) thanh bang: moi dong 1 doi tuong, moi cot 1 ky, them cot + dong Tong cong
    private static (List<string>, List<DongThongKeDTO>) XoayBang(List<ThongKeDAL.DongGom> ds,
        Func<ThongKeDAL.DongGom, string> tenCot, List<string>? cotCoDinh = null)
    {
        var cots = cotCoDinh ?? ds.Select(tenCot).Distinct().OrderBy(c => c).ToList();
        var dongs = ds.GroupBy(d => (d.Ma, d.Ten))
            .Select(g =>
            {
                var giaTris = cots.Select(c => g.Where(d => tenCot(d) == c).Sum(d => d.Tien)).ToList();
                giaTris.Add(giaTris.Sum());
                return new DongThongKeDTO { Nhan = g.Key.Ma == "" ? g.Key.Ten : $"{g.Key.Ma} - {g.Key.Ten}", GiaTris = giaTris };
            })
            .OrderByDescending(d => d.GiaTris[^1]).ThenBy(d => d.Nhan).ToList();
        ThemDongTong(dongs);
        return (cots.Append(TongCong).ToList(), dongs);
    }

    private static void ThemDongTong(List<DongThongKeDTO> dongs)
    {
        if (dongs.Count == 0) return;
        var soCot = dongs[0].GiaTris.Count;
        dongs.Add(new DongThongKeDTO
        {
            Nhan = TongCong,
            GiaTris = Enumerable.Range(0, soCot).Select(i => dongs.Sum(d => d.GiaTris[i])).ToList()
        });
    }

    private static int Ky(string kieu, DateTime ngay) => kieu switch
    {
        "THANG" => ngay.Month,
        "QUY" => (ngay.Month - 1) / 3 + 1,
        _ => 0
    };

    private static string NhanKy(string kieu, int nam, int ky) => kieu switch
    {
        "THANG" => $"{ky:00}/{nam}",
        "QUY" => $"Quý {ky}/{nam}",
        _ => nam.ToString()
    };

    private static string TenSp(string? ten, string? mau, string? co) =>
        (ten ?? "") + (mau != null ? " - " + mau : "") + (co != null ? " - " + co : "");
}
