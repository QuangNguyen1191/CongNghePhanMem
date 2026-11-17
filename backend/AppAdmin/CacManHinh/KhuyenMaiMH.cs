using AppAdmin.CacHopThoai;

namespace AppAdmin.CacManHinh;

public sealed class KhuyenMaiMH : ManHinh
{
    private sealed record DongCt(string ApDung, string ChiTiet, string GiamGia);

    private readonly ComboBox _hinhThuc = GiaoDien.Chon(200);
    private readonly ComboBox _tinhTrang = GiaoDien.Chon(150);
    private readonly Luoi<KhuyenMaiDTO> _luoi = new() { Khoa = k => k.MaKm };
    private readonly Luoi<DongCt> _ct = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly HenGio _hen = new();
    private readonly Button _ketThuc;
    private List<KhuyenMaiDTO> _tatCa = new();
    private List<SanPhamDTO>? _sanPhams;
    private KhuyenMaiDTO? _dangXem;
    private bool _dangNap;

    public KhuyenMaiMH() : base("Khuyến mãi",
        "4 hình thức: giảm theo sản phẩm, theo tổng hóa đơn, mã voucher, ưu đãi thành viên mới. Không cộng dồn: hệ thống chọn mức có lợi nhất.")
    {
        Nut("Thêm chương trình", Keys.Control | Keys.N, Them, chinh: true);
        Nut("Sửa", Keys.F2, Sua);
        Phim("Sửa", Keys.Return, Sua);
        _ketThuc = Nut("Kết thúc ngay", Keys.Delete, KetThuc, nguyHiem: true);
        KhoangCach();
        Nut("Làm mới", Keys.F5, LamMoi);

        Muc.Nap(_hinhThuc, Nhan.CacMuc(Nhan.HinhThucKm));
        Muc.Nap(_tinhTrang, new[] { new Muc(null, "Tất cả"), new Muc("DANG_CHAY", "Đang chạy"), new Muc("SAP_TOI", "Sắp diễn ra"), new Muc("DA_KET_THUC", "Đã kết thúc") });
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { GiaoDien.Nhan("Hình thức"), _hinhThuc, GiaoDien.Nhan("Tình trạng"), _tinhTrang });
        _hinhThuc.SelectedIndexChanged += (_, _) => Loc();
        _tinhTrang.SelectedIndexChanged += (_, _) => { if (!_dangNap) Loc(); };

