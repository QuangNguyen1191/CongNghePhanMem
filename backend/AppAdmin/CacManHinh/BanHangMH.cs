using AppAdmin.CacHopThoai;

namespace AppAdmin.CacManHinh;

// ===================================================================== DON DAT HANG
// Luong don online: CHO_XAC_NHAN -(xac nhan: tru kho + lap HD)-> DA_XAC_NHAN -(giao)-> DANG_GIAO -(hoan tat: HD COD da thanh toan)-> HOAN_TAT
//                   CHO_XAC_NHAN -(tu choi)-> TU_CHOI ;  chua HOAN_TAT -(huy: tra kho, huy HD, hoan tien neu da tra)-> DA_HUY
// Khach xin huy don DA_XAC_NHAN: yeu_cau_huy = CHO_DUYET -> duyet (huy, khong tinh bom hang) / tu choi (bat buoc ly do)
public sealed class DonHangMH : ManHinh
{
    private readonly ComboBox _trangThai = GiaoDien.Chon(150);
    private readonly ComboBox _nguon = GiaoDien.Chon(120);
    private readonly DateTimePicker _tu = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly DateTimePicker _den = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly OTimKiem _tim = new("Mã đơn, tên khách, SĐT", 200, haiCheDo: false);
    private readonly Luoi<DonDatHangDTO> _luoi = new() { Khoa = d => d.MaDon };
    private readonly Luoi<ChiTietDonDatHangDTO> _dong = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly KhungThongTin _ruiRo = new();
    private readonly HenGio _hen = new();
    private readonly Button _xacNhan, _tuChoi, _giao, _hoanTat, _huy, _duyetHuy, _tuChoiHuy, _pdf;
    private List<DonDatHangDTO> _tatCa = new();
    private string? _chonSau;
    private bool _dangNap;

    public override Control OTim => _tim.O;

