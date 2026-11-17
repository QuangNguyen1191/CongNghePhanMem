namespace AppAdmin.CacHopThoai;

// Them / sua 1 SKU (1 size - 1 mau). Ton kho khong sua o day: chi tang qua phieu nhap, giam qua ban hang.
public sealed class FormSanPham : FormNhap
{
    private readonly SanPhamDTO? _cu;
    private readonly TextBox _ten = GiaoDien.O(380);
    private readonly ComboBox _loai = GiaoDien.Chon(240);
    private readonly TextBox _chatLieu = GiaoDien.O(240);
    private readonly TextBox _moTa = new() { Width = 380, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly NumericUpDown _gia = GiaoDien.So();
    private readonly TextBox _co = GiaoDien.O(100);
    private readonly TextBox _mau = GiaoDien.O(180);
    private readonly TextBox _anh = GiaoDien.O(380);
    private readonly ComboBox _trangThai = GiaoDien.Chon(160);

    public SanPhamDTO? KetQua { get; private set; }

    public FormSanPham(SanPhamDTO? sp, List<LoaiSpDTO> loais)
        : base(sp == null ? "Thêm sản phẩm (1 biến thể)" : $"Sửa sản phẩm {sp.MaSp}")
    {
        _cu = sp;
        Muc.Nap(_loai, loais.Select(l => new Muc(l.MaLoaiSp, $"{l.MaLoaiSp} - {l.TenLoaiSp}")), sp?.MaLoaiSp);
        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiSanPham, null), sp?.TrangThai ?? "DANG_BAN");
        ThemO("Tên sản phẩm", _ten, true, "Các biến thể cùng mẫu phải trùng tên (đổi trả chỉ cho đổi sang biến thể cùng tên)");
        ThemO("Loại", _loai, true);
        ThemO("Chất liệu", _chatLieu);
        ThemO("Mô tả", _moTa);
        ThemO("Đơn giá bán (đ)", _gia, true);
        ThemO("Kích cỡ", _co, ghiChu: "vd S, M, L, XL. Bỏ trống nếu không có size");
        ThemO("Màu sắc", _mau);
        ThemO("Hình ảnh", _anh, ghiChu: "Đường dẫn ảnh web bán hàng dùng, vd /images/uniqlo/E487962-000/00-1.jpg");
        ThemO("Trạng thái", _trangThai);
        if (sp != null)
        {
            _ten.Text = sp.TenSp;
            _chatLieu.Text = sp.ChatLieu;
            _moTa.Text = sp.MoTa;
            _gia.Value = Math.Min(_gia.Maximum, sp.DonGia);
            _co.Text = sp.KichCo;
            _mau.Text = sp.MauSac;
            _anh.Text = sp.HinhAnh;
            ThemO("Tồn kho hiện tại", GiaoDien.Nhan($"{sp.SoLuong:N0} cái (thay đổi qua phiếu nhập / bán hàng)", mau: GiaoDien.ChuPhu));
        }
        else _trangThai.Enabled = false;
    }

    protected override bool Luu()
    {
        var s = new SanPhamDTO
        {
            MaSp = _cu?.MaSp ?? "", TenSp = _ten.Text, MaLoaiSp = Muc.Lay(_loai) ?? "", ChatLieu = QuyTac.RongThanhNull(_chatLieu.Text),
            MoTa = QuyTac.RongThanhNull(_moTa.Text), DonGia = _gia.Value, KichCo = _co.Text, MauSac = _mau.Text,
            HinhAnh = QuyTac.RongThanhNull(_anh.Text), TrangThai = Muc.Lay(_trangThai) ?? "DANG_BAN", SoLuong = _cu?.SoLuong ?? 0
        };
        if (_cu == null)
        {
            if (!ThaoTac.Chay("Thêm sản phẩm", () => new SanPhamBLL().Them(s), out var moi)) return false;
            KetQua = moi;
        }
        else
        {
            if (!ThaoTac.Chay("Sửa sản phẩm", () => new SanPhamBLL().Sua(s))) return false;
            KetQua = s;
        }
        return true;
    }
}