        _luoi.Them("Mã KM", k => k.MaKm, 70).Them("Tên chương trình", k => k.TenKm, 220, fill: true)
            .Them("Hình thức", k => Nhan.Ten(Nhan.HinhThucKm, k.HinhThuc), 170)
            .Them("% giảm", k => k.PhanTramGiamGia, 70, "0.##", canPhai: true).Ngay("Bắt đầu", k => k.NgayBd).Ngay("Kết thúc", k => k.NgayKt)
            .Them("Tình trạng", k => TenTinhTrang(k), 105, mau: k => TinhTrang(k) switch
            {
                "DANG_CHAY" => GiaoDien.XanhLa, "SAP_TOI" => GiaoDien.XanhDuong, _ => GiaoDien.Xam
            });
        _luoi.MoDong += _ => Sua();
        _ct.Them("Áp dụng", d => d.ApDung, 120).Them("Chi tiết", d => d.ChiTiet, 200, fill: true).Them("Giảm", d => d.GiamGia, 110, canPhai: true);
        _chiTiet = new KhungChiTiet("Chi tiết khuyến mãi", _ct.Grid, "Mức giảm");

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 60));
        _luoi.ChonThayDoi += HienChiTiet;
    }

    private static string TinhTrang(KhuyenMaiDTO k) =>
        DateTime.Today < k.NgayBd.Date ? "SAP_TOI" : DateTime.Today > k.NgayKt.Date ? "DA_KET_THUC" : "DANG_CHAY";

    private static string TenTinhTrang(KhuyenMaiDTO k) => TinhTrang(k) switch
    {
        "SAP_TOI" => "Sắp diễn ra", "DA_KET_THUC" => "Đã kết thúc", _ => "Đang chạy"
    };

    protected override void NhanThamSo(string thamSo)
    {
        _dangNap = true;
        Muc.Nap(_tinhTrang, _tinhTrang.Items.Cast<Muc>().ToList(), thamSo);
        _dangNap = false;
    }

    public override void LamMoi()
    {
        _sanPhams = null;
        if (!ThaoTac.Chay("Tải khuyến mãi", () => new KhuyenMaiBLL().LayTatCa(), out var ds)) return;
        _tatCa = ds;
        Loc();
    }

    private void Loc()
    {
        var ht = Muc.Lay(_hinhThuc);
        var tt = Muc.Lay(_tinhTrang);
        _luoi.Nap(_tatCa.Where(k => (ht == null || k.HinhThuc == ht) && (tt == null || TinhTrang(k) == tt)));
    }

    private List<SanPhamDTO>? SanPhams()
    {
        if (_sanPhams == null && ThaoTac.Chay("Tải sản phẩm", () => new SanPhamBLL().LayTatCa(), out var ds)) _sanPhams = ds;
        return _sanPhams;
    }

    private void HienChiTiet(KhuyenMaiDTO? k)
    {
        _dangXem = null;
        _ketThuc.Enabled = k != null && TinhTrang(k) != "DA_KET_THUC";
        if (k == null)
        {
            _chiTiet.ThongTin.Xoa();
            _ct.Nap(Array.Empty<DongCt>());
            return;
        }
        _chiTiet.TieuDe.Text = $"{k.MaKm} · {k.TenKm}";
        _chiTiet.ThongTin.Dat(("Hình thức", Nhan.Ten(Nhan.HinhThucKm, k.HinhThuc), null),
            ("Thời gian", $"{k.NgayBd:dd/MM/yyyy} - {k.NgayKt:dd/MM/yyyy}", null),
            ("Tình trạng", TenTinhTrang(k), TinhTrang(k) == "DANG_CHAY" ? GiaoDien.XanhLa : GiaoDien.ChuPhu));
        var ma = k.MaKm;
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaKm != ma || !ThaoTac.Chay("Chi tiết khuyến mãi", () => new KhuyenMaiBLL().LayTheoMa(ma), out var km)) return;
            _dangXem = km;
            var ds = new List<DongCt>();
            if (km.HinhThuc == "SAN_PHAM")
            {
                var sp = SanPhams()?.ToDictionary(s => s.MaSp) ?? new Dictionary<string, SanPhamDTO>();
                ds.AddRange(km.SanPhams.Select(c => new DongCt(c.MaSp,
                    sp.TryGetValue(c.MaSp, out var s) ? $"{s.TenSp} - {s.MauSac} - {s.KichCo} ({GiaoDien.Tien(s.DonGia)})" : "",
                    $"{c.PhanTramGiamGia:0.##}%")));
            }
            ds.AddRange(km.MucHoaDons.OrderBy(m => m.MucApDungHoaDon)
                .Select(m => new DongCt("Hóa đơn", $"Tạm tính từ {GiaoDien.Tien(m.MucApDungHoaDon)}", $"{m.PhanTramGiamGia:0.##}%")));
            ds.AddRange(km.Vouchers.Select(v => new DongCt("Voucher", v.MaVoucher, $"{v.PhanTramGiamGia:0.##}%")));
            if (km.ThanhVienMoi is { } tv)
                ds.Add(new DongCt("Thành viên mới", $"{tv.SoDonDau} đơn đầu đặt bằng tài khoản" + (tv.GiamToiDa != null ? $", tối đa {GiaoDien.Tien(tv.GiamToiDa)} / đơn" : ""),
                    $"{tv.PhanTramGiamGia:0.##}%"));
            _ct.Nap(ds);
        });
    }

    private void Them()
    {
        if (SanPhams() is not { } sps) return;
        using var f = new FormKhuyenMai(null, sps);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void Sua()
    {
        if (_luoi.Chon is not { } k) return;
        var km = _dangXem?.MaKm == k.MaKm ? _dangXem : null;
        if (km == null && !ThaoTac.Chay("Chi tiết khuyến mãi", () => new KhuyenMaiBLL().LayTheoMa(k.MaKm), out km)) return;
        if (SanPhams() is not { } sps) return;
        using var f = new FormKhuyenMai(km, sps);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void KetThuc()
    {
        if (_luoi.Chon is not { } k) return;
        if (!HopThoai.Hoi($"Kết thúc \"{k.TenKm}\" ngay?\nKhuyến mãi không còn áp dụng từ hôm nay (đơn / hóa đơn cũ giữ nguyên mức giảm).")) return;
        if (ThaoTac.Chay("Kết thúc khuyến mãi", () => new KhuyenMaiBLL().KetThuc(k.MaKm))) LamMoi();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}
