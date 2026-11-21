using AppAdmin.CacHopThoai;

namespace AppAdmin.CacManHinh;

// ===================================================================== LOAI SAN PHAM
public sealed class LoaiSpMH : ManHinh
{
    private sealed record Dong(LoaiSpDTO Loai, int SoSku, int SoMau, List<SanPhamDTO> Sps);

    private readonly Luoi<Dong> _luoi = new() { Khoa = d => d.Loai.MaLoaiSp };

    public LoaiSpMH() : base("Loại sản phẩm", "Nhóm sản phẩm hiển thị trên menu web bán hàng. Loại còn sản phẩm thì không xóa được.")
    {
        Nut("Thêm", Keys.Control | Keys.N, Them, chinh: true);
        Nut("Sửa tên", Keys.F2, Sua);
        Phim("Sửa tên", Keys.Return, Sua);
        Nut("Xóa", Keys.Delete, Xoa, nguyHiem: true);
        KhoangCach();
        Nut("Làm mới", Keys.F5, LamMoi);
        _luoi.Them("Mã loại", d => d.Loai.MaLoaiSp, 100).Them("Tên loại sản phẩm", d => d.Loai.TenLoaiSp, 300, fill: true)
            .So("Số mẫu", d => d.SoMau, 90).So("Số SKU (size × màu)", d => d.SoSku, 150);
        _luoi.MoDong += _ => Sua();
        _luoi.XemNhanh = TheLoai;
        NoiDung.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
    }

    // The xem nhanh: cac mau thuoc loai, ton kho, khoang gia (tu danh sach san pham da tai, 0 cau SQL)
    private static TheXemNhanh.NoiDung TheLoai(Dong d)
    {
        var dangBan = d.Sps.Where(s => s.TrangThai == "DANG_BAN").ToList();
        var mau = d.Sps.GroupBy(s => s.TenSp).Select(g => g.Key).ToList();
        var dongs = new List<TheXemNhanh.Dong>
        {
            new("Số mẫu / SKU", $"{d.SoMau} mẫu · {d.SoSku} SKU ({dangBan.Count} đang bán)"),
            new("Tổng tồn kho", $"{d.Sps.Sum(s => s.SoLuong):N0} cái"),
            new("Khoảng giá", d.Sps.Count == 0 ? null : $"{GiaoDien.Tien(d.Sps.Min(s => s.DonGia))} - {GiaoDien.Tien(d.Sps.Max(s => s.DonGia))}"),
            new("Các mẫu", mau.Count == 0 ? "Chưa có sản phẩm" : string.Join("\n", mau.Take(6).Select(t => "• " + t)) + (mau.Count > 6 ? $"\n… và {mau.Count - 6} mẫu khác" : "")),
        };
        return new TheXemNhanh.NoiDung(d.Loai.TenLoaiSp, d.Loai.MaLoaiSp, dongs, GiaoDien.Do, d.SoSku == 0 ? "Trống" : null);
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Tải loại sản phẩm", () => (new LoaiSpBLL().LayTatCa(), new SanPhamBLL().LayTatCa()), out var kq)) return;
        var (loais, sps) = kq;
        _luoi.Nap(loais.Select(l =>
        {
            var cua = sps.Where(s => s.MaLoaiSp == l.MaLoaiSp).ToList();
            return new Dong(l, cua.Count, cua.Select(s => s.TenSp).Distinct().Count(), cua);
        }));
    }

    private void Them()
    {
        var ten = HopThoai.NhapChuoi(FindForm(), "Thêm loại sản phẩm", "Tên loại", true, nhieuDong: false, toiDa: 100);
        if (ten == null) return;
        if (!ThaoTac.Chay("Thêm loại sản phẩm", () => new LoaiSpBLL().Them(ten), out var moi)) return;
        LamMoi();
        _luoi.ChonDong(d => d.Loai.MaLoaiSp == moi.MaLoaiSp);
    }

    private void Sua()
    {
        if (_luoi.Chon is not { } d) return;
        var ten = HopThoai.NhapChuoi(FindForm(), $"Sửa loại {d.Loai.MaLoaiSp}", "Tên loại", true, d.Loai.TenLoaiSp, nhieuDong: false, toiDa: 100);
        if (ten == null || ten == d.Loai.TenLoaiSp) return;
        if (ThaoTac.Chay("Sửa loại sản phẩm", () => new LoaiSpBLL().Sua(new LoaiSpDTO { MaLoaiSp = d.Loai.MaLoaiSp, TenLoaiSp = ten })))
            LamMoi();
    }

