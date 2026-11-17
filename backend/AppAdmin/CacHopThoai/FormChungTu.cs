using System.ComponentModel;

namespace AppAdmin.CacHopThoai;

// Khung chung cho hop thoai lap chung tu: vung tren (thong tin), bang dong hang o giua, tong tien + nut o duoi
public abstract class FormChungTu : Form
{
    // Cac hang thong tin xep tu tren xuong theo thu tu them vao
    protected FlowLayoutPanel Tren { get; } = new()
    {
        Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown,
        WrapContents = false, Padding = new Padding(0, 0, 0, 8)
    };
    protected Panel Giua { get; } = new() { Dock = DockStyle.Fill };
    protected FlowLayoutPanel Duoi { get; } = new() { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
    protected Button NutLuu { get; }

    protected FormChungTu(string tieuDe, string chuNutLuu, Size kichThuoc)
    {
        Text = tieuDe;
        Font = GiaoDien.Chu;
        BackColor = Color.White;
        ClientSize = kichThuoc;
        MinimumSize = new Size(kichThuoc.Width - 100, kichThuoc.Height - 100);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        KeyPreview = true;
        Padding = new Padding(14);
        NutLuu = GiaoDien.NutChinh(chuNutLuu);
        NutLuu.Click += (_, _) => BamLuu();
        var huy = GiaoDien.NutPhu("Đóng  (Esc)");
        huy.DialogResult = DialogResult.Cancel;
        CancelButton = huy;
        Duoi.Controls.Add(NutLuu);
        Duoi.Controls.Add(huy);
        Controls.Add(Giua);
        Controls.Add(Tren);
        Controls.Add(Duoi);
    }

    protected abstract bool Luu();

    private void BamLuu()
    {
        if (NutLuu.Enabled && Luu()) DialogResult = DialogResult.OK;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is (Keys.Control | Keys.S) or Keys.F9)
        {
            BamLuu();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // Bang dong hang sua duoc so luong / don gia
    protected static DataGridView BangDong<T>(BindingList<T> ds, params (string ThuocTinh, string TieuDe, int Rong, bool Sua, string? DinhDang)[] cots)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill, AutoGenerateColumns = false, RowHeadersVisible = false, AllowUserToAddRows = false,
            AllowUserToDeleteRows = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, EnableHeadersVisualStyles = false,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2, Font = GiaoDien.Chu, StandardTab = true
        };
        g.RowTemplate.Height = 28;
        g.ColumnHeadersDefaultCellStyle.Font = GiaoDien.ChuDam;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
        g.DefaultCellStyle.SelectionBackColor = GiaoDien.DongChon;
        g.DefaultCellStyle.SelectionForeColor = GiaoDien.ChuChinh;
        foreach (var c in cots)
        {
            var cot = new DataGridViewTextBoxColumn { DataPropertyName = c.ThuocTinh, HeaderText = c.TieuDe, Width = c.Rong, ReadOnly = !c.Sua };
            cot.DefaultCellStyle.FormatProvider = GiaoDien.Vn;
            if (c.DinhDang != null)
            {
                cot.DefaultCellStyle.Format = c.DinhDang;
                cot.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (c.Sua) cot.DefaultCellStyle.BackColor = Color.FromArgb(0xFF, 0xFB, 0xE6);
            if (c.Rong == 0) cot.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            g.Columns.Add(cot);
        }
        g.DataSource = ds;
        g.DataError += (_, e) =>
        {
            e.ThrowException = false;
            MessageBox.Show("Giá trị không hợp lệ, vui lòng nhập số.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        return g;
    }

    protected static Label NhanTong() => new()
    {
        AutoSize = true, Font = GiaoDien.TieuDeNho, ForeColor = GiaoDien.ChuChinh, Margin = new Padding(0, 8, 24, 0)
    };
}

// ===================================================================== PHIEU NHAP
// Lap phieu nhap: chon NCC, them dong (chon SP hoac doc file Excel), PhieuNhapBLL.Lap cong ton kho trong 1 giao dich
public sealed class FormLapPhieuNhap : FormChungTu
{
    private readonly List<SanPhamDTO> _sanPhams;
    private readonly ComboBox _ncc = GiaoDien.Chon(360);
    private readonly BindingList<ChiTietPhieuNhapDTO> _dong = new();
    private readonly DataGridView _bang;
    private readonly Label _tong = NhanTong();
    private bool _daNhacNhap;

    public PhieuNhapDTO? KetQua { get; private set; }

    public FormLapPhieuNhap(List<NhaCungCapDTO> nccs, List<SanPhamDTO> sanPhams)
        : base("Lập phiếu nhập kho", "Lưu phiếu nhập  (Ctrl+S)", new Size(940, 620))
    {
        _sanPhams = sanPhams;
        Muc.Nap(_ncc, nccs.Where(n => n.TrangThai == "HOP_TAC").Select(n => new Muc(n.MaNcc, $"{n.MaNcc} - {n.TenNcc}")));

        var hang = GiaoDien.HangNgang();
        hang.Controls.Add(GiaoDien.Nhan("Nhà cung cấp *"));
        hang.Controls.Add(_ncc);
        hang.Controls.Add(GiaoDien.Nhan($"Người lập: {Phien.HoTen} · Ngày nhập: {DateTime.Today:dd/MM/yyyy}", mau: GiaoDien.ChuPhu));
        var hang2 = GiaoDien.HangNgang();
        var them = GiaoDien.NutPhu("Thêm sản phẩm  (Insert)");
        them.Click += (_, _) => ThemSanPham();
        var doc = GiaoDien.NutPhu("Đọc từ Excel  (Ctrl+I)");
        doc.Click += (_, _) => DocExcel();
        var mau = GiaoDien.NutPhu("Tải file mẫu");
        mau.Click += (_, _) => XuatFile.LuuExcel(this, new ExcelBLL().MauChiTietPhieuNhap(), "Mau_ChiTietPhieuNhap");
        var xoa = GiaoDien.NutPhu("Xóa dòng  (Delete)");
        xoa.Click += (_, _) => XoaDong();
        hang2.Controls.AddRange(new Control[] { them, doc, mau, xoa });
        Tren.Controls.Add(hang);
        Tren.Controls.Add(hang2);

        _bang = BangDong(_dong, ("MaSp", "Mã SP", 80, false, null), ("TenSp", "Tên sản phẩm", 0, false, null),
            ("MauSac", "Màu", 100, false, null), ("KichCo", "Size", 60, false, null), ("SoLuong", "Số lượng", 90, true, "N0"),
            ("DonGiaNhap", "Đơn giá nhập", 120, true, "N0"), ("ThanhTien", "Thành tiền", 130, false, "N0"));
        _bang.CellEndEdit += (_, _) => TinhTong();
        _bang.UserDeletedRow += (_, _) => TinhTong();
        Giua.Controls.Add(_bang);
        Duoi.Controls.Add(_tong);
        TinhTong();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Insert) { ThemSanPham(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.I) { DocExcel(); e.Handled = true; }
        };
    }

    private void ThemDong(SanPhamDTO s, int soLuong, decimal donGia)
    {
        var cu = _dong.FirstOrDefault(d => d.MaSp == s.MaSp && d.DonGiaNhap == donGia);
        if (cu != null) cu.SoLuong += soLuong;
        else _dong.Add(new ChiTietPhieuNhapDTO { MaSp = s.MaSp, TenSp = s.TenSp, MauSac = s.MauSac, KichCo = s.KichCo, SoLuong = soLuong, DonGiaNhap = donGia });
        TinhTong();
    }

    private void ThemSanPham()
    {
        using var f = new FormChonSanPham(_sanPhams, nhieu: true, chiDangBan: false, "Chọn sản phẩm nhập kho");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        foreach (var s in f.DaChon) ThemDong(s, 1, 0);
        if (!_daNhacNhap)
        {
            _daNhacNhap = true;
            HopThoai.ThongBao("Nhập số lượng và đơn giá nhập cho các dòng vừa thêm (ô nền vàng: gõ số hoặc F2 để sửa, Enter để xong).");
        }
        _bang.Focus();
    }

    private void DocExcel()
    {
        var file = XuatFile.ChonFileExcel(this);
        if (file == null) return;
        if (!ThaoTac.Chay("Đọc Excel chi tiết phiếu nhập", () =>
            {
                using var s = File.OpenRead(file);
                return new ExcelBLL().DocChiTietPhieuNhap(s);
            }, out var kq)) return;
        using var xem = new FormXemTruocExcel<ChiTietPhieuNhapDTO>("Xem trước chi tiết phiếu nhập từ Excel", kq, l => l
            .Them("Mã SP", c => c.MaSp, 80).Them("Tên sản phẩm", c => c.TenSp, 250, fill: true).Them("Màu", c => c.MauSac, 90)
            .Them("Size", c => c.KichCo, 60).So("Số lượng", c => c.SoLuong).Tien("Đơn giá nhập", c => c.DonGiaNhap)
            .Tien("Thành tiền", c => c.ThanhTien), "Thêm vào phiếu");
        if (xem.ShowDialog(this) != DialogResult.OK) return;
        foreach (var c in kq.HopLe)
            ThemDong(new SanPhamDTO { MaSp = c.MaSp, TenSp = c.TenSp ?? "", MauSac = c.MauSac, KichCo = c.KichCo }, c.SoLuong, c.DonGiaNhap);
    }

    private void XoaDong()
    {
        if (_bang.CurrentRow?.DataBoundItem is ChiTietPhieuNhapDTO d) _dong.Remove(d);
        TinhTong();
    }

    private void TinhTong()
    {
        foreach (var d in _dong) d.ThanhTien = d.SoLuong * d.DonGiaNhap;
        _bang.Invalidate();
        _tong.Text = $"{_dong.Count} dòng · {_dong.Sum(d => d.SoLuong):N0} cái · Tổng tiền nhập: {GiaoDien.Tien(_dong.Sum(d => d.ThanhTien))}";
    }

    protected override bool Luu()
    {
        _bang.EndEdit();
        TinhTong();
        var ncc = Muc.Lay(_ncc);
        if (ncc == null)
        {
            HopThoai.Canh("Chọn nhà cung cấp (đang hợp tác)");
            return false;
        }
        var dongs = _dong.Select(d => new ChiTietPhieuNhapDTO { MaSp = d.MaSp, SoLuong = d.SoLuong, DonGiaNhap = d.DonGiaNhap }).ToList();
        if (dongs.Count == 0)
        {
            HopThoai.Canh("Phiếu nhập chưa có sản phẩm nào (Insert để thêm, Ctrl+I để đọc từ Excel).");
            return false;
        }
        if (!HopThoai.Hoi($"Lưu phiếu nhập {dongs.Count} dòng, tổng {GiaoDien.Tien(_dong.Sum(d => d.ThanhTien))}?\nTồn kho sẽ được cộng ngay và không sửa / hủy phiếu được."))
            return false;
        if (!ThaoTac.Chay("Lập phiếu nhập", () => new PhieuNhapBLL().Lap(ncc, Phien.MaNv, dongs), out var p)) return false;
        KetQua = p;
        if (HopThoai.Hoi($"Đã lập phiếu {p.MaPhieuNhap}. In phiếu nhập (PDF)?"))
            ThaoTac.Chay("In phiếu nhập", () => XuatFile.MoPdf(new PdfBLL().PhieuNhap(p), p.MaPhieuNhap));
        return true;
    }
}

// ===================================================================== BAN TAI QUAY
// Lap hoa don tai quay: HoaDonBLL.TinhTruoc de xem truoc tien, HoaDonBLL.BanTaiQuay lap hoa don da thanh toan + tru ton
public sealed class FormLapHoaDon : FormChungTu
{
    private sealed class DongBan
    {
        public string MaSp { get; set; } = "";
        public string TenSp { get; set; } = "";
        public int SoLuong { get; set; } = 1;
        public decimal DonGiaBan { get; set; }
        public decimal ThanhTien { get; set; }
        public int Ton { get; set; }
    }

    private readonly List<SanPhamDTO> _sanPhams;
    private readonly BindingList<DongBan> _dong = new();
    private readonly DataGridView _bang;
    private readonly TextBox _maSp = GiaoDien.O(160);
    private readonly TextBox _sdt = GiaoDien.O(150);
    private readonly Label _khach = GiaoDien.Nhan("Khách vãng lai (không tích điểm)", mau: GiaoDien.ChuPhu);
    private readonly TextBox _voucher = GiaoDien.O(140);
    private readonly ComboBox _phuongThuc = GiaoDien.Chon(150);
    private readonly NumericUpDown _khachDua = GiaoDien.So(1_000_000_000, 150);
    private readonly Label _tien = new() { AutoSize = true, Font = GiaoDien.ChuDam, Margin = new Padding(0, 6, 0, 0) };
    private readonly Label _loi = new() { AutoSize = true, ForeColor = GiaoDien.Do, MaximumSize = new Size(560, 0), Margin = new Padding(0, 6, 0, 0) };
    private readonly System.Windows.Forms.Timer _cho = new() { Interval = 400 };
    private KhachHangDTO? _kh;
    private BangTinhTienDTO? _bangTien;

    public HoaDonDTO? KetQua { get; private set; }

    public FormLapHoaDon(List<SanPhamDTO> sanPhams) : base("Lập hóa đơn bán tại quầy", "Thanh toán & lập hóa đơn  (F9)", new Size(1000, 680))
    {
        _sanPhams = sanPhams.Where(s => s.TrangThai == "DANG_BAN").ToList();
        Muc.Nap(_phuongThuc, new[] { new Muc("TIEN_MAT", "Tiền mặt"), new Muc("CHUYEN_KHOAN", "Chuyển khoản") });

        var hang1 = GiaoDien.HangNgang();
        hang1.Controls.Add(GiaoDien.Nhan("SĐT khách (F4)"));
        hang1.Controls.Add(_sdt);
        var tim = GiaoDien.NutPhu("Tìm");
        tim.Click += (_, _) => TimKhach();
        var bo = GiaoDien.NutPhu("Khách vãng lai");
        bo.Click += (_, _) => { _kh = null; _sdt.Clear(); HienKhach(); };
        hang1.Controls.AddRange(new Control[] { tim, bo, _khach });

        var hang2 = GiaoDien.HangNgang();
        hang2.Controls.Add(GiaoDien.Nhan("Mã SP (F2)"));
        hang2.Controls.Add(_maSp);
        var chon = GiaoDien.NutPhu("Chọn sản phẩm...  (Insert)");
        chon.Click += (_, _) => ChonSanPham();
        var xoa = GiaoDien.NutPhu("Xóa dòng");
        xoa.Click += (_, _) => { if (_bang?.CurrentRow?.DataBoundItem is DongBan d) _dong.Remove(d); HenTinh(); };
        hang2.Controls.AddRange(new Control[] { chon, xoa });

        var hang3 = GiaoDien.HangNgang();
        hang3.Controls.Add(GiaoDien.Nhan("Voucher (F6)"));
        hang3.Controls.Add(_voucher);
        hang3.Controls.Add(GiaoDien.Nhan("Thanh toán (F7)"));
        hang3.Controls.Add(_phuongThuc);
        hang3.Controls.Add(GiaoDien.Nhan("Khách đưa"));
        hang3.Controls.Add(_khachDua);

        Tren.Controls.Add(hang1);
        Tren.Controls.Add(hang2);
        Tren.Controls.Add(hang3);

        _bang = BangDong(_dong, ("MaSp", "Mã SP", 80, false, null), ("TenSp", "Sản phẩm", 0, false, null), ("Ton", "Tồn", 60, false, "N0"),
            ("SoLuong", "Số lượng", 90, true, "N0"), ("DonGiaBan", "Đơn giá (đã KM)", 130, false, "N0"), ("ThanhTien", "Thành tiền", 130, false, "N0"));
        _bang.CellEndEdit += (_, _) => HenTinh();
        _bang.UserDeletedRow += (_, _) => HenTinh();
        var tong = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 6, 0, 0) };
        tong.Controls.Add(_tien);
        tong.Controls.Add(_loi);
        Giua.Controls.Add(_bang);
        Giua.Controls.Add(tong);

        _sdt.KeyDown += (_, e) => { if (e.KeyCode == Keys.Return) { TimKhach(); e.SuppressKeyPress = true; } };
        _maSp.KeyDown += (_, e) => { if (e.KeyCode == Keys.Return) { ThemTheoMa(); e.SuppressKeyPress = true; } };
        _voucher.TextChanged += (_, _) => HenTinh();
        _phuongThuc.SelectedIndexChanged += (_, _) => HenTinh();
        _khachDua.ValueChanged += (_, _) => HienTien();
        _cho.Tick += (_, _) => { _cho.Stop(); TinhTien(); };
        KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.F2: _maSp.Focus(); break;
                case Keys.F4: _sdt.Focus(); break;
                case Keys.F6: _voucher.Focus(); break;
                case Keys.F7: _phuongThuc.Focus(); _phuongThuc.DroppedDown = true; break;
                case Keys.Insert: ChonSanPham(); break;
                default: return;
            }
            e.Handled = true;
        };
        Shown += (_, _) => _maSp.Focus();
        HienKhach();
        TinhTien();
    }

    private void TimKhach()
    {
        var sdt = _sdt.Text.Trim();
        if (sdt.Length == 0) return;
        if (!ThaoTac.Chay("Tìm khách theo SĐT", () => new KhachHangBLL().TimTheoSdt(sdt), out var kh)) return;
        if (kh == null && HopThoai.Hoi($"Chưa có khách với SĐT {sdt}. Thêm khách mới?"))
        {
            using var f = new FormKhachHang(null);
            if (f.ShowDialog(this) == DialogResult.OK) kh = f.KetQua;
        }
        _kh = kh;
        HienKhach();
        HenTinh();
    }

    private void HienKhach()
    {
        _khach.Text = _kh == null
            ? "Khách vãng lai (không tích điểm)"
            : $"{_kh.MaKh} · {_kh.HoTen} · hạng {Nhan.Ten(Nhan.HangThanhVien, _kh.HangThanhVien)} · {_kh.DiemTichLuy:N0} điểm";
        _khach.ForeColor = _kh == null ? GiaoDien.ChuPhu : GiaoDien.XanhLa;
    }

    private void ThemTheoMa()
    {
        var ma = _maSp.Text.Trim().ToUpper();
        if (ma.Length == 0) { ChonSanPham(); return; }
        var sp = _sanPhams.FirstOrDefault(s => s.MaSp == ma);
        if (sp == null)
        {
            // khong phai ma: mo hop chon voi tu khoa
            ChonSanPham();
            return;
        }
        Them(sp);
        _maSp.Clear();
    }

    private void ChonSanPham()
    {
        using var f = new FormChonSanPham(_sanPhams, nhieu: true, chiDangBan: true, "Chọn sản phẩm bán");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        foreach (var s in f.DaChon) Them(s);
    }

    private void Them(SanPhamDTO s)
    {
        var cu = _dong.FirstOrDefault(d => d.MaSp == s.MaSp);
        if (cu != null) cu.SoLuong++;
        else _dong.Add(new DongBan { MaSp = s.MaSp, TenSp = $"{s.TenSp} - {s.MauSac} - {s.KichCo}", Ton = s.SoLuong, DonGiaBan = s.DonGia });
        HenTinh();
    }

    private void HenTinh()
    {
        _cho.Stop();
        _cho.Start();
    }

    private BanTaiQuayDTO YeuCau() => new()
    {
        MaKh = _kh?.MaKh, PhuongThucTt = Muc.Lay(_phuongThuc) ?? "TIEN_MAT", MaVoucher = QuyTac.RongThanhNull(_voucher.Text),
        DongHangs = _dong.Where(d => d.SoLuong > 0).Select(d => new DongHangDTO { MaSp = d.MaSp, SoLuong = d.SoLuong }).ToList()
    };

    // Gia, khuyen mai, voucher deu do BLL tinh (KhuyenMaiBLL.TinhTien), giao dien chi hien
    private void TinhTien()
    {
        _loi.Text = "";
        _bangTien = null;
        var yc = YeuCau();
        if (yc.DongHangs.Count == 0)
        {
            _tien.Text = "Chưa có sản phẩm. Gõ mã SP rồi Enter, hoặc Insert để chọn.";
            NutLuu.Enabled = false;
            return;
        }
        if (ThaoTac.Thu("Tính tiền", () => new HoaDonBLL().TinhTruoc(yc), out var b, out var loi))
        {
            _bangTien = b;
            foreach (var d in _dong)
                if (b.Dongs.FirstOrDefault(x => x.MaSp == d.MaSp) is { } x)
                {
                    d.DonGiaBan = x.DonGiaBan;
                    d.ThanhTien = x.ThanhTien;
                }
            _bang.Invalidate();
        }
        else _loi.Text = loi;
        NutLuu.Enabled = _bangTien != null;
        HienTien();
    }

    private void HienTien()
    {
        if (_bangTien is not { } b)
        {
            if (_dong.Count > 0) _tien.Text = "Chưa tính được tiền, xem lỗi bên dưới";
            return;
        }
        var chu = $"Tạm tính {GiaoDien.Tien(b.TamTinh)}   ·   Giảm {GiaoDien.Tien(b.TienGiam)}"
                  + (b.GhiChuKm != null ? $" ({b.GhiChuKm})" : "") + $"\nTỔNG THANH TOÁN: {GiaoDien.Tien(b.TongTien)}";
        if (Muc.Lay(_phuongThuc) == "TIEN_MAT" && _khachDua.Value > 0)
            chu += _khachDua.Value >= b.TongTien ? $"   ·   Tiền thối: {GiaoDien.Tien(_khachDua.Value - b.TongTien)}" : "   ·   Khách đưa chưa đủ";
        _tien.Text = chu;
    }

    protected override bool Luu()
    {
        _bang.EndEdit();
        _cho.Stop();
        TinhTien();
        if (_bangTien == null) return false;
        if (Muc.Lay(_phuongThuc) == "TIEN_MAT" && _khachDua.Value > 0 && _khachDua.Value < _bangTien.TongTien)
        {
            HopThoai.Canh("Tiền khách đưa chưa đủ");
            return false;
        }
        var yc = YeuCau();
        if (!ThaoTac.Chay("Bán tại quầy", () => new HoaDonBLL().BanTaiQuay(yc, Phien.MaNv), out var hd)) return false;
        KetQua = hd;
        if (HopThoai.Hoi($"Đã lập hóa đơn {hd.MaHoaDon}, tổng {GiaoDien.Tien(hd.TongTien)}. In hóa đơn (PDF)?"))
            ThaoTac.Chay("In hóa đơn", () => XuatFile.MoPdf(new PdfBLL().HoaDon(hd), hd.MaHoaDon));
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _cho.Dispose();
        base.Dispose(disposing);
    }
}