    public DonHangMH() : base("Đơn đặt hàng", "Kho chỉ bị trừ khi xác nhận đơn. Đơn vãng lai: xem độ tin cậy SĐT trước khi xác nhận.")
    {
        _xacNhan = Nut("Xác nhận", Keys.Alt | Keys.X, XacNhan, chinh: true);
        _tuChoi = Nut("Từ chối", Keys.Alt | Keys.T, TuChoi);
        _giao = Nut("Giao hàng", Keys.Alt | Keys.G, GiaoHang);
        _hoanTat = Nut("Hoàn tất", Keys.Alt | Keys.H, HoanTat);
        _huy = Nut("Hủy đơn", Keys.Alt | Keys.U, Huy, nguyHiem: true);
        KhoangCach();
        _duyetHuy = Nut("Duyệt xin hủy", Keys.Alt | Keys.D, DuyetHuy);
        _tuChoiHuy = Nut("Từ chối xin hủy", Keys.Alt | Keys.R, TuChoiHuy);
        KhoangCach();
        _pdf = Nut("In PDF", Keys.Control | Keys.P, InPdf);
        Nut("Làm mới", Keys.F5, LamMoi);

        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiDon).Append(new Muc("XIN_HUY", "Khách xin hủy")));
        Muc.Nap(_nguon, Nhan.CacMuc(Nhan.NguonDon));
        _tu.Checked = _den.Checked = false;
        var loc = GiaoDien.HangNgang();
        var ap = GiaoDien.NutPhu("Lọc");
        ap.Click += (_, _) => Tai();
        loc.Controls.AddRange(new Control[]
        {
            _tim, GiaoDien.Nhan("Trạng thái"), _trangThai, GiaoDien.Nhan("Nguồn"), _nguon, GiaoDien.Nhan("Từ"), _tu, GiaoDien.Nhan("đến"), _den, ap
        });

        _luoi.Them("Mã đơn", d => d.MaDon, 78).Them("Mã KH", d => d.MaKh ?? "Vãng lai", 72, mau: d => d.MaKh == null ? GiaoDien.ChuPhu : null)
            .Them("Mã HĐ", d => d.MaHoaDon, 78).Ngay("Ngày đặt", d => d.NgayDat, 118, coGio: true)
            .Them("Khách hàng", d => d.TenKhachHang, 150, fill: true).Them("SĐT nhận", d => d.SdtNhan, 100)
            .TrangThai("Nguồn", d => d.Nguon, Nhan.NguonDon, 80).Them("Thanh toán", d => Nhan.Ten(Nhan.PhuongThuc, d.PhuongThucTt), 105)
            .Tien("Tổng tiền", d => d.TongTien, 105).TrangThai("Trạng thái", d => d.TrangThai, Nhan.TrangThaiDon, 105)
            .TrangThai("Xin hủy", d => d.YeuCauHuy, Nhan.YeuCauHuy, 105)
            .TrangThai("Hoàn tiền", d => d.TrangThaiHoanTien, Nhan.TrangThaiHoanTien, 105);
        _dong.Them("Mã SP", c => c.MaSp, 70).Them("Sản phẩm", c => $"{c.TenSp} - {c.MauSac} - {c.KichCo}", 200, fill: true)
            .So("SL", c => c.SoLuong, 45).Tien("Đơn giá", c => c.DonGia, 95).Tien("Thành tiền", c => c.ThanhTien, 105);
        _chiTiet = new KhungChiTiet("Chi tiết đơn", _dong.Grid, "Sản phẩm trong đơn");
        _chiTiet.ThemVaoThongTin(_ruiRo);

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 62));

        _trangThai.SelectedIndexChanged += (_, _) => { if (!_dangNap) Tai(); };
        _nguon.SelectedIndexChanged += (_, _) => { if (!_dangNap) Tai(); };
        _tim.CanTim += Loc;
        _luoi.ChonThayDoi += HienChiTiet;
        CapNhatNut(null);
    }

    protected override void NhanThamSo(string thamSo)
    {
        _dangNap = true;
        if (thamSo.StartsWith("DH"))
        {
            _chonSau = thamSo;
            Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiDon).Append(new Muc("XIN_HUY", "Khách xin hủy")));
        }
        else Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiDon).Append(new Muc("XIN_HUY", "Khách xin hủy")), thamSo);
        _dangNap = false;
    }

    public override void LamMoi() => Tai();

    private void Tai()
    {
        var tt = Muc.Lay(_trangThai);
        var nguon = Muc.Lay(_nguon);
        DateTime? tu = _tu.Checked ? _tu.Value.Date : null, den = _den.Checked ? _den.Value.Date : null;
        if (!ThaoTac.Chay("Danh sách đơn hàng", () => tt == "XIN_HUY"
                ? new DonDatHangBLL().LayDanhSach(null, tu, den, nguon, "CHO_DUYET")
                : new DonDatHangBLL().LayDanhSach(tt, tu, den, nguon), out var ds)) return;
        _tatCa = ds;
        Loc();
        if (_chonSau != null)
        {
            var ma = _chonSau;
            _chonSau = null;
            _luoi.ChonDong(d => d.MaDon == ma);
        }
    }

    private void Loc()
    {
        var tk = _tim.TuKhoa;
        var ds = tk.Length == 0
            ? _tatCa
            : _tatCa.Where(d => $"{d.MaDon} {d.TenKhachHang} {d.TenNguoiNhan} {d.SdtNhan} {d.MaHoaDon}".Contains(tk, StringComparison.CurrentCultureIgnoreCase)).ToList();
        _luoi.Nap(ds);
        _tim.BaoKetQua(ds.Count);
    }

    private void CapNhatNut(DonDatHangDTO? d)
    {
        _xacNhan.Enabled = _tuChoi.Enabled = d?.TrangThai == "CHO_XAC_NHAN";
        _giao.Enabled = d is { TrangThai: "DA_XAC_NHAN" } && d.YeuCauHuy != "CHO_DUYET";
        _hoanTat.Enabled = d?.TrangThai == "DANG_GIAO";
        _huy.Enabled = d?.TrangThai is "CHO_XAC_NHAN" or "DA_XAC_NHAN" or "DANG_GIAO";
        _duyetHuy.Enabled = _tuChoiHuy.Enabled = d?.YeuCauHuy == "CHO_DUYET" && d.TrangThai == "DA_XAC_NHAN";
        _pdf.Enabled = d != null;
    }

    private void HienChiTiet(DonDatHangDTO? d)
    {
        CapNhatNut(d);
        if (d == null)
        {
            _chiTiet.ThongTin.Xoa();
            _ruiRo.Xoa("");
            _dong.Nap(Array.Empty<ChiTietDonDatHangDTO>());
            return;
        }
        _chiTiet.TieuDe.Text = $"Đơn {d.MaDon} · {Nhan.Ten(Nhan.TrangThaiDon, d.TrangThai)}";
        var dongs = new List<(string, string?, Color?)>
        {
            ("Khách hàng", $"{d.MaKh} - {d.TenKhachHang}", null),
            ("Người nhận", $"{d.TenNguoiNhan ?? d.TenKhachHang} · {d.SdtNhan}" + (d.EmailNhan != null ? $" · {d.EmailNhan}" : ""), null),
            ("Địa chỉ giao", d.DiaChiGiao, null),
            ("Thanh toán", Nhan.Ten(Nhan.PhuongThuc, d.PhuongThucTt), null),
            ("Giảm giá", d.TienGiam > 0 ? $"{GiaoDien.Tien(d.TienGiam)}" + (d.MaVoucher != null ? $" (voucher {d.MaVoucher})" : d.MaKm != null ? $" ({d.MaKm})" : "") : "Không", null),
            ("Tổng tiền", GiaoDien.Tien(d.TongTien), null),
            ("Hóa đơn", d.MaHoaDon ?? "Chưa lập (lập khi xác nhận)", null),
            ("Nhân viên xử lý", d.TenNhanVien, null),
        };
        if (d.YeuCauHuy != null || d.LyDoHuy != null)
            dongs.Add(("Hủy / xin hủy", $"{Nhan.Ten(Nhan.YeuCauHuy, d.YeuCauHuy)} {d.LyDoHuy}".Trim() + (d.NguoiHuy != null ? $" (bởi {(d.NguoiHuy == "KHACH" ? "khách" : "cửa hàng")})" : ""),
                GiaoDien.Do));
        if (d.PhanHoiHuy != null) dongs.Add(("Phản hồi của shop", d.PhanHoiHuy, null));
        if (d.TrangThaiHoanTien != null) dongs.Add(("Hoàn tiền", $"{Nhan.Ten(Nhan.TrangThaiHoanTien, d.TrangThaiHoanTien)} · {GiaoDien.Tien(d.SoTienHoan)}", Nhan.Mau(Nhan.TrangThaiHoanTien, d.TrangThaiHoanTien)));
        _chiTiet.ThongTin.Dat(dongs.ToArray());
        _ruiRo.Xoa("Đang tải độ tin cậy SĐT…");

        var ma = d.MaDon;
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaDon != ma) return;
            if (!ThaoTac.Chay("Chi tiết đơn + độ tin cậy SĐT", () => (new DonDatHangBLL().LayTheoMa(ma), new KhachHangBLL().RuiRo(d.SdtNhan, d.MaKh)),
                    out var kq)) return;
            _dong.Nap(kq.Item1.ChiTiets);
            var r = kq.Item2;
            _ruiRo.Dat(("Độ tin cậy SĐT", $"{Nhan.Ten(Nhan.RuiRo, r.MucRuiRo)} · {r.GhiChu}", Nhan.Mau(Nhan.RuiRo, r.MucRuiRo)),
                ("Lịch sử", $"{r.SoDonHoanTat} hoàn tất · {r.SoDonBiTuChoi} bị từ chối · {r.SoLanBomHang} bom hàng · COD: {(r.DuocDatCod ? "được" : "không")}", null));
        });
    }

    // Moi thao tac doi trang thai: chay BLL, tai lai danh sach, giu dong dang chon
    private void Lam(string ten, Action<string> lam, string? hoi = null)
    {
        if (_luoi.Chon is not { } d) return;
        if (hoi != null && !HopThoai.Hoi(hoi.Replace("{ma}", d.MaDon))) return;
        if (ThaoTac.Chay(ten, () => lam(d.MaDon))) Tai();
    }

    private void XacNhan()
    {
        if (_luoi.Chon is not { } d) return;
        if (!HopThoai.Hoi($"Xác nhận đơn {d.MaDon} ({GiaoDien.Tien(d.TongTien)})?\nKho bị trừ ngay và hóa đơn được lập"
                          + (d.PhuongThucTt == "COD" ? " (chờ thanh toán đến khi giao xong)." : " (đã thanh toán)."))) return;
        if (!ThaoTac.Chay("Xác nhận đơn", () => new DonDatHangBLL().XacNhan(d.MaDon, Phien.MaNv), out var hd)) return;
        Tai();
        if (HopThoai.Hoi($"Đã lập hóa đơn {hd.MaHoaDon}. In phiếu giao hàng (PDF đơn hàng)?")) InPdf();
    }

    private void TuChoi() => Lam("Từ chối đơn", ma => new DonDatHangBLL().TuChoi(ma, Phien.MaNv),
        "Từ chối đơn {ma}? Đơn chưa trừ kho, chưa có hóa đơn.");

    private void GiaoHang() => Lam("Giao hàng", ma => new DonDatHangBLL().GiaoHang(ma, Phien.MaNv), "Chuyển đơn {ma} sang đang giao?");

    private void HoanTat() => Lam("Hoàn tất đơn", ma => new DonDatHangBLL().HoanTat(ma, Phien.MaNv),
        "Khách đã nhận đơn {ma}? Hóa đơn COD chuyển sang đã thanh toán, khách được cộng điểm.");

    private void Huy()
    {
        if (_luoi.Chon is not { } d) return;
        var canhBao = d.YeuCauHuy == "CHO_DUYET"
            ? "Khách đang xin hủy: hủy lúc này được tính là duyệt yêu cầu (không tính bom hàng)."
            : d.MaHoaDon != null
                ? "Đơn đã lập hóa đơn: hủy sẽ trả hàng về kho, hủy hóa đơn, tạo phiếu hoàn tiền nếu khách đã trả trước, và TÍNH 1 LẦN BOM HÀNG cho SĐT."
                : "Đơn chưa xác nhận nên chưa trừ kho.";
        var lyDo = HopThoai.NhapChuoi(FindForm(), $"Hủy đơn {d.MaDon}", "Lý do hủy", false,
            goiY: new[] { "Khách không nhận hàng", "Không liên lạc được khách", "Khách yêu cầu hủy qua hotline", "Hết hàng", "Sai địa chỉ giao" });
        if (lyDo == null || !HopThoai.Hoi($"{canhBao}\n\nHủy đơn {d.MaDon}?")) return;
        if (ThaoTac.Chay("Hủy đơn", () => new DonDatHangBLL().NhanVienHuy(d.MaDon, Phien.MaNv, lyDo))) Tai();
    }

    private void DuyetHuy() => Lam("Duyệt yêu cầu hủy", ma => new DonDatHangBLL().DuyetHuy(ma, Phien.MaNv),
        "Đồng ý cho khách hủy đơn {ma}?\nHàng về kho, hóa đơn bị hủy, tạo phiếu hoàn tiền nếu đã thanh toán. Không tính bom hàng.");

    private void TuChoiHuy()
    {
        if (_luoi.Chon is not { } d) return;
        var lyDo = HopThoai.NhapChuoi(FindForm(), $"Từ chối yêu cầu hủy đơn {d.MaDon}", "Lý do (khách sẽ thấy)", true,
            goiY: new[] { "Đơn đã được đóng gói và chuẩn bị giao", "Đơn đã bàn giao cho đơn vị vận chuyển" });
        if (lyDo == null) return;
        if (ThaoTac.Chay("Từ chối yêu cầu hủy", () => new DonDatHangBLL().TuChoiHuy(d.MaDon, Phien.MaNv, lyDo))) Tai();
    }

    private void InPdf()
    {
        if (_luoi.Chon is not { } d) return;
        var ma = d.MaDon;
        ThaoTac.Chay("In đơn hàng", () => XuatFile.MoPdf(new PdfBLL().DonDatHang(new DonDatHangBLL().LayTheoMa(ma)), ma));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}

