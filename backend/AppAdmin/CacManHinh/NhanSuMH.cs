using AppAdmin.CacHopThoai;

namespace AppAdmin.CacManHinh;

// ===================================================================== KHACH HANG (+ gio hang cua khach)
public sealed class KhachHangMH : ManHinh
{
    private readonly OTimKiem _tim = new("Mã KH, SĐT hoặc họ tên");
    private readonly ComboBox _hang = GiaoDien.Chon(130);
    private readonly ComboBox _taiKhoan = GiaoDien.Chon(150);
    private readonly Luoi<KhachHangDTO> _luoi = new() { Khoa = k => k.MaKh };
    private readonly KhungThongTin _thongTin = new();
    private readonly KhungThongTin _ruiRo = new();
    private readonly Luoi<DonDatHangDTO> _don = new();
    private readonly Luoi<HoaDonDTO> _hoaDon = new();
    private readonly Luoi<ChiTietGioHangDTO> _gio = new();
    private readonly Label _tongGio = GiaoDien.Nhan("", GiaoDien.ChuDam);
    private readonly Button _xoaGio = GiaoDien.NutNguyHiem("Xóa giỏ hàng của khách");
    private readonly TabControl _tab = new() { Dock = DockStyle.Fill, Font = GiaoDien.Chu, Padding = new Point(14, 5) };
    private readonly HenGio _hen = new();
    private List<KhachHangDTO> _tatCa = new();

    public override Control OTim => _tim.O;

    public KhachHangMH() : base("Khách hàng & giỏ hàng",
        "Khách có tài khoản đăng ký trên web; khách mua tại quầy / vãng lai không có tài khoản. Chọn khách để xem đơn, hóa đơn, giỏ hàng.")
    {
        Nut("Thêm khách tại quầy", Keys.Control | Keys.N, Them, chinh: true);
        Nut("Sửa", Keys.F2, Sua);
        Phim("Sửa", Keys.Return, Sua);
        Nut("Hạng / điểm", Keys.Control | Keys.H, SuaHangDiem);
        KhoangCach();
        Nut("Xuất Excel", Keys.Control | Keys.E, () =>
        {
            var ds = _luoi.DuLieu;
            if (ThaoTac.Chay("Xuất Excel khách hàng", () => new ExcelBLL().XuatKhachHang(ds), out var f)) XuatFile.LuuExcel(FindForm(), f, "KhachHang");
        });
        Nut("Nhập Excel", Keys.Control | Keys.I, NhapExcel);
        Nut("File mẫu", Keys.None, () => XuatFile.LuuExcel(FindForm(), new ExcelBLL().MauKhachHang(), "Mau_KhachHang"));
        Nut("Làm mới", Keys.F5, LamMoi);
        Phim("Đổi chế độ tìm kiếm", Keys.F3, _tim.DoiCheDo);

        Muc.Nap(_hang, Nhan.CacMuc(Nhan.HangThanhVien));
        Muc.Nap(_taiKhoan, new[] { new Muc(null, "Tất cả"), new Muc("CO", "Có tài khoản"), new Muc("KHONG", "Không tài khoản") });
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { _tim, GiaoDien.Nhan("Hạng"), _hang, _taiKhoan });

        _luoi.Them("Mã KH", k => k.MaKh, 70).Them("Mã TK", k => k.MaTk ?? "Không", 70, mau: k => k.MaTk == null ? GiaoDien.ChuPhu : null).Them("Họ tên", k => k.HoTen, 180, fill: true).Them("SĐT", k => k.Sdt, 105)
            .Them("Email", k => k.Email, 170).Them("Tỉnh / thành", k => k.TinhThanh, 120)
            .TrangThai("Hạng", k => k.HangThanhVien, Nhan.HangThanhVien, 85).So("Điểm", k => k.DiemTichLuy, 65);
        _luoi.MoDong += _ => Sua();