    private void Xoa()
    {
        if (_luoi.Chon is not { } d) return;
        if (d.SoSku > 0)
        {
            HopThoai.Canh($"Loại \"{d.Loai.TenLoaiSp}\" còn {d.SoSku} SKU nên không xóa được. Chuyển sản phẩm sang loại khác hoặc ngừng bán.");
            return;
        }
        if (!HopThoai.Hoi($"Xóa loại \"{d.Loai.TenLoaiSp}\"?")) return;
        if (ThaoTac.Chay("Xóa loại sản phẩm", () => new LoaiSpBLL().Xoa(d.Loai.MaLoaiSp))) LamMoi();
    }
}

// ===================================================================== SAN PHAM
public sealed class SanPhamMH : ManHinh
{
    private readonly OTimKiem _tim = new("Mã SP (SP012) hoặc tên sản phẩm");
    private readonly ComboBox _loai = GiaoDien.Chon(170);
    private readonly ComboBox _trangThai = GiaoDien.Chon(130);
    private readonly CheckBox _tonThap = new() { Text = $"Tồn dưới {QuyTac.NguongTonThap}", AutoSize = true, Margin = new Padding(0, 7, 10, 0) };
    private readonly Luoi<SanPhamDTO> _luoi = new() { Khoa = s => s.MaSp };
    private readonly Luoi<SanPhamDTO> _bienThe = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly HenGio _hen = new();
    private List<SanPhamDTO> _tatCa = new();
    private Dictionary<string, decimal> _giaKm = new();
    private List<LoaiSpDTO> _loais = new();
    private bool _dangNap;

    public override Control OTim => _tim.O;