// ===================================================================== HOA DON (main-sub, yeu cau 4)
public sealed class HoaDonMH : ManHinh
{
    private readonly OTimKiem _tim = new("Mã HĐ (HD00012) hoặc tên khách");
    private readonly DateTimePicker _tu = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly DateTimePicker _den = GiaoDien.Ngay(coTheBoTrong: true);
    private readonly ComboBox _trangThai = GiaoDien.Chon(150);
    private readonly Luoi<HoaDonDTO> _luoi = new() { Khoa = h => h.MaHoaDon };
    private readonly Luoi<ChiTietHoaDonDTO> _dong = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly Label _tong = GiaoDien.Nhan("", GiaoDien.ChuDam);
    private readonly HenGio _hen = new();
    private readonly Button _huy, _doiTra;
    private List<HoaDonDTO> _tatCa = new();
    private string? _chonSau;

    public override Control OTim => _tim.O;

    public HoaDonMH() : base("Hóa đơn", "Main-sub: danh sách hóa đơn 1 câu SQL, chọn 1 dòng chỉ chạy thêm 1 câu lấy chi tiết (xem thanh trạng thái).")
    {
        Nut("Lập hóa đơn tại quầy", Keys.Control | Keys.N, LapHoaDon, chinh: true);
        _doiTra = Nut("Lập đổi trả", Keys.Control | Keys.D, LapDoiTra);
        _huy = Nut("Hủy hóa đơn", Keys.Delete, Huy, nguyHiem: true);
        KhoangCach();
        Nut("In PDF", Keys.Control | Keys.P, InPdf);
        Nut("Xuất Excel", Keys.Control | Keys.E, () =>
        {
            var ds = _luoi.DuLieu;
            if (ThaoTac.Chay("Xuất Excel hóa đơn", () => new ExcelBLL().XuatHoaDon(ds), out var f)) XuatFile.LuuExcel(FindForm(), f, "HoaDon");
        });
        Nut("Làm mới", Keys.F5, LamMoi);
        Phim("Đổi chế độ tìm kiếm", Keys.F3, _tim.DoiCheDo);

        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiHoaDon));
        _tu.Checked = _den.Checked = false;
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { _tim, GiaoDien.Nhan("Từ"), _tu, GiaoDien.Nhan("đến"), _den, GiaoDien.Nhan("Trạng thái"), _trangThai });
        foreach (var n in new[] { _tu, _den }) n.ValueChanged += (_, _) => Loc();
        _trangThai.SelectedIndexChanged += (_, _) => Loc();

        _luoi.Them("Mã HĐ", h => h.MaHoaDon, 80).Them("Mã đơn", h => h.MaDon ?? "Tại quầy", 80, mau: h => h.MaDon == null ? GiaoDien.ChuPhu : null)
            .Them("Mã KH", h => h.MaKh ?? "Vãng lai", 72, mau: h => h.MaKh == null ? GiaoDien.ChuPhu : null).Them("Mã NV", h => h.MaNv, 70)
            .Ngay("Ngày lập", h => h.NgayLap, 90)
            .Them("Khách hàng", h => h.TenKhachHang ?? "Khách vãng lai", 150, fill: true, mau: h => h.MaKh == null ? GiaoDien.ChuPhu : null)
            .Them("Nhân viên", h => h.TenNhanVien, 130)
            .Them("Thanh toán", h => Nhan.Ten(Nhan.PhuongThuc, h.PhuongThucTt), 105).Tien("Giảm", h => h.TienGiam, 85)
            .Tien("Tổng tiền", h => h.TongTien, 105).TrangThai("Trạng thái", h => h.TrangThai, Nhan.TrangThaiHoaDon, 110);
        _luoi.MoDong += _ => InPdf();
        _dong.Them("Mã SP", c => c.MaSp, 70).Them("Sản phẩm", c => $"{c.TenSp} - {c.MauSac} - {c.KichCo}", 200, fill: true)
            .So("SL", c => c.SoLuong, 45).Tien("Đơn giá bán", c => c.DonGiaBan, 100).Tien("Thành tiền", c => c.ThanhTien, 105);
        _chiTiet = new KhungChiTiet("Chi tiết hóa đơn", _dong.Grid, "Các dòng hàng (sub)");

        var trai = new Panel { Dock = DockStyle.Fill };
        _tong.Dock = DockStyle.Bottom;
        _tong.Padding = new Padding(4, 6, 0, 2);
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        trai.Controls.Add(_tong);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 62));
        _tim.CanTim += Loc;
        _luoi.ChonThayDoi += HienChiTiet;
    }

    protected override void NhanThamSo(string thamSo)
    {
        if (thamSo.StartsWith("HD")) _chonSau = thamSo;
    }

    public override void LamMoi()
    {
        if (!ThaoTac.Chay("Danh sách hóa đơn", () => new HoaDonBLL().LayDanhSach(), out var ds)) return;
        _tatCa = ds;
        Loc();
        if (_chonSau != null)
        {
            var ma = _chonSau;
            _chonSau = null;
            _luoi.ChonDong(h => h.MaHoaDon == ma);
        }
    }

    // Yeu cau 2 + 6: cung 1 bo dieu kien (ghep AND), che do tai su dung loc tren _tatCa, che do truy van moi chay SELECT moi
    private TimKiemNangCaoDTO DieuKien()
    {
        var dk = new TimKiemNangCaoDTO { KhopTatCa = true };
        var tk = _tim.TuKhoa;
        if (tk.Length > 0) dk.DieuKiens.Add(TuKhoa.Dk(TuKhoa.LaMa(tk, "HD") ? "ma_hoa_don" : "ten_khach_hang", "LIKE", tk));
        if (_tu.Checked) dk.DieuKiens.Add(TuKhoa.Dk("ngay_lap", ">=", _tu.Value.Date));
        if (_den.Checked) dk.DieuKiens.Add(TuKhoa.Dk("ngay_lap", "<=", _den.Value.Date));
        if (Muc.Lay(_trangThai) is { } t) dk.DieuKiens.Add(TuKhoa.Dk("trang_thai", "=", t));
        return dk;
    }

    private void Loc()
    {
        var dk = DieuKien();
        var taiSuDung = _tim.TaiSuDung;
        if (!ThaoTac.Chay(taiSuDung ? "Lọc hóa đơn (tái sử dụng)" : "Tìm hóa đơn (truy vấn mới)", () =>
            {
                if (dk.DieuKiens.Count == 0) return taiSuDung ? _tatCa : new HoaDonBLL().LayDanhSach();
                return taiSuDung ? new TimKiemBLL().LocHoaDon(_tatCa, dk) : new TimKiemBLL().TimHoaDon(dk);
            }, out var ds)) return;
        _luoi.Nap(ds);
        _tim.BaoKetQua(ds.Count);
        var daTra = ds.Where(h => h.TrangThai == "DA_THANH_TOAN").ToList();
        _tong.Text = $"{ds.Count:N0} hóa đơn · đã thanh toán {daTra.Count:N0} HĐ = {GiaoDien.Tien(daTra.Sum(h => h.TongTien))}";
    }

    private void HienChiTiet(HoaDonDTO? h)
    {
        _huy.Enabled = h != null && h.MaDon == null && h.TrangThai != "DA_HUY";
        _doiTra.Enabled = h?.TrangThai == "DA_THANH_TOAN";
        if (h == null)
        {
            _chiTiet.ThongTin.Xoa();
            _dong.Nap(Array.Empty<ChiTietHoaDonDTO>());
            return;
        }
        _chiTiet.TieuDe.Text = $"Hóa đơn {h.MaHoaDon}";
        _chiTiet.ThongTin.Dat(("Ngày lập", GiaoDien.NgayVn(h.NgayLap), null), ("Khách hàng", h.MaKh == null ? "Khách vãng lai" : $"{h.MaKh} - {h.TenKhachHang}", null),
            ("Nhân viên lập", $"{h.MaNv} - {h.TenNhanVien}", null), ("Nguồn", h.MaDon == null ? "Bán tại quầy" : $"Đơn online {h.MaDon}", null),
            ("Thanh toán", Nhan.Ten(Nhan.PhuongThuc, h.PhuongThucTt), null),
            ("Khuyến mãi", h.MaKm == null ? "Không" : $"{h.MaKm} {h.TenKm} · giảm {GiaoDien.Tien(h.TienGiam)}", null),
            ("Tổng tiền", GiaoDien.Tien(h.TongTien), null),
            ("Trạng thái", Nhan.Ten(Nhan.TrangThaiHoaDon, h.TrangThai), Nhan.Mau(Nhan.TrangThaiHoaDon, h.TrangThai)));
        var ma = h.MaHoaDon;
        // Yeu cau 4: bam 1 dong -> chi 1 cau SQL lay chi tiet
        _hen.Hen(() =>
        {
            if (_luoi.Chon?.MaHoaDon == ma && ThaoTac.Chay("Chi tiết hóa đơn (sub)", () => new HoaDonBLL().LayChiTiet(ma), out var ds))
                _dong.Nap(ds);
        });
    }

    private void LapHoaDon()
    {
        if (!ThaoTac.Chay("Chuẩn bị bán hàng", () => new SanPhamBLL().LayTatCa(true), out var sps)) return;
        using var f = new FormLapHoaDon(sps);
        if (f.ShowDialog(FindForm()) != DialogResult.OK || f.KetQua is not { } hd) return;
        _chonSau = hd.MaHoaDon;
        LamMoi();
    }

    private void LapDoiTra()
    {
        if (_luoi.Chon is not { } h) return;
        using var f = new FormLapDoiTra(h.MaHoaDon);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void Huy()
    {
        if (_luoi.Chon is not { } h) return;
        if (!HopThoai.Hoi($"Hủy hóa đơn tại quầy {h.MaHoaDon} ({GiaoDien.Tien(h.TongTien)})?\nHàng trả về kho, điểm của khách được tính lại.")) return;
        if (ThaoTac.Chay("Hủy hóa đơn", () => new HoaDonBLL().Huy(h.MaHoaDon))) LamMoi();
    }

    private void InPdf()
    {
        if (_luoi.Chon is not { } h) return;
        var ma = h.MaHoaDon;
        ThaoTac.Chay("In hóa đơn", () => XuatFile.MoPdf(new PdfBLL().HoaDon(new HoaDonBLL().LayTheoMa(ma)), ma));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hen.Dispose();
        base.Dispose(disposing);
    }
}

