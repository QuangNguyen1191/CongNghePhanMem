using AppAdmin.CacHopThoai;

namespace AppAdmin.CacManHinh;

// Nhap hang: danh sach phieu (main) + chi tiet phieu (sub, 1 cau SQL khi chon 1 dong)
public sealed class PhieuNhapMH : ManHinh
{
    private readonly DateTimePicker _tu = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly DateTimePicker _den = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly ComboBox _ncc = GiaoDien.Chon(220);
    private readonly Luoi<PhieuNhapDTO> _luoi = new() { Khoa = p => p.MaPhieuNhap };
    private readonly Luoi<ChiTietPhieuNhapDTO> _dong = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly HenGio _hen = new();
    private List<NhaCungCapDTO> _nccs = new();
    private bool _dangNap;

    public PhieuNhapMH() : base("Nhập hàng", "Phiếu nhập cộng tồn kho ngay khi lưu. Tổng tiền nhập được tính là khoản chi trong báo cáo thu - chi.")
    {
        Nut("Lập phiếu nhập", Keys.Control | Keys.N, Lap, chinh: true);
        Nut("In phiếu (PDF)", Keys.Control | Keys.P, InPdf);
        Nut("Xuất Excel", Keys.Control | Keys.E, () =>
        {
            var ds = _luoi.DuLieu;
            if (ThaoTac.Chay("Xuất Excel phiếu nhập", () => new ExcelBLL().XuatPhieuNhap(ds), out var f)) XuatFile.LuuExcel(FindForm(), f, "PhieuNhap");
        });
        Nut("Làm mới", Keys.F5, LamMoi);

        _tu.Checked = false;
        _den.Checked = false;
        var loc = GiaoDien.HangNgang();
        var ap = GiaoDien.NutPhu("Lọc");
        ap.Click += (_, _) => Tai();
        loc.Controls.AddRange(new Control[] { GiaoDien.Nhan("Từ ngày"), _tu, GiaoDien.Nhan("Đến ngày"), _den, GiaoDien.Nhan("NCC"), _ncc, ap });
        _ncc.SelectedIndexChanged += (_, _) => { if (!_dangNap) Tai(); };

        _luoi.Them("Mã phiếu", p => p.MaPhieuNhap, 90).Ngay("Ngày nhập", p => p.NgayNhap).Them("Nhà cung cấp", p => p.TenNcc, 200, fill: true)
            .Them("Người lập", p => p.TenNhanVien, 150).Tien("Tổng tiền", p => p.TongTien, 130);
        _dong.Them("Mã SP", c => c.MaSp, 70).Them("Sản phẩm", c => $"{c.TenSp} - {c.MauSac} - {c.KichCo}", 200, fill: true)
            .So("SL", c => c.SoLuong, 55).Tien("Giá nhập", c => c.DonGiaNhap, 100).Tien("Thành tiền", c => c.ThanhTien, 110);
        _chiTiet = new KhungChiTiet("Chi tiết phiếu nhập", _dong.Grid, "Các dòng hàng");

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 55));
        _luoi.ChonThayDoi += HienChiTiet;
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Tải nhà cung cấp", () => new NhaCungCapBLL().LayTatCa(), out var nccs)) return;
        _nccs = nccs;
        _dangNap = true;
        var chon = Muc.Lay(_ncc);
        Muc.Nap(_ncc, new[] { new Muc(null, "Tất cả") }.Concat(nccs.Select(n => new Muc(n.MaNcc, n.TenNcc))), chon);
        _dangNap = false;
        Tai();
    }

    private void Tai()
    {
        DateTime? tu = _tu.Checked ? _tu.Value.Date : null, den = _den.Checked ? _den.Value.Date : null;
        var ncc = Muc.Lay(_ncc);
        if (ThaoTac.Chay("Danh sách phiếu nhập", () => new PhieuNhapBLL().LayDanhSach(tu, den, ncc), out var ds)) _luoi.Nap(ds);
    }

    private void HienChiTiet(PhieuNhapDTO? p)
    {
        if (p == null)
        {
            _chiTiet.ThongTin.Xoa();
            _dong.Nap(Array.Empty<ChiTietPhieuNhapDTO>());
            return;
        }
        _chiTiet.TieuDe.Text = $"Phiếu {p.MaPhieuNhap}";
        _chiTiet.ThongTin.Dat(("Ngày nhập", GiaoDien.NgayVn(p.NgayNhap)), ("Nhà cung cấp", $"{p.MaNcc} - {p.TenNcc}"),
            ("Người lập", $"{p.MaNv} - {p.TenNhanVien}"), ("Tổng tiền", GiaoDien.Tien(p.TongTien)));
        var ma = p.MaPhieuNhap;
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaPhieuNhap == ma && ThaoTac.Chay("Chi tiết phiếu nhập", () => new PhieuNhapBLL().LayChiTiet(ma), out var ds))
                _dong.Nap(ds);
        });
    }

    private void Lap()
    {
        if (!ThaoTac.Chay("Chuẩn bị lập phiếu", () => new SanPhamBLL().LayTatCa(), out var sps)) return;
        using var f = new FormLapPhieuNhap(_nccs, sps);
        if (f.ShowDialog(FindForm()) != DialogResult.OK) return;
        Tai();
        if (f.KetQua is { } p) _luoi.ChonDong(x => x.MaPhieuNhap == p.MaPhieuNhap);
    }

    private void InPdf()
    {
        if (_luoi.Chon is not { } p) return;
        var ma = p.MaPhieuNhap;
        ThaoTac.Chay("In phiếu nhập", () => XuatFile.MoPdf(new PdfBLL().PhieuNhap(new PhieuNhapBLL().LayTheoMa(ma)), ma));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}