// Tao nhanh 1 mau san pham nhieu size x nhieu mau: moi to hop 1 ma SP (SanPhamBLL.ThemNhieu, 1 giao dich)
public sealed class FormThemMauSanPham : FormNhap
{
    private static readonly string[] CacCo = { "XS", "S", "M", "L", "XL", "XXL", "3XL" };
    private readonly TextBox _ten = GiaoDien.O(380);
    private readonly ComboBox _loai = GiaoDien.Chon(240);
    private readonly TextBox _chatLieu = GiaoDien.O(240);
    private readonly TextBox _moTa = new() { Width = 380, Height = 50, Multiline = true };
    private readonly NumericUpDown _gia = GiaoDien.So();
    private readonly CheckedListBox _co = new() { Width = 380, Height = 30, MultiColumn = true, ColumnWidth = 52, CheckOnClick = true, BorderStyle = BorderStyle.None };
    private readonly TextBox _coKhac = GiaoDien.O(240);
    private readonly TextBox _mau = GiaoDien.O(380);
    private readonly TextBox _anh = GiaoDien.O(380);
    private readonly Label _xemTruoc = GiaoDien.Nhan("", GiaoDien.ChuDam, GiaoDien.XanhDuong);

    public List<SanPhamDTO> KetQua { get; private set; } = new();

    public FormThemMauSanPham(List<LoaiSpDTO> loais) : base("Thêm mẫu sản phẩm nhiều biến thể", 620)
    {
        Muc.Nap(_loai, loais.Select(l => new Muc(l.MaLoaiSp, $"{l.MaLoaiSp} - {l.TenLoaiSp}")));
        _co.Items.AddRange(CacCo.Cast<object>().ToArray());
        foreach (var i in new[] { 1, 2, 3 }) _co.SetItemChecked(i, true);
        ThemO("Tên sản phẩm", _ten, true);
        ThemO("Loại", _loai, true);
        ThemO("Chất liệu", _chatLieu);
        ThemO("Mô tả", _moTa);
        ThemO("Đơn giá bán (đ)", _gia, true, "Áp dụng cho mọi biến thể, sửa riêng từng SKU sau nếu cần");
        ThemO("Kích cỡ", _co);
        ThemO("Size khác", _coKhac, ghiChu: "Cách nhau bằng dấu phẩy, vd 28, 29, 30 (quần jean). Không chọn size nào: tạo SKU không size");
        ThemO("Màu sắc", _mau, true, "Cách nhau bằng dấu phẩy, vd Trắng, Đen, Be");
        ThemO("Hình ảnh", _anh);
        ThemRong(_xemTruoc);
        _co.ItemCheck += (_, _) => BeginInvoke(new Action(XemTruoc));
        _coKhac.TextChanged += (_, _) => XemTruoc();
        _mau.TextChanged += (_, _) => XemTruoc();
        XemTruoc();
    }

    private List<string?> LayCo()
    {
        var ds = _co.CheckedItems.Cast<string>().Select(s => (string?)s)
            .Concat(Tach(_coKhac.Text).Select(s => (string?)s.ToUpper())).Distinct().ToList();
        return ds.Count == 0 ? new List<string?> { null } : ds;
    }

    private static List<string> Tach(string s) =>
        s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

    private void XemTruoc()
    {
        var co = LayCo();
        var mau = Tach(_mau.Text);
        _xemTruoc.Text = mau.Count == 0
            ? "Nhập ít nhất 1 màu"
            : $"Sẽ tạo {mau.Count} màu × {co.Count} size = {mau.Count * co.Count} SKU (tồn kho = 0, nhập hàng sau)";
    }

    protected override bool Luu()
    {
        var mau = Tach(_mau.Text);
        if (mau.Count == 0)
        {
            HopThoai.Canh("Nhập ít nhất 1 màu sắc");
            return false;
        }
        var ds = (from m in mau
                  from c in LayCo()
                  select new SanPhamDTO
                  {
                      TenSp = _ten.Text, MaLoaiSp = Muc.Lay(_loai) ?? "", ChatLieu = QuyTac.RongThanhNull(_chatLieu.Text),
                      MoTa = QuyTac.RongThanhNull(_moTa.Text), DonGia = _gia.Value, KichCo = c, MauSac = m,
                      HinhAnh = QuyTac.RongThanhNull(_anh.Text), TrangThai = "DANG_BAN"
                  }).ToList();
        if (ds.Count > 60 && !HopThoai.Hoi($"Tạo {ds.Count} SKU cùng lúc?")) return false;
        if (!ThaoTac.Chay("Thêm mẫu sản phẩm", () => new SanPhamBLL().ThemNhieu(ds), out var kq)) return false;
        KetQua = kq;
        HopThoai.ThongBao($"Đã tạo {kq.Count} SKU: {kq.First().MaSp} … {kq.Last().MaSp}");
        return true;
    }
}

