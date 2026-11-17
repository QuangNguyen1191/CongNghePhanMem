using System.ComponentModel;

namespace AppAdmin.CacHopThoai;

// Them / sua chuong trinh khuyen mai 4 hinh thuc. Phan chi tiet doi theo hinh thuc:
//   SAN_PHAM: danh sach SKU + % giam    HOA_DON: cac muc tong tien + % giam
//   VOUCHER : cac ma + % giam           THANH_VIEN_MOI: N don dau, X%, toi da Y dong
public sealed class FormKhuyenMai : FormNhap
{
    private readonly KhuyenMaiDTO? _cu;
    private readonly List<SanPhamDTO> _sanPhams;
    private readonly Dictionary<string, SanPhamDTO> _spTheoMa;

    private readonly TextBox _ten = GiaoDien.O(380);
    private readonly ComboBox _hinhThuc = GiaoDien.Chon(260);
    private readonly DateTimePicker _bd = GiaoDien.Ngay();
    private readonly DateTimePicker _kt = GiaoDien.Ngay();
    private readonly CheckBox _coPhanTram = new() { Text = "Có", AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
    private readonly NumericUpDown _phanTram = GiaoDien.So(100, 90, 2);
    private readonly Panel _chiTiet = new() { Width = 600, Height = 250 };

    private readonly BindingList<CtKmSanPhamDTO> _dsSp = new();
    private readonly BindingList<CtKmHoaDonDTO> _dsMuc = new() { AllowNew = true };
    private readonly BindingList<CtKmVoucherDTO> _dsVoucher = new() { AllowNew = true };
    private readonly NumericUpDown _soDonDau = GiaoDien.So(10, 90);
    private readonly NumericUpDown _tvPhanTram = GiaoDien.So(100, 90, 2);
    private readonly CheckBox _coToiDa = new() { Text = "Giới hạn", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 6, 0) };
    private readonly NumericUpDown _toiDa = GiaoDien.So(100_000_000);