// ===================================================================== DOI TRA
public sealed class DoiTraMH : ManHinh
{
    private readonly ComboBox _trangThai = GiaoDien.Chon(150);
    private readonly Luoi<DoiTraDTO> _luoi = new() { Khoa = d => d.MaDoiTra };
    private readonly Luoi<ChiTietDoiTraDTO> _dong = new();
    private readonly KhungChiTiet _chiTiet;
    private readonly Button _duyet, _tuChoi, _hoanTat;
    private bool _dangNap;

    public DoiTraMH() : base("Đổi trả", $"Đổi / trả trong {QuyTac.SoNgayDoiTra} ngày kể từ ngày lập hóa đơn. Trả hàng hoàn theo giá thực trả (đã trừ giảm giá hóa đơn).")
    {
        Nut("Lập phiếu tại quầy", Keys.Control | Keys.N, Lap, chinh: true);
        _duyet = Nut("Duyệt", Keys.Alt | Keys.X, () => Lam("Duyệt phiếu đổi trả", ma => new DoiTraBLL().Duyet(ma, Phien.MaNv), "Duyệt phiếu {ma}? Khách mang hàng đến để hoàn tất."));
        _tuChoi = Nut("Từ chối", Keys.Alt | Keys.T, () => Lam("Từ chối phiếu đổi trả", ma => new DoiTraBLL().TuChoi(ma, Phien.MaNv), "Từ chối phiếu {ma}?"));
        _hoanTat = Nut("Nhận hàng & hoàn tất", Keys.Alt | Keys.H, HoanTat);
        KhoangCach();
        Nut("In PDF", Keys.Control | Keys.P, InPdf);
        Nut("Làm mới", Keys.F5, LamMoi);

        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiDoiTra));
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { GiaoDien.Nhan("Trạng thái"), _trangThai });
        _trangThai.SelectedIndexChanged += (_, _) => { if (!_dangNap) LamMoi(); };

        _luoi.Them("Mã phiếu", d => d.MaDoiTra, 85).Them("Mã HĐ", d => d.MaHoaDon, 80).Ngay("Ngày", d => d.NgayDoiTra)
            .Them("Khách hàng", d => d.TenKhachHang ?? "Khách vãng lai", 150, fill: true).Them("Loại", d => Nhan.Ten(Nhan.LoaiDoiTra, d.Loai), 110)
            .TrangThai("Trạng thái", d => d.TrangThai, Nhan.TrangThaiDoiTra, 100).Tien("Tiền hoàn", d => d.TienHoan, 100)
            .Them("Nhân viên", d => d.TenNhanVien, 130);
        _dong.Them("Mã SP", c => c.MaSp, 70).Them("Sản phẩm", c => c.TenSp, 180, fill: true).So("SL", c => c.SoLuong, 45)
            .Them("Đổi sang", c => c.MaSpDoi == null ? "" : $"{c.MaSpDoi} {c.TenSpDoi}", 170);
        _chiTiet = new KhungChiTiet("Chi tiết phiếu", _dong.Grid, "Sản phẩm đổi / trả");

        var trai = new Panel { Dock = DockStyle.Fill };
        trai.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        trai.Controls.Add(loc);
        NoiDung.Controls.Add(BoCuc.ChiaDoi(trai, _chiTiet, 62));
        _luoi.ChonThayDoi += HienChiTiet;
    }

    protected override void NhanThamSo(string thamSo)
    {
        _dangNap = true;
        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiDoiTra), thamSo);
        _dangNap = false;
    }

    public override void LamMoi()
    {
        var tt = Muc.Lay(_trangThai);
        // DoiTraBLL.LayDanhSach da kem chi tiet tung phieu nen ben phai khong can chay them SQL
        if (ThaoTac.Chay("Danh sách đổi trả", () => new DoiTraBLL().LayDanhSach(tt), out var ds)) _luoi.Nap(ds);
    }

    private void HienChiTiet(DoiTraDTO? d)
    {
        _duyet.Enabled = _tuChoi.Enabled = d?.TrangThai == "CHO_DUYET";
        _hoanTat.Enabled = d?.TrangThai is "CHO_DUYET" or "DA_DUYET";
        if (d == null)
        {
            _chiTiet.ThongTin.Xoa();
            _dong.Nap(Array.Empty<ChiTietDoiTraDTO>());
            return;
        }
        _chiTiet.TieuDe.Text = $"Phiếu {d.MaDoiTra} · {Nhan.Ten(Nhan.LoaiDoiTra, d.Loai)}";
        _chiTiet.ThongTin.Dat(("Hóa đơn", d.MaHoaDon, null), ("Khách hàng", d.TenKhachHang ?? "Khách vãng lai", null), ("Lý do", d.LyDo, null),
            ("Ngày yêu cầu", GiaoDien.NgayVn(d.NgayDoiTra), null),
            ("Trạng thái", Nhan.Ten(Nhan.TrangThaiDoiTra, d.TrangThai), Nhan.Mau(Nhan.TrangThaiDoiTra, d.TrangThai)),
            ("Tiền hoàn", d.Loai == "TRA" ? GiaoDien.Tien(d.TienHoan) + (d.NgayHoan != null ? $" ngày {d.NgayHoan:dd/MM/yyyy}" : "") : "Không (đổi hàng)", null),
            ("Nhân viên xử lý", d.TenNhanVien, null));
        _dong.Nap(d.ChiTiets);
    }

    private void Lam(string ten, Action<string> lam, string hoi)
    {
        if (_luoi.Chon is not { } d || !HopThoai.Hoi(hoi.Replace("{ma}", d.MaDoiTra))) return;
        if (ThaoTac.Chay(ten, () => lam(d.MaDoiTra))) LamMoi();
    }

    private void HoanTat()
    {
        if (_luoi.Chon is not { } d) return;
        if (!HopThoai.Hoi($"Đã nhận hàng của phiếu {d.MaDoiTra}?\nHàng trả về kho"
                          + (d.Loai == "DOI" ? ", xuất sản phẩm đổi cho khách." : ", hoàn tiền theo giá thực trả."))) return;
        if (!ThaoTac.Chay("Hoàn tất đổi trả", () => new DoiTraBLL().HoanTat(d.MaDoiTra, Phien.MaNv), out var kq)) return;
        if (kq.TienHoan > 0) HopThoai.ThongBao($"Hoàn cho khách {GiaoDien.Tien(kq.TienHoan)}.");
        LamMoi();
    }

    private void Lap()
    {
        using var f = new FormLapDoiTra(null);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) LamMoi();
    }

    private void InPdf()
    {
        if (_luoi.Chon is not { } d) return;
        var ma = d.MaDoiTra;
        ThaoTac.Chay("In phiếu đổi trả", () => XuatFile.MoPdf(new PdfBLL().DoiTra(new DoiTraBLL().LayTheoMa(ma)), ma));
    }
}