        _don.Them("Mã đơn", d => d.MaDon, 80).Ngay("Ngày đặt", d => d.NgayDat, 120, coGio: true)
            .TrangThai("Trạng thái", d => d.TrangThai, Nhan.TrangThaiDon, 110).Tien("Tổng tiền", d => d.TongTien, 110);
        _don.ChuKhiTrong = "Chưa có đơn đặt hàng";
        _hoaDon.Them("Mã HĐ", h => h.MaHoaDon, 80).Ngay("Ngày lập", h => h.NgayLap)
            .TrangThai("Trạng thái", h => h.TrangThai, Nhan.TrangThaiHoaDon, 110).Tien("Tổng tiền", h => h.TongTien, 110);
        _hoaDon.ChuKhiTrong = "Chưa có hóa đơn";
        _gio.Them("Mã SP", c => c.MaSp, 70).Them("Sản phẩm", c => $"{c.TenSp} - {c.MauSac} - {c.KichCo}", 180, fill: true)
            .So("SL", c => c.SoLuong, 50).Tien("Đơn giá", c => c.DonGia, 95).Them("Tồn", c => c.TonKho, 55, "N0", true,
                mau: c => c.TonKho < c.SoLuong ? GiaoDien.Do : null);
        _gio.ChuKhiTrong = "Giỏ hàng trống";
        _xoaGio.Click += (_, _) => XoaGio();

