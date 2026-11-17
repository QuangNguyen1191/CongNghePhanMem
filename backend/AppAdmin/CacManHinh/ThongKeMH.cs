namespace AppAdmin.CacManHinh;

// Yeu cau 3 + 5a-5e: moi bao cao chay duoc theo 2 cach cua ThongKeBLL
//   TAI_SU_DUNG : dung lai danh sach hoa don / chi tiet / khoan chi da tai (gom nhom bang LINQ), lan sau 0 cau SQL
//   TRUY_VAN_MOI: gui 1 cau GROUP BY moi xuong SQL Server
// Nut "So sanh 2 cach" chay ca hai, doi chieu ket qua va hien thoi gian + so cau SQL.
public sealed class ThongKeMH : ManHinh
{
    private sealed record LoaiBaoCao(string Ma, string Ten, string MoTa);

    private static readonly LoaiBaoCao[] CacBaoCao =
    {
        new("5a", "5a. Tổng tiền từ ngày A đến ngày B", "Số hóa đơn đã thanh toán và tổng tiền trong khoảng ngày"),
        new("5b", "5b. Thu - chi - lợi nhuận", "Thu = hóa đơn đã thanh toán; chi = phiếu nhập + tiền hoàn trả hàng"),
        new("5c", "5c. Theo nhân viên × năm", "Doanh thu từng nhân viên lập hóa đơn, mỗi năm 1 cột"),
        new("5d", "5d. Theo khách hàng × quý / năm", "Chọn 1 năm: cột là 4 quý. Tất cả năm: cột là các năm"),
        new("5e", "5e. Theo sản phẩm × quý", "Doanh thu từng SKU theo 4 quý của 1 năm"),
    };