// ===================================================================== HOAN TIEN
public sealed class HoanTienMH : ManHinh
{
    private readonly ComboBox _trangThai = GiaoDien.Chon(150);
    private readonly Luoi<HoanTienDTO> _luoi = new() { Khoa = h => h.MaHoanTien };
    private readonly Label _tong = GiaoDien.Nhan("", GiaoDien.ChuDam);
    private readonly Button _daHoan;
    private bool _dangNap;

    public HoanTienMH() : base("Hoàn tiền", "Đơn đã chuyển khoản / thanh toán online bị hủy thì hệ thống tự tạo phiếu chờ hoàn. Đánh dấu đã hoàn sau khi chuyển tiền lại cho khách.")
    {
        _daHoan = Nut("Đánh dấu đã hoàn", Keys.Alt | Keys.X, DaHoan, chinh: true);
        Nut("Xem đơn", Keys.Return, () => { if (_luoi.Chon is { } h) MoManHinh?.Invoke("DonHang", h.MaDon); });
        Nut("Làm mới", Keys.F5, LamMoi);
        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiHoanTien), "CHO_HOAN");
        var loc = GiaoDien.HangNgang();
        loc.Controls.AddRange(new Control[] { GiaoDien.Nhan("Trạng thái"), _trangThai, _tong });
        _trangThai.SelectedIndexChanged += (_, _) => { if (!_dangNap) LamMoi(); };
        _luoi.Them("Mã phiếu", h => h.MaHoanTien, 85).Them("Mã đơn", h => h.MaDon, 80).Them("Mã HĐ", h => h.MaHoaDon, 80)
            .Them("Khách hàng", h => h.TenKhachHang, 150, fill: true).Them("SĐT", h => h.SdtNhan, 100).Tien("Số tiền", h => h.SoTien, 110)
            .Them("Phương thức", h => Nhan.Ten(Nhan.PhuongThuc, h.PhuongThuc), 120)
            .TrangThai("Trạng thái", h => h.TrangThai, Nhan.TrangThaiHoanTien, 110).Ngay("Ngày tạo", h => h.NgayTao, 120, coGio: true)
            .Ngay("Ngày hoàn", h => h.NgayHoan, 120, coGio: true).Them("Người hoàn", h => h.TenNhanVien, 120).Them("Ghi chú", h => h.GhiChu, 220);
        _luoi.ChonThayDoi += h => _daHoan.Enabled = h?.TrangThai == "CHO_HOAN";
        NoiDung.Controls.Add(BoCuc.KhungBang(_luoi.Grid));
        NoiDung.Controls.Add(loc);
    }

    protected override void NhanThamSo(string thamSo)
    {
        _dangNap = true;
        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiHoanTien), thamSo);
        _dangNap = false;
    }

    public override void LamMoi()
    {
        var tt = Muc.Lay(_trangThai);
        if (!ThaoTac.Chay("Danh sách hoàn tiền", () => new HoanTienBLL().LayDanhSach(tt), out var ds)) return;
        _luoi.Nap(ds);
        var cho = ds.Where(h => h.TrangThai == "CHO_HOAN").ToList();
        _tong.Text = cho.Count == 0 ? "" : $"   Chờ hoàn: {cho.Count} phiếu · {GiaoDien.Tien(cho.Sum(h => h.SoTien))}";
    }

    private void DaHoan()
    {
        if (_luoi.Chon is not { TrangThai: "CHO_HOAN" } h) return;
        var ghiChu = HopThoai.NhapChuoi(FindForm(), $"Đã hoàn {GiaoDien.Tien(h.SoTien)} cho {h.TenKhachHang}", "Ghi chú đối soát (mã giao dịch, ngân hàng...)", false,
            goiY: new[] { "Đã chuyển khoản lại cho khách", "Đã hoàn qua cổng thanh toán online", "Đã hoàn tiền mặt tại quầy" });
        if (ghiChu == null) return;
        if (ThaoTac.Chay("Đánh dấu đã hoàn tiền", () => new HoanTienBLL().DaHoan(h.MaHoanTien, Phien.MaNv, ghiChu), out _)) LamMoi();
    }
}