        var tabTT = new TabPage("Thông tin") { BackColor = Color.White, Padding = new Padding(8), AutoScroll = true };
        tabTT.Controls.Add(_ruiRo);
        tabTT.Controls.Add(GiaoDien.TieuDeMuc("Độ tin cậy SĐT (đơn online)"));
        tabTT.Controls.Add(_thongTin);
        var tabGio = new TabPage("Giỏ hàng") { BackColor = Color.White, Padding = new Padding(8) };
        var duoiGio = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        duoiGio.Controls.Add(_xoaGio);
        duoiGio.Controls.Add(_tongGio);
        tabGio.Controls.Add(_gio.Grid);
        tabGio.Controls.Add(duoiGio);
        _tab.TabPages.Add(tabTT);
        _tab.TabPages.Add(TaoTab("Đơn hàng", _don.Grid));
        _tab.TabPages.Add(TaoTab("Hóa đơn", _hoaDon.Grid));
        _tab.TabPages.Add(tabGio);
        _tab.SelectedIndexChanged += (_, _) => HienChiTiet(_luoi.Chon);

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        var phai = GiaoDien.Khung(dem: 1);
        phai.Controls.Add(_tab);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, phai, 58));

        _tim.CanTim += Loc;
        _hang.SelectedIndexChanged += (_, _) => Loc();
        _taiKhoan.SelectedIndexChanged += (_, _) => Loc();
        _luoi.ChonThayDoi += HienChiTiet;
    }

    private static TabPage TaoTab(string ten, Control noiDung)
    {
        var t = new TabPage(ten) { BackColor = Color.White, Padding = new Padding(8) };
        t.Controls.Add(noiDung);
        return t;
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Tải khách hàng", () => new KhachHangBLL().LayTatCa(), out var ds)) return;
        _tatCa = ds;
        Loc();
    }

    // Yeu cau 2: tu khoa la so -> tim theo SDT, "KH..." -> theo ma, con lai -> theo ho ten
    private TimKiemNangCaoDTO DieuKien()
    {
        var dk = new TimKiemNangCaoDTO { KhopTatCa = true };
        var tk = _tim.TuKhoa;
        if (TuKhoa.LaMa(tk, "KH")) dk.DieuKiens.Add(TuKhoa.Dk("ma_kh", "LIKE", tk));
        else if (TuKhoa.LaSo(tk)) dk.DieuKiens.Add(TuKhoa.Dk("sdt", "LIKE", new string(tk.Where(char.IsDigit).ToArray())));
        else if (tk.Length > 0) dk.DieuKiens.Add(TuKhoa.Dk("ho_ten", "LIKE", tk));
        if (Muc.Lay(_hang) is { } h) dk.DieuKiens.Add(TuKhoa.Dk("hang_thanh_vien", "=", h));
        return dk;
    }

    private void Loc()
    {
        var dk = DieuKien();
        var taiSuDung = _tim.TaiSuDung;
        if (!ThaoTac.Chay(taiSuDung ? "Lọc khách hàng (tái sử dụng)" : "Tìm khách hàng (truy vấn mới)", () =>
            {
                if (dk.DieuKiens.Count == 0) return taiSuDung ? _tatCa : new KhachHangBLL().LayTatCa();
                return taiSuDung ? new TimKiemBLL().LocKhachHang(_tatCa, dk) : new TimKiemBLL().TimKhachHang(dk);
            }, out var ds)) return;
        // co / khong tai khoan khong co trong danh sach truong tim kiem cua BLL: loc them tren ket qua
        ds = Muc.Lay(_taiKhoan) switch
        {
            "CO" => ds.Where(k => k.MaTk != null).ToList(),
            "KHONG" => ds.Where(k => k.MaTk == null).ToList(),
            _ => ds
        };
        _luoi.Nap(ds);
        _tim.BaoKetQua(ds.Count);
    }

    // Main-sub: chi nap du lieu cua tab dang mo
    private void HienChiTiet(KhachHangDTO? k)
    {
        if (k == null)
        {
            _thongTin.Xoa();
            _ruiRo.Xoa("");
            return;
        }
        _thongTin.Dat(("Mã KH", k.MaKh, null), ("Họ tên", k.HoTen, null), ("SĐT", k.Sdt + (k.SdtDaXacThuc ? "  ✓ đã xác thực OTP" : ""), null),
            ("Email", k.Email, null), ("Ngày sinh", GiaoDien.NgayVn(k.NgaySinh), null), ("Địa chỉ", k.DiaChi, null),
            ("Tỉnh / thành", k.TinhThanh, null),
            ("Hạng / điểm", $"{Nhan.Ten(Nhan.HangThanhVien, k.HangThanhVien)} · {k.DiemTichLuy:N0} điểm", Nhan.Mau(Nhan.HangThanhVien, k.HangThanhVien)),
            ("Tài khoản", k.MaTk == null ? "Không (mua tại quầy / vãng lai)" : k.MaTk, null));
        var ma = k.MaKh;
        var tab = _tab.SelectedIndex;
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaKh != ma) return;
            switch (tab)
            {
                case 0:
                    if (k.Sdt == null)
                    {
                        _ruiRo.Xoa("Khách chưa có số điện thoại");
                        break;
                    }
                    if (ThaoTac.Chay("Độ tin cậy SĐT", () => new KhachHangBLL().RuiRo(k.Sdt, ma), out var r))
                        _ruiRo.Dat(("Mức", Nhan.Ten(Nhan.RuiRo, r.MucRuiRo), Nhan.Mau(Nhan.RuiRo, r.MucRuiRo)), ("Ghi chú", r.GhiChu, null),
                            ("Đơn hoàn tất", r.SoDonHoanTat.ToString(), null), ("Đang chờ", r.SoDonDangCho.ToString(), null),
                            ("Bị từ chối", r.SoDonBiTuChoi.ToString(), null),
                            ("Bom hàng", r.SoLanBomHang.ToString(), r.SoLanBomHang > 0 ? GiaoDien.Do : null),
                            ("Được chọn COD", r.DuocDatCod ? "Có" : "Không", r.DuocDatCod ? null : GiaoDien.Do));
                    break;
                case 1:
                    if (ThaoTac.Chay("Đơn của khách", () => new DonDatHangBLL().LayCuaKhach(ma), out var dons)) _don.Nap(dons);
                    break;
                case 2:
                    if (ThaoTac.Chay("Hóa đơn của khách", () => new HoaDonBLL().LayCuaKhach(ma), out var hds)) _hoaDon.Nap(hds);
                    break;
                case 3:
                    if (k.MaTk == null)
                    {
                        _gio.Nap(Array.Empty<ChiTietGioHangDTO>());
                        _tongGio.Text = "Khách không có tài khoản nên không có giỏ hàng trên server";
                        _xoaGio.Enabled = false;
                        break;
                    }
                    if (ThaoTac.Chay("Giỏ hàng của khách", () => new GioHangBLL().Xem(ma), out var gio))
                    {
                        _gio.Nap(gio.ChiTiets);
                        _tongGio.Text = gio.ChiTiets.Count == 0 ? "" : $"Tạm tính {GiaoDien.Tien(gio.TamTinh)} · cập nhật {GiaoDien.GioVn(gio.NgayCapNhat)}";
                        _xoaGio.Enabled = gio.ChiTiets.Count > 0;
                    }
                    break;
            }
        });
    }

    private void XoaGio()
    {
        if (_luoi.Chon is not { } k || !HopThoai.Hoi($"Xóa toàn bộ giỏ hàng của {k.HoTen}?\nDùng khi giỏ có hàng ngừng bán / hết hàng làm khách không đặt được.")) return;
        if (ThaoTac.Chay("Xóa giỏ hàng", () => new GioHangBLL().XoaHet(k.MaKh))) HienChiTiet(k);
    }

    private void Them()
    {
        using var f = new FormKhachHang(null);
        if (f.ShowDialog(FindForm()) != DialogResult.OK) return;
        LamMoi();
        if (f.KetQua is { } k) _luoi.ChonDong(x => x.MaKh == k.MaKh);
    }

    private void Sua()
    {
        if (_luoi.Chon is not { } k) return;
        using var f = new FormKhachHang(k);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void SuaHangDiem()
    {
        if (_luoi.Chon is not { } k) return;
        using var f = new FormHangDiem(k);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void NhapExcel()
    {
        var duongDan = XuatFile.ChonFileExcel(FindForm());
        if (duongDan == null) return;
        if (!ThaoTac.Chay("Đọc Excel khách hàng", () =>
            {
                using var s = File.OpenRead(duongDan);
                return new ExcelBLL().DocKhachHang(s);
            }, out var kq)) return;
        using var xem = new FormXemTruocExcel<KhachHangDTO>("Xem trước khách hàng từ Excel", kq, l => l
            .Them("Họ tên", k => k.HoTen, 180, fill: true).Them("SĐT", k => k.Sdt, 110).Them("Email", k => k.Email, 170)
            .Ngay("Ngày sinh", k => k.NgaySinh).Them("Tỉnh / thành", k => k.TinhThanh, 120), "Thêm");
        if (xem.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (!ThaoTac.Chay("Nhập khách hàng từ Excel", () => new ExcelBLL().NhapKhachHang(kq.HopLe), out var kqNhap)) return;
        NhaCungCapMH.BaoKetQuaNhap(kqNhap.SoDongDaThem, kqNhap.Lois);
        LamMoi();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}

// ===================================================================== NHAN VIEN
public sealed class NhanVienMH : ManHinh
{
    private readonly OTimKiem _tim = new("Mã NV, họ tên, chức vụ", haiCheDo: false);
    private readonly ComboBox _trangThai = GiaoDien.Chon(130);
    private readonly Luoi<NhanVienDTO> _luoi = new() { Khoa = n => n.MaNv };
    private readonly Luoi<HoaDonDTO> _hoaDon = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly HenGio _hen = new();
    private List<NhanVienDTO> _tatCa = new();

    public override Control OTim => _tim.O;

    public NhanVienMH() : base("Nhân viên", "Thêm nhân viên tạo luôn tài khoản đăng nhập. Nghỉ việc: giữ hồ sơ (hóa đơn cũ vẫn trỏ tới), khóa tài khoản.")
    {
        Nut("Thêm nhân viên", Keys.Control | Keys.N, Them, chinh: true);
        Nut("Sửa", Keys.F2, Sua);
        Phim("Sửa", Keys.Return, Sua);
        Nut("Cho nghỉ việc", Keys.Delete, NghiViec, nguyHiem: true);
        Nut("Đặt lại mật khẩu", Keys.Control | Keys.R, DatLaiMatKhau);
        KhoangCach();
        Nut("Xuất Excel", Keys.Control | Keys.E, () =>
        {
            var ds = _luoi.DuLieu;
            if (ThaoTac.Chay("Xuất Excel nhân viên", () => new ExcelBLL().XuatNhanVien(ds), out var f)) XuatFile.LuuExcel(FindForm(), f, "NhanVien");
        });
        Nut("Làm mới", Keys.F5, LamMoi);

        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiNhanVien), "DANG_LAM");
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { _tim, GiaoDien.Nhan("Trạng thái"), _trangThai });
        _luoi.Them("Mã NV", n => n.MaNv, 70).Them("Mã TK", n => n.MaTk, 70).Them("Họ tên", n => n.HoTen, 170, fill: true).Them("Chức vụ", n => n.ChucVu, 170)
            .Them("SĐT", n => n.Sdt, 100).Them("Email", n => n.Email, 190).Ngay("Vào làm", n => n.NgayVaoLam)
            .Tien("Lương", n => n.Luong, 105).Them("Tài khoản", n => n.TenDangNhap, 110)
            .TrangThai("Vai trò", n => n.VaiTro, Nhan.VaiTro, 120).TrangThai("Trạng thái", n => n.TrangThai, Nhan.TrangThaiNhanVien, 90);
        _luoi.MoDong += _ => Sua();
        _hoaDon.Them("Mã HĐ", h => h.MaHoaDon, 80).Ngay("Ngày lập", h => h.NgayLap).Them("Khách", h => h.TenKhachHang ?? "Khách vãng lai", 140, fill: true)
            .Tien("Tổng tiền", h => h.TongTien, 110);
        _hoaDon.ChuKhiTrong = "Chưa lập hóa đơn nào";
        _chiTiet = new KhungChiTiet("Chi tiết nhân viên", _hoaDon.Grid, "Hóa đơn do nhân viên lập");

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 64));
        _tim.CanTim += Loc;
        _trangThai.SelectedIndexChanged += (_, _) => Loc();
        _luoi.ChonThayDoi += HienChiTiet;
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Tải nhân viên", () => new NhanVienBLL().LayTatCa(), out var ds)) return;
        _tatCa = ds;
        Loc();
    }

    private void Loc()
    {
        var tk = _tim.TuKhoa;
        var tt = Muc.Lay(_trangThai);
        var ds = _tatCa.Where(n => (tt == null || n.TrangThai == tt)
                                   && (tk.Length == 0 || $"{n.MaNv} {n.HoTen} {n.ChucVu} {n.TenDangNhap} {n.Sdt}".Contains(tk, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
        _luoi.Nap(ds);
        _tim.BaoKetQua(ds.Count);
    }

    private void HienChiTiet(NhanVienDTO? n)
    {
        if (n == null)
        {
            _chiTiet.ThongTin.Xoa();
            _hoaDon.Nap(Array.Empty<HoaDonDTO>());
            return;
        }
        _chiTiet.TieuDe.Text = $"{n.MaNv} · {n.HoTen}";
        (string, string?, Color?)[] ThongTin(string? them = null) => new (string, string?, Color?)[]
        {
            ("Chức vụ", n.ChucVu, null), ("SĐT / email", $"{n.Sdt ?? "-"} · {n.Email}", null), ("Ngày vào làm", GiaoDien.NgayVn(n.NgayVaoLam), null),
            ("Lương", GiaoDien.Tien(n.Luong), null), ("Tài khoản", $"{n.TenDangNhap} ({Nhan.Ten(Nhan.VaiTro, n.VaiTro)})", null),
            ("Trạng thái", Nhan.Ten(Nhan.TrangThaiNhanVien, n.TrangThai), Nhan.Mau(Nhan.TrangThaiNhanVien, n.TrangThai)),
            ("Doanh số", them ?? "đang tải…", null)
        };
        _chiTiet.ThongTin.Dat(ThongTin());
        var ma = n.MaNv;
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaNv != ma || !ThaoTac.Chay("Hóa đơn của nhân viên", () => new HoaDonBLL().LayDanhSach(maNv: ma), out var ds)) return;
            _hoaDon.Nap(ds);
            var daTra = ds.Where(h => h.TrangThai == "DA_THANH_TOAN").ToList();
            _chiTiet.ThongTin.Dat(ThongTin($"{daTra.Count:N0} hóa đơn đã thanh toán · {GiaoDien.Tien(daTra.Sum(h => h.TongTien))}"));
        });
    }

    private void Them()
    {
        using var f = new FormNhanVien(null);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void Sua()
    {
        if (_luoi.Chon is not { } n) return;
        using var f = new FormNhanVien(n);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void NghiViec()
    {
        if (_luoi.Chon is not { } n) return;
        if (n.MaNv == Phien.Hien?.MaNv)
        {
            HopThoai.Canh("Không tự cho mình nghỉ việc được (sẽ bị khóa tài khoản đang dùng).");
            return;
        }
        if (n.TrangThai == "NGHI_VIEC")
        {
            HopThoai.Canh("Nhân viên này đã nghỉ việc.");
            return;
        }
        if (!HopThoai.Hoi($"Cho {n.HoTen} ({n.MaNv}) nghỉ việc?\nTài khoản \"{n.TenDangNhap}\" bị khóa ngay (web staff hết hiệu lực trong tối đa 30 giây).")) return;
        if (ThaoTac.Chay("Cho nghỉ việc", () => new NhanVienBLL().NghiViec(n.MaNv))) LamMoi();
    }

    private void DatLaiMatKhau()
    {
        if (_luoi.Chon is not { } n || !HopThoai.Hoi($"Đặt lại mật khẩu cho tài khoản \"{n.TenDangNhap}\" của {n.HoTen}?")) return;
        if (ThaoTac.Chay("Đặt lại mật khẩu", () => new TaiKhoanBLL().DatLaiMatKhau(n.MaTk), out var mk))
            HopThoai.HienChuoiSaoChep(FindForm(), "Mật khẩu tạm", $"Mật khẩu tạm của \"{n.TenDangNhap}\". Báo cho nhân viên và nhắc đổi ngay sau khi đăng nhập.", mk);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}

// ===================================================================== TAI KHOAN
public sealed class TaiKhoanMH : ManHinh
{
    private readonly OTimKiem _tim = new("Tên đăng nhập, họ tên, mã", haiCheDo: false);
    private readonly ComboBox _vaiTro = GiaoDien.Chon(170);
    private readonly ComboBox _trangThai = GiaoDien.Chon(130);
    private readonly Luoi<TaiKhoanDTO> _luoi = new() { Khoa = t => t.MaTk };
    private readonly Button _khoa;
    private List<TaiKhoanDTO> _tatCa = new();
    // Ho so nhan vien / khach hang tai san 1 lan cung danh sach tai khoan: re chuot xem nhanh 0 cau SQL
    private Dictionary<string, NhanVienDTO> _nvTheoMa = new();
    private Dictionary<string, KhachHangDTO> _khTheoMa = new();

    public override Control OTim => _tim.O;

    public TaiKhoanMH() : base("Tài khoản",
        "Mỗi vai trò chỉ đăng nhập đúng nơi: KHACH_HANG → web bán hàng, NHAN_VIEN → web staff, ADMIN → app admin + web staff.")
    {
        _khoa = Nut("Khóa / mở khóa", Keys.Delete, KhoaMo, nguyHiem: true);
        Nut("Đặt lại mật khẩu", Keys.Control | Keys.R, DatLai);
        KhoangCach();
        Nut("Làm mới", Keys.F5, LamMoi);
        Muc.Nap(_vaiTro, Nhan.CacMuc(Nhan.VaiTro));
        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiTaiKhoan));
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { _tim, GiaoDien.Nhan("Vai trò"), _vaiTro, GiaoDien.Nhan("Trạng thái"), _trangThai });
        _luoi.Them("Mã TK", t => t.MaTk, 75).Them("Mã NV / KH", t => t.MaNv ?? t.MaKh, 100).Them("Tên đăng nhập", t => t.TenDangNhap, 150).Them("Họ tên", t => t.HoTen, 200, fill: true)
            .TrangThai("Vai trò", t => t.VaiTro, Nhan.VaiTro, 140)
            .TrangThai("Trạng thái", t => t.TrangThai, Nhan.TrangThaiTaiKhoan, 100).Ngay("Ngày tạo", t => t.NgayTao, 130, coGio: true);
        _luoi.ChonThayDoi += t => _khoa.Text = t?.TrangThai == "KHOA" ? "Mở khóa  (Delete)" : "Khóa  (Delete)";
        _luoi.XemNhanh = TheTaiKhoan;
        NoiDung.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        NoiDung.Controls.Add(loc);
        _tim.CanTim += Loc;
        _vaiTro.SelectedIndexChanged += (_, _) => Loc();
        _trangThai.SelectedIndexChanged += (_, _) => Loc();
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Tải tài khoản + hồ sơ", () => (new TaiKhoanBLL().LayTatCa(), new NhanVienBLL().LayTatCa(), new KhachHangBLL().LayTatCa()),
                out var kq)) return;
        _tatCa = kq.Item1;
        _nvTheoMa = kq.Item2.GroupBy(n => n.MaNv).ToDictionary(g => g.Key, g => g.First());
        _khTheoMa = kq.Item3.GroupBy(k => k.MaKh).ToDictionary(g => g.Key, g => g.First());
        Loc();
    }

    // Noi dung the xem nhanh khi re chuot vao 1 tai khoan: thong tin tai khoan + ho so nhan vien / khach hang
    private TheXemNhanh.NoiDung TheTaiKhoan(TaiKhoanDTO t)
    {
        var dongs = new List<TheXemNhanh.Dong>
        {
            new("Trạng thái", Nhan.Ten(Nhan.TrangThaiTaiKhoan, t.TrangThai), Nhan.Mau(Nhan.TrangThaiTaiKhoan, t.TrangThai)),
            new("Ngày tạo", GiaoDien.GioVn(t.NgayTao)),
        };
        if (t.MaNv != null && _nvTheoMa.TryGetValue(t.MaNv, out var n))
        {
            dongs.Add(new("Mã nhân viên", n.MaNv));
            dongs.Add(new("Chức vụ", n.ChucVu));
            dongs.Add(new("SĐT", n.Sdt));
            dongs.Add(new("Email", n.Email));
            dongs.Add(new("Ngày vào làm", GiaoDien.NgayVn(n.NgayVaoLam)));
            dongs.Add(new("Công việc", Nhan.Ten(Nhan.TrangThaiNhanVien, n.TrangThai), Nhan.Mau(Nhan.TrangThaiNhanVien, n.TrangThai)));
        }
        else if (t.MaKh != null && _khTheoMa.TryGetValue(t.MaKh, out var k))
        {
            dongs.Add(new("Mã khách hàng", k.MaKh));
            dongs.Add(new("SĐT", k.Sdt));
            dongs.Add(new("Email", k.Email));
            dongs.Add(new("Ngày sinh", GiaoDien.NgayVn(k.NgaySinh)));
            dongs.Add(new("Hạng / điểm", $"{Nhan.Ten(Nhan.HangThanhVien, k.HangThanhVien)} · {k.DiemTichLuy:N0} điểm",
                Nhan.Mau(Nhan.HangThanhVien, k.HangThanhVien)));
            dongs.Add(new("Địa chỉ", string.Join(", ", new[] { k.DiaChi, k.TinhThanh }.Where(s => !string.IsNullOrWhiteSpace(s)))));
        }
        return new TheXemNhanh.NoiDung(
            TieuDe: string.IsNullOrWhiteSpace(t.HoTen) ? t.TenDangNhap : t.HoTen,
            PhuDe: $"@{t.TenDangNhap} · {t.MaTk}",
            Dongs: dongs,
            MauNhan: Nhan.Mau(Nhan.VaiTro, t.VaiTro),
            Nhan: Nhan.Ten(Nhan.VaiTro, t.VaiTro));
    }

    private void Loc()
    {
        var tk = _tim.TuKhoa;
        var vt = Muc.Lay(_vaiTro);
        var tt = Muc.Lay(_trangThai);
        var ds = _tatCa.Where(t => (vt == null || t.VaiTro == vt) && (tt == null || t.TrangThai == tt)
                                   && (tk.Length == 0 || $"{t.MaTk} {t.TenDangNhap} {t.HoTen} {t.MaNv} {t.MaKh}".Contains(tk, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
        _luoi.Nap(ds);
        _tim.BaoKetQua(ds.Count);
    }

    private void KhoaMo()
    {
        if (_luoi.Chon is not { } t) return;
        var khoa = t.TrangThai != "KHOA";
        if (khoa && t.MaTk == Phien.MaTk)
        {
            HopThoai.Canh("Không tự khóa tài khoản đang đăng nhập.");
            return;
        }
        if (!HopThoai.Hoi(khoa
                ? $"Khóa tài khoản \"{t.TenDangNhap}\" ({t.HoTen})?\nPhiên đăng nhập trên web hết hiệu lực trong tối đa 30 giây."
                : $"Mở khóa tài khoản \"{t.TenDangNhap}\"?" + (t.MaNv != null ? "\nLưu ý: nhân viên đã nghỉ việc vẫn không đăng nhập được." : ""))) return;
        if (ThaoTac.Chay(khoa ? "Khóa tài khoản" : "Mở khóa tài khoản", () => new TaiKhoanBLL().KhoaMo(t.MaTk, khoa))) LamMoi();
    }

    private void DatLai()
    {
        if (_luoi.Chon is not { } t || !HopThoai.Hoi($"Đặt lại mật khẩu cho \"{t.TenDangNhap}\" ({t.HoTen})?")) return;
        if (ThaoTac.Chay("Đặt lại mật khẩu", () => new TaiKhoanBLL().DatLaiMatKhau(t.MaTk), out var mk))
            HopThoai.HienChuoiSaoChep(FindForm(), "Mật khẩu tạm", $"Mật khẩu tạm của \"{t.TenDangNhap}\". Báo cho người dùng và nhắc đổi ngay.", mk);
    }
}