    private readonly ListBox _ds = new() { Dock = DockStyle.Top, Height = 132, BorderStyle = BorderStyle.None, Font = GiaoDien.Chu, IntegralHeight = false };
    private readonly Label _moTaBc = new() { Dock = DockStyle.Top, Height = 40, ForeColor = GiaoDien.ChuPhu, Font = GiaoDien.ChuNho };
    private readonly DateTimePicker _tu = GiaoDien.Ngay();
    private readonly DateTimePicker _den = GiaoDien.Ngay();
    private readonly ComboBox _kieu = GiaoDien.Chon(150);
    private readonly ComboBox _nam = GiaoDien.Chon(150);
    private readonly RadioButton _taiSuDung = new() { Text = "Tái sử dụng danh sách (LINQ)", AutoSize = true, Checked = true };
    private readonly RadioButton _truyVanMoi = new() { Text = "Truy vấn mới (GROUP BY)", AutoSize = true };
    private readonly Label _lucTai = new() { AutoSize = true, ForeColor = GiaoDien.ChuPhu, Font = GiaoDien.ChuNho, MaximumSize = new Size(260, 0) };
    private readonly TableLayoutPanel _thamSo = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 4, 0, 4) };

    private readonly Label _tieuDe = new() { Dock = DockStyle.Top, Height = 30, Font = GiaoDien.TieuDeNho, AutoEllipsis = true };
    private readonly Label _meta = new() { Dock = DockStyle.Top, Height = 24, ForeColor = GiaoDien.ChuPhu };
    private readonly Luoi<DongThongKeDTO> _luoi = new() { InDam = d => d.Nhan == ThongKeBLL.TongCong };
    private readonly Luoi<DongSoSanh> _soSanh = new();
    private readonly Panel _khungSoSanh = new() { Dock = DockStyle.Top, Height = 0, Padding = new Padding(0, 4, 0, 8) };
    private readonly Label _ketLuan = new() { Dock = DockStyle.Bottom, Height = 26, Font = GiaoDien.ChuDam };
    private readonly BieuDoCot _bieuDo = new() { Dock = DockStyle.Fill };
    private readonly TabControl _tab = new() { Dock = DockStyle.Fill, Padding = new Point(14, 5) };
    private BangThongKeDTO? _bang;
    private List<int> _cacNam = new();

    private sealed record DongSoSanh(string Cach, long ThoiGianMs, int SoCau, int SoDong, string GhiChu);

    public ThongKeMH() : base("Báo cáo thống kê", "Mỗi báo cáo tính được theo 2 cách (yêu cầu 3). Kết quả 2 cách phải giống nhau; so sánh thời gian và số câu SQL.")
    {
        Nut("Xem báo cáo", Keys.F5, LamMoi, chinh: true);
        Phim("Xem báo cáo", Keys.Return, LamMoi);
        Nut("So sánh 2 cách", Keys.Control | Keys.M, SoSanh);
        Nut("Đổi cách tính", Keys.F3, () => (_taiSuDung.Checked ? _truyVanMoi : _taiSuDung).Checked = true);
        Nut("Làm mới dữ liệu đã tải", Keys.Control | Keys.Shift | Keys.R, () =>
        {
            ThongKeBLL.XoaBoNhoDem();
            CapNhatLucTai();
            HopThoai.ThongBao("Đã xóa danh sách tái sử dụng. Lần xem kế tiếp theo cách tái sử dụng sẽ tải lại từ CSDL.");
        });
        KhoangCach();
        Nut("Xuất Excel", Keys.Control | Keys.E, XuatExcel);
        Nut("Xuất PDF", Keys.Control | Keys.P, XuatPdf);
        for (var i = 0; i < CacBaoCao.Length; i++)
        {
            var chiSo = i;
            Phim($"Chọn báo cáo {CacBaoCao[i].Ma}", Keys.Alt | (Keys.D1 + i), () => _ds.SelectedIndex = chiSo);
        }

        // ---- ben trai: chon bao cao + tham so
        foreach (var b in CacBaoCao) _ds.Items.Add(b);
        // FormattingEnabled phai bat thi su kien Format moi duoc goi (mac dinh tat -> hien ToString() cua record)
        _ds.FormattingEnabled = true;
        _ds.Format += (_, e) => { if (e.ListItem is LoaiBaoCao b) e.Value = $"{b.Ten}   (Alt+{Array.IndexOf(CacBaoCao, b) + 1})"; };
        _ds.SelectedIndexChanged += (_, _) => DoiBaoCao();
        Muc.Nap(_kieu, new[] { new Muc("THANG", "Theo tháng"), new Muc("QUY", "Theo quý"), new Muc("NAM", "Theo năm") });
        _tu.Value = new DateTime(DateTime.Today.Year, 1, 1);
        _den.Value = DateTime.Today;
        _thamSo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        var cach = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 4, 0, 4) };
        cach.Controls.Add(_taiSuDung);
        cach.Controls.Add(_truyVanMoi);
        cach.Controls.Add(_lucTai);
        _taiSuDung.CheckedChanged += (_, _) => { if (_taiSuDung.Checked) LamMoi(); };
        _truyVanMoi.CheckedChanged += (_, _) => { if (_truyVanMoi.Checked) LamMoi(); };

        var trai = GiaoDien.Khung(dem: 10);
        trai.Width = 300;
        trai.Dock = DockStyle.Left;
        trai.Controls.Add(cach);
        trai.Controls.Add(GiaoDien.TieuDeMuc("Cách tính"));
        trai.Controls.Add(_thamSo);
        trai.Controls.Add(GiaoDien.TieuDeMuc("Tham số"));
        trai.Controls.Add(_moTaBc);
        trai.Controls.Add(_ds);
        trai.Controls.Add(GiaoDien.TieuDeMuc("Báo cáo"));

        // ---- ben phai: ket qua
        _soSanh.Them("Cách tính", d => d.Cach, 230).So("Thời gian (ms)", d => d.ThoiGianMs, 120).So("Số câu SQL", d => d.SoCau, 100)
            .So("Số dòng", d => d.SoDong, 80).Them("Ghi chú", d => d.GhiChu, 300, fill: true);
        _khungSoSanh.Controls.Add(BoCuc.KhungBang(_soSanh.Grid, "So sánh 2 cách tính", _ketLuan));
        var tabBang = new TabPage("Bảng số liệu") { BackColor = Color.White };
        tabBang.Controls.Add(_luoi.Grid);
        var tabBieuDo = new TabPage("Biểu đồ") { BackColor = Color.White, Padding = new Padding(6) };
        tabBieuDo.Controls.Add(_bieuDo);
        _tab.TabPages.Add(tabBang);
        _tab.TabPages.Add(tabBieuDo);
        var phai = GiaoDien.Khung(dem: 10);
        phai.Controls.Add(_tab);
        phai.Controls.Add(_khungSoSanh);
        phai.Controls.Add(_meta);
        phai.Controls.Add(_tieuDe);

        NoiDung.Controls.Add(phai);
        NoiDung.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 10 });
        NoiDung.Controls.Add(trai);
    }

    private LoaiBaoCao BaoCao => (LoaiBaoCao)(_ds.SelectedItem ?? CacBaoCao[0]);

    private void DoiBaoCao()
    {
        _moTaBc.Text = BaoCao.MoTa;
        _thamSo.SuspendLayout();
        _thamSo.Controls.Clear();
        _thamSo.RowStyles.Clear();
        void Dong(string nhan, Control c)
        {
            _thamSo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _thamSo.Controls.Add(GiaoDien.Nhan(nhan));
            _thamSo.Controls.Add(c);
        }
        var chonNam = Muc.Lay(_nam);
        switch (BaoCao.Ma)
        {
            case "5a":
                Dong("Từ ngày", _tu);
                Dong("Đến ngày", _den);
                break;
            case "5b":
                Dong("Kỳ", _kieu);
                Muc.Nap(_nam, new[] { new Muc(null, "Tất cả các năm") }.Concat(_cacNam.Select(n => new Muc(n.ToString(), n.ToString()))), chonNam);
                Dong("Năm", _nam);
                break;
            case "5d":
                Muc.Nap(_nam, new[] { new Muc(null, "Tất cả (cột = năm)") }.Concat(_cacNam.Select(n => new Muc(n.ToString(), $"{n} (cột = quý)"))), chonNam);
                Dong("Năm", _nam);
                break;
            case "5e":
                Muc.Nap(_nam, _cacNam.Select(n => new Muc(n.ToString(), n.ToString())), chonNam ?? _cacNam.LastOrDefault().ToString());
                Dong("Năm", _nam);
                break;
            default:
                _thamSo.Controls.Add(new Label { Text = "Không cần tham số", AutoSize = true, ForeColor = GiaoDien.ChuPhu });
                break;
        }
        _thamSo.ResumeLayout();
        _khungSoSanh.Height = 0;
        if (IsHandleCreated) LamMoi();
    }

    public override void LamMoi()
    {
        if (_cacNam.Count == 0)
        {
            if (!ThaoTac.Chay("Các năm có dữ liệu", () => new ThongKeBLL().CacNamCoDuLieu(), out var nams)) return;
            _cacNam = nams.Count > 0 ? nams : new List<int> { DateTime.Today.Year };
            if (_ds.SelectedIndex < 0) _ds.SelectedIndex = 0;      // goi DoiBaoCao -> LamMoi
            else DoiBaoCao();
            return;
        }
        var cach = _taiSuDung.Checked ? ThongKeBLL.TaiSuDung : ThongKeBLL.TruyVanMoi;
        if (!ThaoTac.Chay($"Báo cáo {BaoCao.Ma} ({(cach == ThongKeBLL.TaiSuDung ? "tái sử dụng" : "truy vấn mới")})", () => Tinh(cach), out var bang)) return;
        Hien(bang);
    }

    private BangThongKeDTO Tinh(string cach)
    {
        var bll = new ThongKeBLL();
        var nam = int.TryParse(Muc.Lay(_nam), out var n) ? n : (int?)null;
        return BaoCao.Ma switch
        {
            "5a" => bll.TongTienKhoang(_tu.Value.Date, _den.Value.Date, cach),
            "5b" => bll.ThuChiLoiNhuan(Muc.Lay(_kieu) ?? "THANG", nam, cach),
            "5c" => bll.TheoNhanVienNam(cach),
            "5d" => bll.TheoKhachHang(nam, cach),
            _ => bll.TheoSanPhamQuy(nam ?? DateTime.Today.Year, cach)
        };
    }

    private void Hien(BangThongKeDTO bang)
    {
        _bang = bang;
        _tieuDe.Text = bang.TieuDe;
        _meta.Text = $"Cách: {(bang.CachTinh == ThongKeBLL.TaiSuDung ? "tái sử dụng danh sách đã tải (LINQ)" : "truy vấn mới (GROUP BY trên SQL Server)")}"
                     + $"  ·  {bang.ThoiGianMs} ms  ·  {bang.SoCauTruyVan} câu SQL  ·  {Math.Max(0, bang.Dongs.Count - 1)} dòng dữ liệu";
        _luoi.XoaCot();
        _luoi.Them(bang.Cots.Count > 0 && BaoCao.Ma == "5b" ? "Kỳ" : "Đối tượng", d => d.Nhan, 260, fill: BaoCao.Ma is "5a" or "5b");
        for (var i = 0; i < bang.Cots.Count; i++)
        {
            var c = i;
            _luoi.Them(bang.Cots[c], d => c < d.GiaTris.Count ? d.GiaTris[c] : null, bang.Cots[c] == "Số hóa đơn" ? 100 : 130, "N0", canPhai: true,
                mau: d => c < d.GiaTris.Count && d.GiaTris[c] < 0 ? GiaoDien.Do : null);
        }
        _luoi.ChuKhiTrong = "Không có dữ liệu trong kỳ đã chọn";
        _luoi.Nap(bang.Dongs);
        VeBieuDo(bang);
        CapNhatLucTai();
    }

    private void VeBieuDo(BangThongKeDTO bang)
    {
        var dongs = bang.Dongs.Where(d => d.Nhan != ThongKeBLL.TongCong).ToList();
        _bieuDo.TieuDe = bang.TieuDe;
        if (BaoCao.Ma == "5a" || dongs.Count == 0)
        {
            _bieuDo.ChuKhiTrong = BaoCao.Ma == "5a" ? "Báo cáo 5a chỉ có 1 dòng tổng, xem ở bảng số liệu" : "Không có dữ liệu";
            _bieuDo.Xoa();
            return;
        }
        if (BaoCao.Ma == "5b")
        {
            _bieuDo.Dat(dongs.Select(d => d.Nhan).ToList(), new List<BieuDoCot.Chuoi>
            {
                new("Tổng thu", GiaoDien.Den, dongs.Select(d => d.GiaTris[0]).ToList()),
                new("Tổng chi", GiaoDien.Xam, dongs.Select(d => d.GiaTris[1]).ToList()),
                new("Lợi nhuận", GiaoDien.Do, dongs.Select(d => d.GiaTris[2]).ToList()),
            });
            return;
        }
        // bang xoay (doi tuong x ky): 10 doi tuong doanh thu cao nhat, moi ky 1 mau
        var top = dongs.OrderByDescending(d => d.GiaTris[^1]).Take(10).ToList();
        var soKy = bang.Cots.Count - 1;
        _bieuDo.TieuDe = bang.TieuDe + (dongs.Count > 10 ? " (10 cao nhất)" : "");
        _bieuDo.Dat(top.Select(d => d.Nhan).ToList(),
            Enumerable.Range(0, soKy).Select(k => new BieuDoCot.Chuoi(bang.Cots[k], GiaoDien.BangMau[k % GiaoDien.BangMau.Length],
                top.Select(d => d.GiaTris[k]).ToList())).ToList());
    }

    private void CapNhatLucTai()
    {
        var luc = new ThongKeBLL().LucTaiDuLieuTaiSuDung;
        _lucTai.Text = luc == null
            ? "Chưa tải danh sách tái sử dụng (lần đầu sẽ chạy 3 câu SQL: hóa đơn, chi tiết, khoản chi)"
            : $"Danh sách tái sử dụng tải lúc {luc:HH:mm:ss}, dùng lại tối đa {ThongKeBLL.ThoiHanBoNhoDem.TotalMinutes:0} phút "
              + "hoặc tới khi có hóa đơn / phiếu nhập mới từ app này";
    }

    // Chay ca 2 cach, doi chieu tung o
    private void SoSanh()
    {
        if (!ThaoTac.Chay($"So sánh 2 cách báo cáo {BaoCao.Ma}", () => (Tinh(ThongKeBLL.TaiSuDung), Tinh(ThongKeBLL.TruyVanMoi)), out var kq)) return;
        var (a, b) = kq;
        var giong = a.Cots.SequenceEqual(b.Cots) && a.Dongs.Count == b.Dongs.Count
                    && a.Dongs.Zip(b.Dongs).All(p => p.First.Nhan == p.Second.Nhan && p.First.GiaTris.SequenceEqual(p.Second.GiaTris));
        _soSanh.Nap(new[]
        {
            new DongSoSanh("Tái sử dụng (LINQ trên danh sách)", a.ThoiGianMs, a.SoCauTruyVan, a.Dongs.Count,
                a.SoCauTruyVan == 0 ? "Dùng lại danh sách đã tải, không truy vấn" : "Lần đầu: tải danh sách hóa đơn / chi tiết / khoản chi"),
            new DongSoSanh("Truy vấn mới (GROUP BY)", b.ThoiGianMs, b.SoCauTruyVan, b.Dongs.Count, "SQL Server tổng hợp, chỉ trả kết quả"),
        });
        _ketLuan.Text = giong ? "✓ Kết quả 2 cách giống nhau từng ô" : "✗ Kết quả 2 cách KHÁC nhau, kiểm tra lại dữ liệu";
        _ketLuan.ForeColor = giong ? GiaoDien.XanhLa : GiaoDien.Do;
        _khungSoSanh.Height = 150;
        Hien(_taiSuDung.Checked ? a : b);
    }

    private void XuatExcel()
    {
        if (_bang is not { } b) return;
        if (ThaoTac.Chay("Xuất Excel báo cáo", () => new ExcelBLL().XuatBangThongKe(b), out var f)) XuatFile.LuuExcel(FindForm(), f, $"ThongKe_{BaoCao.Ma}");
    }

    private void XuatPdf()
    {
        if (_bang is not { } b) return;
        ThaoTac.Chay("Xuất PDF báo cáo", () => XuatFile.MoPdf(new PdfBLL().BaoCao(b), $"ThongKe_{BaoCao.Ma}_{DateTime.Now:yyyyMMdd_HHmm}"));
    }
}