    public FormKhuyenMai(KhuyenMaiDTO? km, List<SanPhamDTO> sanPhams)
        : base(km == null ? "Thêm chương trình khuyến mãi" : $"Sửa khuyến mãi {km.MaKm}", 660)
    {
        _cu = km;
        _sanPhams = sanPhams;
        _spTheoMa = sanPhams.ToDictionary(s => s.MaSp);
        Muc.Nap(_hinhThuc, Nhan.CacMuc(Nhan.HinhThucKm, null), km?.HinhThuc ?? "VOUCHER");
        var phanTramHang = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0) };
        phanTramHang.Controls.Add(_coPhanTram);
        phanTramHang.Controls.Add(_phanTram);
        _coPhanTram.CheckedChanged += (_, _) => _phanTram.Enabled = _coPhanTram.Checked;

        ThemO("Tên chương trình", _ten, true);
        ThemO("Hình thức", _hinhThuc, true, "Không cộng dồn: hệ thống tự chọn mức giảm có lợi nhất cho khách");
        ThemO("Ngày bắt đầu", _bd, true);
        ThemO("Ngày kết thúc", _kt, true);
        ThemO("% giảm hiển thị", phanTramHang, ghiChu: "Tùy chọn, để hiện trên banner. Mức giảm thật lấy ở bảng chi tiết bên dưới");
        ThemRong(GiaoDien.TieuDeMuc("Chi tiết áp dụng"));
        ThemRong(_chiTiet);

        _soDonDau.Minimum = 1;
        _soDonDau.Value = 3;
        _tvPhanTram.Value = 10;
        _toiDa.Value = 100_000;
        _coToiDa.CheckedChanged += (_, _) => _toiDa.Enabled = _coToiDa.Checked;

        if (km != null)
        {
            _ten.Text = km.TenKm;
            _bd.Value = km.NgayBd;
            _kt.Value = km.NgayKt;
            _coPhanTram.Checked = km.PhanTramGiamGia != null;
            _phanTram.Value = km.PhanTramGiamGia ?? 0;
            foreach (var c in km.SanPhams) _dsSp.Add(c);
            foreach (var c in km.MucHoaDons) _dsMuc.Add(c);
            foreach (var c in km.Vouchers) _dsVoucher.Add(c);
            // Doi hinh thuc khac thi voucher cu van con trong CT_KM_VOUCHER va van ap dung duoc -> khong cho doi
            if (km.Vouchers.Count > 0) _hinhThuc.Enabled = false;
            if (km.ThanhVienMoi is { } tv)
            {
                _soDonDau.Value = Math.Clamp(tv.SoDonDau, 1, 10);
                _tvPhanTram.Value = tv.PhanTramGiamGia;
                _coToiDa.Checked = tv.GiamToiDa != null;
                _toiDa.Value = tv.GiamToiDa ?? 100_000;
            }
        }
        else
        {
            _bd.Value = DateTime.Today;
            _kt.Value = DateTime.Today.AddDays(30);
        }
        _phanTram.Enabled = _coPhanTram.Checked;
        _hinhThuc.SelectedIndexChanged += (_, _) => VeChiTiet();
        VeChiTiet();
    }

    private void VeChiTiet()
    {
        _chiTiet.Controls.Clear();
        switch (Muc.Lay(_hinhThuc))
        {
            case "SAN_PHAM":
                var g = BangSua(_dsSp, ("MaSp", "Mã SP", 80, true, null), ("PhanTramGiamGia", "% giảm", 90, false, "0.##"));
                var ten = new DataGridViewTextBoxColumn { HeaderText = "Sản phẩm", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
                g.Columns.Insert(1, ten);
                g.CellFormatting += (_, e) =>
                {
                    if (e.ColumnIndex != 1 || e.RowIndex < 0 || e.RowIndex >= _dsSp.Count) return;
                    e.Value = _spTheoMa.TryGetValue(_dsSp[e.RowIndex].MaSp, out var s) ? $"{s.TenSp} - {s.MauSac} - {s.KichCo}" : "?";
                };
                var them = GiaoDien.NutPhu("Thêm sản phẩm...  (Insert)");
                them.Click += (_, _) => ThemSanPham();
                g.KeyDown += (_, e) => { if (e.KeyCode == Keys.Insert) ThemSanPham(); };
                DatBang(g, them, "Thêm SP dùng % ở ô \"% giảm hiển thị\" (mặc định 10%), sửa từng dòng trong bảng. Delete để xóa dòng.");
                break;
            case "HOA_DON":
                DatBang(BangSua(_dsMuc, ("MucApDungHoaDon", "Hóa đơn từ (đ)", 200, false, "N0"), ("PhanTramGiamGia", "% giảm", 100, false, "0.##")),
                    null, "Gõ vào dòng trống cuối bảng để thêm mức. Hóa đơn đạt nhiều mức thì lấy mức giảm cao nhất.");
                break;
            case "VOUCHER":
                var gv = BangSua(_dsVoucher, ("MaVoucher", "Mã voucher", 200, false, null), ("PhanTramGiamGia", "% giảm", 100, false, "0.##"));
                // KhuyenMaiDAL.Sua khong xoa voucher cu (co the da dung trong don): khoa ma cu, chi sua % / them ma moi
                var maCu = _cu?.Vouchers.Select(v => v.MaVoucher).ToHashSet() ?? new HashSet<string>();
                bool LaMaCu(int dong) => dong >= 0 && dong < _dsVoucher.Count && maCu.Contains(_dsVoucher[dong].MaVoucher);
                gv.CellBeginEdit += (_, e) => { if (e.ColumnIndex == 0 && LaMaCu(e.RowIndex)) e.Cancel = true; };
                gv.UserDeletingRow += (_, e) =>
                {
                    if (!LaMaCu(e.Row?.Index ?? -1)) return;
                    e.Cancel = true;
                    HopThoai.Canh("Mã voucher đã lưu không xóa được (có thể đã dùng trong đơn hàng). Muốn ngừng mã: kết thúc chương trình.");
                };
                DatBang(gv, null, maCu.Count > 0
                    ? "Mã đã lưu chỉ sửa được % giảm. Gõ vào dòng trống cuối bảng để thêm mã mới."
                    : "Gõ vào dòng trống cuối bảng để thêm mã. Mã tự viết hoa, không được trùng với KM khác.");
                break;
            default:
                var bang = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 8, 0, 0) };
                bang.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
                void Dong(string nhan, Control c)
                {
                    bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    bang.Controls.Add(GiaoDien.Nhan(nhan));
                    bang.Controls.Add(c);
                }
                var toiDaHang = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0) };
                toiDaHang.Controls.Add(_coToiDa);
                toiDaHang.Controls.Add(_toiDa);
                Dong("Số đơn đầu được giảm (1-10)", _soDonDau);
                Dong("% giảm mỗi đơn", _tvPhanTram);
                Dong("Giảm tối đa mỗi đơn (đ)", toiDaHang);
                bang.Controls.Add(new Label
                {
                    Text = "Chỉ tính đơn đặt bằng tài khoản; đơn bị hủy / từ chối được trả lại lượt. "
                           + "Nên chỉ để 1 chương trình loại này chạy cùng lúc.",
                    AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = GiaoDien.ChuPhu, Margin = new Padding(0, 10, 0, 0)
                });
                bang.SetColumnSpan(bang.Controls[^1], 2);
                _chiTiet.Controls.Add(bang);
                break;
        }
    }

    private void DatBang(DataGridView g, Control? nut, string ghiChu)
    {
        var duoi = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(0, 4, 0, 0) };
        if (nut != null) duoi.Controls.Add(nut);
        duoi.Controls.Add(new Label { Text = ghiChu, AutoSize = true, MaximumSize = new Size(nut == null ? 580 : 380, 0), ForeColor = GiaoDien.ChuPhu, Margin = new Padding(0, 4, 0, 0) });
        _chiTiet.Controls.Add(g);
        _chiTiet.Controls.Add(duoi);
    }

    // Bang sua truc tiep, gan voi BindingList<T>
    private static DataGridView BangSua<T>(BindingList<T> ds, params (string ThuocTinh, string TieuDe, int Rong, bool ChiDoc, string? DinhDang)[] cots)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill, AutoGenerateColumns = false, RowHeadersWidth = 24, AllowUserToAddRows = ds.AllowNew,
            AllowUserToDeleteRows = true, SelectionMode = DataGridViewSelectionMode.CellSelect, BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle, EnableHeadersVisualStyles = false, Font = GiaoDien.Chu
        };
        g.ColumnHeadersDefaultCellStyle.Font = GiaoDien.ChuDam;
        foreach (var c in cots)
        {
            var cot = new DataGridViewTextBoxColumn { DataPropertyName = c.ThuocTinh, HeaderText = c.TieuDe, Width = c.Rong, ReadOnly = c.ChiDoc };
            cot.DefaultCellStyle.FormatProvider = GiaoDien.Vn;
            if (c.DinhDang != null)
            {
                cot.DefaultCellStyle.Format = c.DinhDang;
                cot.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
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

    private void ThemSanPham()
    {
        using var f = new FormChonSanPham(_sanPhams, nhieu: true, chiDangBan: false, "Chọn sản phẩm được giảm giá");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        var phanTram = _coPhanTram.Checked && _phanTram.Value > 0 ? _phanTram.Value : 10;
        foreach (var s in f.DaChon.Where(s => _dsSp.All(c => c.MaSp != s.MaSp)))
            _dsSp.Add(new CtKmSanPhamDTO { MaKm = _cu?.MaKm ?? "", MaSp = s.MaSp, PhanTramGiamGia = phanTram });
    }

    protected override bool Luu()
    {
        foreach (var g in _chiTiet.Controls.OfType<DataGridView>()) g.EndEdit();
        var hinhThuc = Muc.Lay(_hinhThuc) ?? "";
        var k = new KhuyenMaiDTO
        {
            MaKm = _cu?.MaKm ?? "", TenKm = _ten.Text, HinhThuc = hinhThuc, NgayBd = _bd.Value.Date, NgayKt = _kt.Value.Date,
            PhanTramGiamGia = _coPhanTram.Checked ? _phanTram.Value : null,
            SanPhams = hinhThuc == "SAN_PHAM" ? _dsSp.ToList() : new(),
            MucHoaDons = hinhThuc == "HOA_DON" ? _dsMuc.Where(m => m.PhanTramGiamGia > 0 || m.MucApDungHoaDon > 0).ToList() : new(),
            Vouchers = hinhThuc == "VOUCHER" ? _dsVoucher.Where(v => !string.IsNullOrWhiteSpace(v.MaVoucher)).ToList() : new(),
            ThanhVienMoi = hinhThuc == "THANH_VIEN_MOI"
                ? new CtKmThanhVienMoiDTO
                {
                    MaKm = _cu?.MaKm ?? "", SoDonDau = (int)_soDonDau.Value, PhanTramGiamGia = _tvPhanTram.Value,
                    GiamToiDa = _coToiDa.Checked ? _toiDa.Value : null
                }
                : null
        };
        return _cu == null
            ? ThaoTac.Chay("Thêm khuyến mãi", () => new KhuyenMaiBLL().Them(k), out _)
            : ThaoTac.Chay("Sửa khuyến mãi", () => new KhuyenMaiBLL().Sua(k));
    }
}