public sealed class FormNhaCungCap : FormNhap
{
    private readonly NhaCungCapDTO? _cu;
    private readonly TextBox _ten = GiaoDien.O(360);
    private readonly TextBox _diaChi = GiaoDien.O(360);
    private readonly TextBox _sdt = GiaoDien.O(180);
    private readonly TextBox _email = GiaoDien.O(260);
    private readonly TextBox _mst = GiaoDien.O(180);
    private readonly ComboBox _trangThai = GiaoDien.Chon(180);

    public FormNhaCungCap(NhaCungCapDTO? n) : base(n == null ? "Thêm nhà cung cấp" : $"Sửa nhà cung cấp {n.MaNcc}")
    {
        _cu = n;
        Muc.Nap(_trangThai, Nhan.CacMuc(Nhan.TrangThaiNcc, null), n?.TrangThai ?? "HOP_TAC");
        ThemO("Tên nhà cung cấp", _ten, true);
        ThemO("Địa chỉ", _diaChi);
        ThemO("Số điện thoại", _sdt);
        ThemO("Email", _email);
        ThemO("Mã số thuế", _mst);
        ThemO("Trạng thái", _trangThai, ghiChu: "Ngừng hợp tác: không lập phiếu nhập mới được, phiếu cũ vẫn giữ");
        if (n == null) return;
        _ten.Text = n.TenNcc;
        _diaChi.Text = n.DiaChi;
        _sdt.Text = n.Sdt;
        _email.Text = n.Email;
        _mst.Text = n.MaSoThue;
    }

    protected override bool Luu()
    {
        var n = new NhaCungCapDTO
        {
            MaNcc = _cu?.MaNcc ?? "", TenNcc = _ten.Text, DiaChi = QuyTac.RongThanhNull(_diaChi.Text), Sdt = QuyTac.RongThanhNull(_sdt.Text),
            Email = _email.Text, MaSoThue = _mst.Text, TrangThai = Muc.Lay(_trangThai) ?? "HOP_TAC"
        };
        return _cu == null
            ? ThaoTac.Chay("Thêm nhà cung cấp", () => new NhaCungCapBLL().Them(n), out _)
            : ThaoTac.Chay("Sửa nhà cung cấp", () => new NhaCungCapBLL().Sua(n));
    }
}

// Chon 1 hoac nhieu SKU tu danh sach da tai (loc trong bo nho, khong chay SQL)
public sealed class FormChonSanPham : Form
{
    private readonly List<SanPhamDTO> _tatCa;
    private readonly TextBox _o = new() { Dock = DockStyle.Top, Font = new Font(GiaoDien.TenFont, 11f) };
    private readonly Luoi<SanPhamDTO> _luoi;

    public List<SanPhamDTO> DaChon { get; private set; } = new();