// ===================================================================== DOI TRA TAI QUAY
// Lap phieu doi / tra tu 1 hoa don: DoiTraBLL.GuiYeuCau (kiem tra 30 ngay, so luong con duoc doi tra), co the hoan tat ngay
public sealed class FormLapDoiTra : FormChungTu
{
    private sealed class DongDoiTra
    {
        public string MaSp { get; set; } = "";
        public string TenSp { get; set; } = "";
        public int SoLuong { get; set; }
        public string? MaSpDoi { get; set; }
        public string TenSpDoi { get; set; } = "";
    }

    private readonly TextBox _maHd = GiaoDien.O(140);
    private readonly Label _thongTin = GiaoDien.Nhan("Nhập mã hóa đơn rồi Enter", mau: GiaoDien.ChuPhu);
    private readonly RadioButton _doi = new() { Text = "Đổi size / màu", AutoSize = true, Margin = new Padding(0, 6, 12, 0) };
    private readonly RadioButton _tra = new() { Text = "Trả hàng, hoàn tiền", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 12, 0) };
    private readonly ComboBox _lyDo = GiaoDien.Chon(360, goTuDo: true);
    private readonly Luoi<ChiTietHoaDonDTO> _dongHd = new();
    private readonly NumericUpDown _soLuong = GiaoDien.So(1000, 80);
    private readonly ComboBox _doiSang = GiaoDien.Chon(320);
    private readonly BindingList<DongDoiTra> _chon = new();
    private readonly DataGridView _bangChon;
    private HoaDonDTO? _hd;

    public DoiTraDTO? KetQua { get; private set; }

    public FormLapDoiTra(string? maHoaDon) : base("Lập phiếu đổi / trả tại quầy", "Lập phiếu  (Ctrl+S)", new Size(1000, 680))
    {
        _lyDo.Items.AddRange(new object[] { "Không vừa size", "Không thích màu", "Sản phẩm lỗi", "Giao sai sản phẩm", "Đổi ý" });
        var hang1 = GiaoDien.HangNgang();
        hang1.Controls.Add(GiaoDien.Nhan("Mã hóa đơn *"));
        hang1.Controls.Add(_maHd);
        var tai = GiaoDien.NutPhu("Tải hóa đơn");
        tai.Click += (_, _) => TaiHoaDon();
        hang1.Controls.Add(tai);
        hang1.Controls.Add(_thongTin);
        var hang2 = GiaoDien.HangNgang();
        hang2.Controls.Add(GiaoDien.Nhan("Loại"));
        hang2.Controls.Add(_tra);
        hang2.Controls.Add(_doi);
        hang2.Controls.Add(GiaoDien.Nhan("Lý do"));
        hang2.Controls.Add(_lyDo);
        Tren.Controls.Add(hang1);
        Tren.Controls.Add(hang2);

        _dongHd.Them("Mã SP", c => c.MaSp, 80).Them("Sản phẩm đã mua", c => $"{c.TenSp} - {c.MauSac} - {c.KichCo}", 260, fill: true)
            .So("SL mua", c => c.SoLuong, 70).Tien("Đơn giá", c => c.DonGiaBan, 110);
        _dongHd.ChuKhiTrong = "Chưa tải hóa đơn";
        _dongHd.ChonThayDoi += _ => NapBienThe();

        var them = GiaoDien.NutChinh("Thêm vào phiếu ↓");
        them.Click += (_, _) => ThemDong();
        var hangThem = GiaoDien.HangNgang();
        hangThem.Dock = DockStyle.Bottom;
        hangThem.Controls.Add(GiaoDien.Nhan("Số lượng"));
        hangThem.Controls.Add(_soLuong);
        hangThem.Controls.Add(GiaoDien.Nhan("Đổi sang"));
        hangThem.Controls.Add(_doiSang);
        hangThem.Controls.Add(them);
        _soLuong.Minimum = 1;

        _bangChon = BangDong(_chon, ("MaSp", "Mã SP", 80, false, null), ("TenSp", "Sản phẩm", 0, false, null),
            ("SoLuong", "Số lượng", 80, false, "N0"), ("TenSpDoi", "Đổi sang", 260, false, null));

        var chia = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 8 };
        var tren = BoCuc.KhungBang(_dongHd.Grid, "1. Chọn dòng trong hóa đơn", hangThem);
        chia.Panel1.Controls.Add(tren);
        chia.Panel2.Controls.Add(BoCuc.KhungBang(_bangChon, "2. Các dòng của phiếu (Delete để bỏ)"));
        Load += (_, _) => chia.SplitterDistance = chia.Height / 2 + 20;
        Giua.Controls.Add(chia);

        _doi.CheckedChanged += (_, _) => { _doiSang.Enabled = _doi.Checked; _chon.Clear(); NapBienThe(); };
        _doiSang.Enabled = false;
        _maHd.KeyDown += (_, e) => { if (e.KeyCode == Keys.Return) { TaiHoaDon(); e.SuppressKeyPress = true; } };
        if (maHoaDon != null)
        {
            _maHd.Text = maHoaDon;
            Shown += (_, _) => TaiHoaDon();
        }
    }

    private void TaiHoaDon()
    {
        var ma = _maHd.Text.Trim().ToUpper();
        if (ma.Length == 0) return;
        if (!ThaoTac.Chay("Tải hóa đơn", () => new HoaDonBLL().LayTheoMa(ma), out var hd)) return;
        _hd = hd;
        _chon.Clear();
        var soNgay = (DateTime.Today - hd.NgayLap.Date).Days;
        _thongTin.Text = $"{hd.MaHoaDon} · {hd.TenKhachHang ?? "Khách vãng lai"} · {hd.NgayLap:dd/MM/yyyy} ({soNgay} ngày trước) · "
                         + $"{Nhan.Ten(Nhan.TrangThaiHoaDon, hd.TrangThai)} · {GiaoDien.Tien(hd.TongTien)}";
        _thongTin.ForeColor = hd.TrangThai == "DA_THANH_TOAN" && soNgay <= QuyTac.SoNgayDoiTra ? GiaoDien.ChuChinh : GiaoDien.Do;
        _dongHd.Nap(hd.ChiTiets);
        _dongHd.Grid.Focus();
    }

    private void NapBienThe()
    {
        _doiSang.Items.Clear();
        if (!_doi.Checked || _dongHd.Chon is not { } c) return;
        if (!ThaoTac.Chay("Lấy biến thể cùng mẫu", () => new SanPhamBLL().LayCacBienThe(c.MaSp), out var ds)) return;
        Muc.Nap(_doiSang, ds.Where(s => s.MaSp != c.MaSp && s.TrangThai == "DANG_BAN")
            .Select(s => new Muc(s.MaSp, $"{s.MaSp} · {s.MauSac} · {s.KichCo} · tồn {s.SoLuong}")));
    }

    private void ThemDong()
    {
        if (_dongHd.Chon is not { } c) return;
        var doiSang = _doi.Checked ? _doiSang.SelectedItem as Muc : null;
        if (_doi.Checked && doiSang == null)
        {
            HopThoai.Canh("Chọn biến thể muốn đổi sang (cùng mẫu, khác size / màu)");
            return;
        }
        _chon.Remove(_chon.FirstOrDefault(d => d.MaSp == c.MaSp)!);
        _chon.Add(new DongDoiTra
        {
            MaSp = c.MaSp, TenSp = $"{c.TenSp} - {c.MauSac} - {c.KichCo}", SoLuong = (int)_soLuong.Value,
            MaSpDoi = doiSang?.Ma, TenSpDoi = doiSang?.Ten ?? ""
        });
    }

    protected override bool Luu()
    {
        if (_hd == null)
        {
            HopThoai.Canh("Tải hóa đơn trước");
            return false;
        }
        var loai = _doi.Checked ? "DOI" : "TRA";
        var dongs = _chon.Select(d => new ChiTietDoiTraDTO { MaSp = d.MaSp, SoLuong = d.SoLuong, MaSpDoi = d.MaSpDoi }).ToList();
        var maHd = _hd.MaHoaDon;
        if (!ThaoTac.Chay("Lập phiếu đổi trả", () => new DoiTraBLL().GuiYeuCau(maHd, loai, _lyDo.Text, dongs, null), out var dt)) return false;
        KetQua = dt;
        if (HopThoai.Hoi($"Đã lập phiếu {dt.MaDoiTra} (chờ duyệt).\nKhách đang ở quầy và đã giao hàng: hoàn tất ngay (nhận hàng về kho"
                         + (loai == "DOI" ? ", xuất hàng đổi" : ", hoàn tiền") + ")?"))
        {
            if (ThaoTac.Chay("Hoàn tất đổi trả", () => new DoiTraBLL().HoanTat(dt.MaDoiTra, Phien.MaNv), out var xong))
            {
                KetQua = xong;
                if (xong.TienHoan > 0) HopThoai.ThongBao($"Hoàn cho khách {GiaoDien.Tien(xong.TienHoan)} (theo giá thực trả, đã trừ giảm giá hóa đơn).");
            }
        }
        if (HopThoai.Hoi("In phiếu đổi trả (PDF)?"))
        {
            var ma = dt.MaDoiTra;
            ThaoTac.Chay("In phiếu đổi trả", () => XuatFile.MoPdf(new PdfBLL().DoiTra(new DoiTraBLL().LayTheoMa(ma)), ma));
        }
        return true;
    }
}