    public SanPhamMH() : base("Sản phẩm & tồn kho", "Mỗi dòng là 1 SKU (1 size - 1 màu). Tồn kho chỉ tăng qua phiếu nhập, giảm khi bán / xác nhận đơn.")
    {
        Nut("Thêm SKU", Keys.Control | Keys.N, ThemMot, chinh: true);
        Nut("Thêm mẫu nhiều size × màu", Keys.Control | Keys.Shift | Keys.N, ThemMau);
        Nut("Sửa", Keys.F2, Sua);
        Phim("Sửa", Keys.Return, Sua);
        Nut("Ngừng / mở bán", Keys.Delete, NgungBan);
        KhoangCach();
        Nut("Xuất Excel", Keys.Control | Keys.E, XuatExcel);
        Nut("Nhập Excel", Keys.Control | Keys.I, NhapExcel);
        Nut("File mẫu", Keys.None, () => XuatFile.LuuExcel(FindForm(), new ExcelBLL().MauSanPham(), "Mau_SanPham"));
        Nut("Làm mới", Keys.F5, LamMoi);
        Phim("Đổi chế độ tìm kiếm", Keys.F3, _tim.DoiCheDo);

        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiSanPham));
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { _tim, GiaoDien.Nhan("Loại"), _loai, GiaoDien.Nhan("Trạng thái"), _trangThai, _tonThap });

        _luoi.Them("Mã SP", s => s.MaSp, 70).Them("Mã loại", s => s.MaLoaiSp, 70).Them("Tên sản phẩm", s => s.TenSp, 240, fill: true).Them("Loại", s => s.TenLoaiSp, 110)
            .Them("Màu", s => s.MauSac, 100).Them("Size", s => s.KichCo, 55).Tien("Đơn giá", s => s.DonGia, 100)
            .Them("Giá KM hôm nay", s => _giaKm.TryGetValue(s.MaSp, out var g) ? g : null, 115, "N0", canPhai: true, mau: _ => GiaoDien.Do)
            .Them("Tồn", s => s.SoLuong, 60, "N0", canPhai: true, mau: s => s.SoLuong < QuyTac.NguongTonThap ? GiaoDien.Do : null)
            .TrangThai("Trạng thái", s => s.TrangThai, Nhan.TrangThaiSanPham, 95);
        _luoi.MoDong += _ => Sua();
        _bienThe.Them("Mã SP", s => s.MaSp, 70).Them("Màu", s => s.MauSac, 100, fill: true).Them("Size", s => s.KichCo, 55)
            .Them("Tồn", s => s.SoLuong, 55, "N0", canPhai: true, mau: s => s.SoLuong < QuyTac.NguongTonThap ? GiaoDien.Do : null)
            .TrangThai("Trạng thái", s => s.TrangThai, Nhan.TrangThaiSanPham, 90);
        _chiTiet = new KhungChiTiet("Chi tiết sản phẩm", _bienThe.Grid, "Các biến thể cùng mẫu (size / màu)");

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 64));

        _tim.CanTim += Loc;
        _loai.SelectedIndexChanged += (_, _) => { if (!_dangNap) Loc(); };
        _trangThai.SelectedIndexChanged += (_, _) => { if (!_dangNap) Loc(); };
        _tonThap.CheckedChanged += (_, _) => Loc();
        _luoi.ChonThayDoi += s => HienChiTiet(s);
    }

    protected override void NhanThamSo(string thamSo)
    {
        if (thamSo == "TON_THAP") _tonThap.Checked = true;
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Tải sản phẩm", () =>
            {
                var bll = new SanPhamBLL();
                return (bll.LayTatCa(), bll.GiaKhuyenMaiHomNay(), new LoaiSpBLL().LayTatCa());
            }, out var kq)) return;
        (_tatCa, _giaKm, _loais) = kq;
        _dangNap = true;
        var chon = Muc.Lay(_loai);
        Muc.Nap(_loai, new[] { new Muc(null, "Tất cả") }.Concat(_loais.Select(l => new Muc(l.MaLoaiSp, l.TenLoaiSp))), chon);
        _dangNap = false;
        Loc();
    }

    // Yeu cau 2: cung 1 bo dieu kien, 2 cach chay
    private TimKiemNangCaoDTO DieuKien()
    {
        var dk = new TimKiemNangCaoDTO { KhopTatCa = true };
        var tk = _tim.TuKhoa;
        if (tk.Length > 0) dk.DieuKiens.Add(TuKhoa.Dk(TuKhoa.LaMa(tk, "SP") ? "ma_sp" : "ten_sp", "LIKE", tk));
        if (Muc.Lay(_loai) is { } l) dk.DieuKiens.Add(TuKhoa.Dk("ma_loai_sp", "=", l));
        if (Muc.Lay(_trangThai) is { } t) dk.DieuKiens.Add(TuKhoa.Dk("trang_thai", "=", t));
        if (_tonThap.Checked) dk.DieuKiens.Add(TuKhoa.Dk("so_luong", "<", QuyTac.NguongTonThap));
        return dk;
    }

    private void Loc()
    {
        var dk = DieuKien();
        var taiSuDung = _tim.TaiSuDung;
        if (!ThaoTac.Chay(taiSuDung ? "Lọc sản phẩm (tái sử dụng)" : "Tìm sản phẩm (truy vấn mới)", () =>
            {
                if (dk.DieuKiens.Count == 0) return taiSuDung ? _tatCa : new SanPhamBLL().LayTatCa();
                return taiSuDung ? new TimKiemBLL().LocSanPham(_tatCa, dk) : new TimKiemBLL().TimSanPham(dk);
            }, out var ds)) return;
        _luoi.Nap(ds);
        _tim.BaoKetQua(ds.Count);
    }

    private void HienChiTiet(SanPhamDTO? s)
    {
        if (s == null)
        {
            _chiTiet.ThongTin.Xoa();
            _bienThe.Nap(Array.Empty<SanPhamDTO>());
            return;
        }
        _chiTiet.TieuDe.Text = $"{s.MaSp} · {s.TenSp}";
        _chiTiet.ThongTin.Dat(
            ("Loại", $"{s.MaLoaiSp} - {s.TenLoaiSp}", null), ("Màu / size", $"{s.MauSac ?? "-"} / {s.KichCo ?? "-"}", null),
            ("Đơn giá", GiaoDien.Tien(s.DonGia), null),
            ("Giá KM hôm nay", _giaKm.TryGetValue(s.MaSp, out var g) ? GiaoDien.Tien(g) : "Không có KM", _giaKm.ContainsKey(s.MaSp) ? GiaoDien.Do : null),
            ("Tồn kho", $"{s.SoLuong:N0} cái", s.SoLuong < QuyTac.NguongTonThap ? GiaoDien.Do : null),
            ("Chất liệu", s.ChatLieu, null), ("Mô tả", s.MoTa, null), ("Hình ảnh", s.HinhAnh, null),
            ("Trạng thái", Nhan.Ten(Nhan.TrangThaiSanPham, s.TrangThai), Nhan.Mau(Nhan.TrangThaiSanPham, s.TrangThai)));
        // main-sub: chi khi dung lai tren 1 dong moi chay cau lay bien the
        var ma = s.MaSp;
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaSp != ma) return;
            if (ThaoTac.Chay("Xem biến thể", () => new SanPhamBLL().LayCacBienThe(ma), out var ds)) _bienThe.Nap(ds);
        });
    }

    private void ThemMot()
    {
        using var f = new FormSanPham(null, _loais);
        if (f.ShowDialog(FindForm()) != DialogResult.OK) return;
        LamMoi();
        if (f.KetQua is { } moi) _luoi.ChonDong(s => s.MaSp == moi.MaSp);
    }

    private void ThemMau()
    {
        using var f = new FormThemMauSanPham(_loais);
        if (f.ShowDialog(FindForm()) != DialogResult.OK) return;
        LamMoi();
        if (f.KetQua.FirstOrDefault() is { } moi) _luoi.ChonDong(s => s.MaSp == moi.MaSp);
    }

    private void Sua()
    {
        if (_luoi.Chon is not { } s) return;
        using var f = new FormSanPham(s, _loais);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void NgungBan()
    {
        if (_luoi.Chon is not { } s) return;
        var ngung = s.TrangThai == "DANG_BAN";
        if (!HopThoai.Hoi(ngung
                ? $"Ngừng bán {s.MaSp} {s.TenSp} ({s.MauSac} / {s.KichCo})?\nSản phẩm ẩn khỏi web, không bán tại quầy được, dữ liệu cũ vẫn giữ."
                : $"Mở bán lại {s.MaSp} {s.TenSp}?")) return;
        if (ThaoTac.Chay(ngung ? "Ngừng bán" : "Mở bán", () => new SanPhamBLL().NgungBan(s.MaSp, ngung))) LamMoi();
    }

    private void XuatExcel()
    {
        var ds = _luoi.DuLieu;
        if (ThaoTac.Chay("Xuất Excel sản phẩm", () => new ExcelBLL().XuatSanPham(ds), out var file))
            XuatFile.LuuExcel(FindForm(), file, "SanPham");
    }

    private void NhapExcel()
    {
        var duongDan = XuatFile.ChonFileExcel(FindForm());
        if (duongDan == null) return;
        if (!ThaoTac.Chay("Đọc Excel sản phẩm", () =>
            {
                using var s = File.OpenRead(duongDan);
                return new ExcelBLL().DocSanPham(s);
            }, out var kq)) return;
        using var xem = new FormXemTruocExcel<SanPhamDTO>("Xem trước sản phẩm từ Excel", kq, l => l
            .Them("Tên sản phẩm", s => s.TenSp, 220, fill: true).Them("Mã loại", s => s.MaLoaiSp, 80).Them("Màu", s => s.MauSac, 90)
            .Them("Size", s => s.KichCo, 60).Tien("Đơn giá", s => s.DonGia).Them("Chất liệu", s => s.ChatLieu, 120), "Thêm");
        if (xem.ShowDialog(FindForm()) != DialogResult.OK) return;
        // tat ca hoac khong: SanPhamBLL.ThemNhieu chay trong 1 giao dich
        if (!ThaoTac.Chay("Nhập sản phẩm từ Excel", () => new ExcelBLL().NhapSanPham(kq.HopLe), out var moi)) return;
        HopThoai.ThongBao($"Đã thêm {moi.Count} SKU ({moi.First().MaSp} … {moi.Last().MaSp}). Tồn kho = 0, lập phiếu nhập để có hàng.");
        LamMoi();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}

// ===================================================================== NHA CUNG CAP
public sealed class NhaCungCapMH : ManHinh
{
    private readonly OTimKiem _tim = new("Tên, SĐT, mã số thuế", haiCheDo: false);
    private readonly ComboBox _trangThai = GiaoDien.Chon(150);
    private readonly Luoi<NhaCungCapDTO> _luoi = new() { Khoa = n => n.MaNcc };
    private readonly Luoi<PhieuNhapDTO> _phieu = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly HenGio _hen = new();
    private List<NhaCungCapDTO> _tatCa = new();

    public override Control OTim => _tim.O;

    public NhaCungCapMH() : base("Nhà cung cấp", "NCC đã có phiếu nhập chỉ chuyển sang ngừng hợp tác, không xóa.")
    {
        Nut("Thêm", Keys.Control | Keys.N, Them, chinh: true);
        Nut("Sửa", Keys.F2, Sua);
        Phim("Sửa", Keys.Return, Sua);
        Nut("Xóa / ngừng hợp tác", Keys.Delete, Xoa, nguyHiem: true);
        KhoangCach();
        Nut("Xuất Excel", Keys.Control | Keys.E, () =>
        {
            var ds = _luoi.DuLieu;
            if (ThaoTac.Chay("Xuất Excel NCC", () => new ExcelBLL().XuatNhaCungCap(ds), out var f)) XuatFile.LuuExcel(FindForm(), f, "NhaCungCap");
        });
        Nut("Nhập Excel", Keys.Control | Keys.I, NhapExcel);
        Nut("File mẫu", Keys.None, () => XuatFile.LuuExcel(FindForm(), new ExcelBLL().MauNhaCungCap(), "Mau_NhaCungCap"));
        Nut("Làm mới", Keys.F5, LamMoi);

        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiNcc));
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { _tim, GiaoDien.Nhan("Trạng thái"), _trangThai });
        _luoi.Them("Mã NCC", n => n.MaNcc, 75).Them("Tên nhà cung cấp", n => n.TenNcc, 220, fill: true).Them("SĐT", n => n.Sdt, 110)
            .Them("Email", n => n.Email, 170).Them("Mã số thuế", n => n.MaSoThue, 110)
            .TrangThai("Trạng thái", n => n.TrangThai, Nhan.TrangThaiNcc, 110);
        _luoi.MoDong += _ => Sua();
        _phieu.Them("Mã phiếu", p => p.MaPhieuNhap, 90).Ngay("Ngày nhập", p => p.NgayNhap).Them("Người lập", p => p.TenNhanVien, 130, fill: true)
            .Tien("Tổng tiền", p => p.TongTien);
        _phieu.ChuKhiTrong = "Chưa có phiếu nhập";
        _phieu.MoDong += _ => MoManHinh?.Invoke("PhieuNhap", null);
        _chiTiet = new KhungChiTiet("Chi tiết nhà cung cấp", _phieu.Grid, "Phiếu nhập từ nhà cung cấp này");

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 60));
        _tim.CanTim += Loc;
        _trangThai.SelectedIndexChanged += (_, _) => Loc();
        _luoi.ChonThayDoi += HienChiTiet;
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Tải nhà cung cấp", () => new NhaCungCapBLL().LayTatCa(), out var ds)) return;
        _tatCa = ds;
        Loc();
    }

    private void Loc()
    {
        var tk = _tim.TuKhoa;
        var tt = Muc.Lay(_trangThai);
        var ds = _tatCa.Where(n => (tt == null || n.TrangThai == tt)
                                   && (tk.Length == 0 || $"{n.MaNcc} {n.TenNcc} {n.Sdt} {n.MaSoThue} {n.Email}".Contains(tk, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
        _luoi.Nap(ds);
        _tim.BaoKetQua(ds.Count);
    }

    private void HienChiTiet(NhaCungCapDTO? n)
    {
        if (n == null)
        {
            _chiTiet.ThongTin.Xoa();
            _phieu.Nap(Array.Empty<PhieuNhapDTO>());
            return;
        }
        _chiTiet.TieuDe.Text = $"{n.MaNcc} · {n.TenNcc}";
        _chiTiet.ThongTin.Dat(("Địa chỉ", n.DiaChi, null), ("SĐT", n.Sdt, null), ("Email", n.Email, null), ("Mã số thuế", n.MaSoThue, null),
            ("Trạng thái", Nhan.Ten(Nhan.TrangThaiNcc, n.TrangThai), Nhan.Mau(Nhan.TrangThaiNcc, n.TrangThai)));
        var ma = n.MaNcc;
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaNcc != ma) return;
            if (ThaoTac.Chay("Phiếu nhập của NCC", () => new PhieuNhapBLL().LayDanhSach(maNcc: ma), out var ds))
            {
                _phieu.Nap(ds);
                _chiTiet.ThongTin.Dat(("Địa chỉ", n.DiaChi, null), ("SĐT", n.Sdt, null), ("Email", n.Email, null), ("Mã số thuế", n.MaSoThue, null),
                    ("Trạng thái", Nhan.Ten(Nhan.TrangThaiNcc, n.TrangThai), Nhan.Mau(Nhan.TrangThaiNcc, n.TrangThai)),
                    ("Đã nhập", $"{ds.Count} phiếu · {GiaoDien.Tien(ds.Sum(p => p.TongTien))}", null));
            }
        });
    }

    private void Them()
    {
        using var f = new FormNhaCungCap(null);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void Sua()
    {
        if (_luoi.Chon is not { } n) return;
        using var f = new FormNhaCungCap(n);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void Xoa()
    {
        if (_luoi.Chon is not { } n) return;
        // bang phieu nhap ben phai da nap xong cho dung NCC nay thi biet truoc co xoa duoc khong
        var coPhieu = _phieu.SoDong > 0 && _phieu.DuLieu[0].MaNcc == n.MaNcc;
        if (coPhieu)
        {
            if (n.TrangThai == "NGUNG_HOP_TAC")
            {
                HopThoai.Canh("Nhà cung cấp đã có phiếu nhập nên không xóa được (đang ở trạng thái ngừng hợp tác).");
                return;
            }
            if (!HopThoai.Hoi($"{n.TenNcc} đã có {_phieu.SoDong} phiếu nhập nên không xóa được.\nChuyển sang ngừng hợp tác?")) return;
            var sua = new NhaCungCapDTO
            {
                MaNcc = n.MaNcc, TenNcc = n.TenNcc, DiaChi = n.DiaChi, Sdt = n.Sdt, Email = n.Email, MaSoThue = n.MaSoThue,
                TrangThai = "NGUNG_HOP_TAC"
            };
            if (ThaoTac.Chay("Ngừng hợp tác NCC", () => new NhaCungCapBLL().Sua(sua))) LamMoi();
            return;
        }
        if (!HopThoai.Hoi($"Xóa nhà cung cấp {n.MaNcc} - {n.TenNcc}?")) return;
        if (ThaoTac.Chay("Xóa NCC", () => new NhaCungCapBLL().Xoa(n.MaNcc))) LamMoi();
    }

    private void NhapExcel()
    {
        var duongDan = XuatFile.ChonFileExcel(FindForm());
        if (duongDan == null) return;
        if (!ThaoTac.Chay("Đọc Excel NCC", () =>
            {
                using var s = File.OpenRead(duongDan);
                return new ExcelBLL().DocNhaCungCap(s);
            }, out var kq)) return;
        using var xem = new FormXemTruocExcel<NhaCungCapDTO>("Xem trước nhà cung cấp từ Excel", kq, l => l
            .Them("Tên nhà cung cấp", n => n.TenNcc, 220, fill: true).Them("Địa chỉ", n => n.DiaChi, 180).Them("SĐT", n => n.Sdt, 110)
            .Them("Email", n => n.Email, 150).Them("Mã số thuế", n => n.MaSoThue, 110), "Thêm");
        if (xem.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (!ThaoTac.Chay("Nhập NCC từ Excel", () => new ExcelBLL().NhapNhaCungCap(kq.HopLe), out var kqNhap)) return;
        BaoKetQuaNhap(kqNhap.SoDongDaThem, kqNhap.Lois);
        LamMoi();
    }

    // Dung chung cho nhap khach hang / NCC: them tung dong, dong loi (vd trung) bao lai
    public static void BaoKetQuaNhap(int soDong, List<LoiDongExcelDTO> lois) =>
        HopThoai.ThongBao($"Đã thêm {soDong} dòng." + (lois.Count == 0
            ? ""
            : $"\n{lois.Count} dòng không thêm được (thứ tự trong các dòng hợp lệ):\n"
              + string.Join("\n", lois.Take(15).Select(l => $"  • Dòng {l.Dong}: {l.ThongBao}")) + (lois.Count > 15 ? "\n  …" : "")));

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}