    public FormChonSanPham(List<SanPhamDTO> ds, bool nhieu, bool chiDangBan, string tieuDe = "Chọn sản phẩm")
    {
        _tatCa = chiDangBan ? ds.Where(s => s.TrangThai == "DANG_BAN").ToList() : ds;
        Text = tieuDe + (nhieu ? "  ·  Ctrl / Shift + click để chọn nhiều, Enter để chọn" : "  ·  Enter để chọn");
        Font = GiaoDien.Chu;
        ClientSize = new Size(860, 520);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        Padding = new Padding(10);
        KeyPreview = true;
        _luoi = new Luoi<SanPhamDTO>(nhieu)
            .Them("Mã SP", s => s.MaSp, 80)
            .Them("Tên sản phẩm", s => s.TenSp, 260, fill: true)
            .Them("Màu", s => s.MauSac, 110)
            .Them("Size", s => s.KichCo, 60)
            .Tien("Đơn giá", s => s.DonGia, 100)
            .So("Tồn", s => s.SoLuong, 60)
            .TrangThai("Trạng thái", s => s.TrangThai, Nhan.TrangThaiSanPham, 100);
        _luoi.MoDong += _ => XacNhan();
        var ok = GiaoDien.NutChinh("Chọn  (Enter)");
        ok.Click += (_, _) => XacNhan();
        var huy = GiaoDien.NutPhu("Hủy  (Esc)");
        huy.DialogResult = DialogResult.Cancel;
        CancelButton = huy;
        var duoi = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        duoi.Controls.Add(ok);
        duoi.Controls.Add(huy);
        Controls.Add(_luoi.Grid);
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        _o.PlaceholderText = "Gõ mã, tên, màu hoặc size để lọc. ↓ để xuống bảng";
        Controls.Add(_o);
        Controls.Add(duoi);
        _o.TextChanged += (_, _) => Loc();
        _o.KeyDown += (_, e) =>
        {
            if (e.KeyCode is Keys.Down or Keys.Return)
            {
                _luoi.Grid.Focus();
                e.SuppressKeyPress = true;
            }
        };
        _luoi.Grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Return) return;
            XacNhan();
            e.SuppressKeyPress = true;
        };
        Loc();
    }

    private void Loc()
    {
        var tk = _o.Text.Trim();
        _luoi.Nap(tk.Length == 0
            ? _tatCa
            : _tatCa.Where(s => $"{s.MaSp} {s.TenSp} {s.MauSac} {s.KichCo}".Contains(tk, StringComparison.CurrentCultureIgnoreCase)));
    }

    private void XacNhan()
    {
        DaChon = _luoi.CacDongChon;
        if (DaChon.Count == 0 && _luoi.Chon is { } c) DaChon = new List<SanPhamDTO> { c };
        if (DaChon.Count == 0) return;
        DialogResult = DialogResult.OK;
    }
}

// Yeu cau 7: xem truoc ket qua doc file Excel (dong hop le + loi tung dong) truoc khi ghi vao CSDL
public sealed class FormXemTruocExcel<T> : Form where T : class
{
    public FormXemTruocExcel(string tieuDe, KetQuaDocExcelDTO<T> kq, Action<Luoi<T>> khaiBaoCot, string chuNutGhi)
    {
        Text = tieuDe;
        Font = GiaoDien.Chu;
        ClientSize = new Size(940, 600);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        Padding = new Padding(12);

        var tom = new Label
        {
            Dock = DockStyle.Top, Height = 30, Font = GiaoDien.ChuDam,
            Text = $"File có {kq.TongSoDong} dòng dữ liệu: {kq.HopLe.Count} dòng hợp lệ, {kq.Lois.Count} dòng lỗi"
                   + (kq.Lois.Count > 0 ? ". Dòng lỗi sẽ bị bỏ qua, sửa file rồi đọc lại nếu cần." : ""),
            ForeColor = kq.Lois.Count > 0 ? GiaoDien.Vang : GiaoDien.XanhLa
        };
        var luoiHopLe = new Luoi<T> { ChuKhiTrong = "Không có dòng hợp lệ" };
        khaiBaoCot(luoiHopLe);
        luoiHopLe.Nap(kq.HopLe);
        var luoiLoi = new Luoi<LoiDongExcelDTO> { ChuKhiTrong = "Không có lỗi" }
            .So("Dòng trong file", l => l.Dong, 110)
            .Them("Lỗi", l => l.ThongBao, 500, fill: true, mau: _ => GiaoDien.Do);
        luoiLoi.Nap(kq.Lois);

        var chia = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 8 };
        chia.Panel1.Controls.Add(BoCuc.KhungBang(luoiHopLe.Grid, $"Dòng hợp lệ ({kq.HopLe.Count})"));
        chia.Panel2.Controls.Add(BoCuc.KhungBang(luoiLoi.Grid, $"Dòng lỗi ({kq.Lois.Count})"));
        Load += (_, _) => chia.SplitterDistance = (int)(chia.Height * (kq.Lois.Count > 0 ? 0.6 : 0.82));

        var ghi = GiaoDien.NutChinh($"{chuNutGhi} {kq.HopLe.Count} dòng hợp lệ  (Ctrl+S)");
        ghi.Enabled = kq.HopLe.Count > 0;
        ghi.Click += (_, _) => DialogResult = DialogResult.OK;
        var huy = GiaoDien.NutPhu("Hủy  (Esc)");
        huy.DialogResult = DialogResult.Cancel;
        CancelButton = huy;
        var duoi = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        duoi.Controls.Add(ghi);
        duoi.Controls.Add(huy);
        Controls.Add(chia);
        Controls.Add(tom);
        Controls.Add(duoi);
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S && ghi.Enabled) DialogResult = DialogResult.OK;
        };
    }
}
