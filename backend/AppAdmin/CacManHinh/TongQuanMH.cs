namespace AppAdmin.CacManHinh;

// O so lieu tren trang tong quan: la Button nen Tab / Enter duoc, bam de mo man hinh lien quan
public sealed class OSoLieu : Button
{
    public string SoLieu { get; set; } = "-";
    public string Nhan { get; set; } = "";
    public string Phu { get; set; } = "";
    public bool CanXuLy { get; set; }

    public OSoLieu()
    {
        Size = new Size(214, 108);
        FlatStyle = FlatStyle.Flat;
        BackColor = Color.White;
        Cursor = Cursors.Hand;
        Margin = new Padding(0, 0, 12, 12);
        UseVisualStyleBackColor = false;
        FlatAppearance.BorderColor = GiaoDien.Vien;
        FlatAppearance.MouseOverBackColor = GiaoDien.DongXen;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        using (var but = new Pen(Focused ? GiaoDien.Do : GiaoDien.Vien, Focused ? 2 : 1))
            e.Graphics.DrawRectangle(but, 0, 0, Width - 1, Height - 1);
        if (CanXuLy)
        {
            using var co = new SolidBrush(GiaoDien.Do);
            e.Graphics.FillRectangle(co, 0, 0, 4, Height);
        }
        TextRenderer.DrawText(e.Graphics, Nhan, GiaoDien.Chu, new Point(14, 10), GiaoDien.ChuPhu);
        TextRenderer.DrawText(e.Graphics, SoLieu, GiaoDien.SoLon, new Point(12, 32), CanXuLy ? GiaoDien.Do : GiaoDien.ChuChinh);
        TextRenderer.DrawText(e.Graphics, Phu, GiaoDien.ChuNho, new Rectangle(14, 80, Width - 20, 20), GiaoDien.ChuPhu,
            TextFormatFlags.EndEllipsis);
    }
}

public sealed class TongQuanMH : HaTang.ManHinh
{
    private sealed record DuLieu(int ChoXacNhan, int XinHuy, int DoiTraCho, List<HoanTienDTO> ChoHoan, List<SanPhamDTO> TonThap,
        BangThongKeDTO HomNay, BangThongKeDTO ThangNay, List<KhuyenMaiDTO> KmDangChay, List<ThuChiDTO> ThuChi);

    private readonly Dictionary<string, OSoLieu> _o = new();
    private readonly BieuDoCot _bieuDo = new() { Dock = DockStyle.Fill };
    private readonly Luoi<SanPhamDTO> _tonThap = new();
    private readonly Luoi<KhuyenMaiDTO> _km = new();

    public TongQuanMH() : base("Tổng quan", "Việc cần xử lý và tình hình kinh doanh. Bấm vào ô để mở danh sách tương ứng.")
    {
        Nut("Làm mới", Keys.F5, LamMoi);

        var hangO = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 4, 0, 0) };
        void O(string khoa, string nhan, string manHinh, string? thamSo)
        {
            var o = new OSoLieu { Nhan = nhan };
            o.Click += (_, _) => MoManHinh?.Invoke(manHinh, thamSo);
            _o[khoa] = o;
            hangO.Controls.Add(o);
        }
        O("cho", "Đơn chờ xác nhận", "DonHang", "CHO_XAC_NHAN");
        O("huy", "Khách xin hủy đơn", "DonHang", "XIN_HUY");
        O("doitra", "Đổi trả chờ duyệt", "DoiTra", "CHO_DUYET");
        O("hoan", "Chờ hoàn tiền", "HoanTien", "CHO_HOAN");
        O("ton", $"Sản phẩm tồn dưới {QuyTac.NguongTonThap}", "SanPham", "TON_THAP");
        O("homnay", "Doanh thu hôm nay", "ThongKe", null);
        O("km", "Khuyến mãi đang chạy", "KhuyenMai", "DANG_CHAY");

        _tonThap.Them("Mã SP", s => s.MaSp, 70).Them("Sản phẩm", s => $"{s.TenSp} - {s.MauSac} - {s.KichCo}", 220, fill: true)
            .So("Tồn", s => s.SoLuong, 50);
        _tonThap.ChuKhiTrong = "Không có sản phẩm sắp hết hàng";
        _tonThap.MoDong += _ => MoManHinh?.Invoke("PhieuNhap", null);
        _km.Them("Mã", k => k.MaKm, 60).Them("Chương trình", k => k.TenKm, 200, fill: true).Ngay("Đến", k => k.NgayKt, 90);
        _km.ChuKhiTrong = "Không có khuyến mãi đang chạy";

        var phai = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0) };
        phai.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        phai.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        phai.Controls.Add(BoCuc.KhungBang(_tonThap.Grid, "Sắp hết hàng (Enter: đi nhập hàng)"), 0, 0);
        phai.Controls.Add(BoCuc.KhungBang(_km.Grid, "Khuyến mãi đang chạy"), 0, 1);
        var khungBieuDo = GiaoDien.Khung(dem: 8);
        khungBieuDo.Controls.Add(_bieuDo);
        var chia = BoCuc.ChiaDoi(khungBieuDo, phai, 62);
        NoiDung.Controls.Add(chia);
        NoiDung.Controls.Add(hangO);
    }

    protected override bool LamMoiKhiMoLai => true;

    public override void LamMoi()
    {
        var homNay = DateTime.Today;
        if (!ThaoTac.Chay("Tải tổng quan", () =>
            {
                var don = new DonDatHangBLL();
                var tk = new ThongKeBLL();
                return new DuLieu(
                    don.LayDanhSach("CHO_XAC_NHAN").Count,
                    don.LayDanhSach(yeuCauHuy: "CHO_DUYET").Count,
                    new DoiTraBLL().LayDanhSach("CHO_DUYET").Count,
                    new HoanTienBLL().LayDanhSach("CHO_HOAN"),
                    new SanPhamBLL().LayTonThap(),
                    tk.TongTienKhoang(homNay, homNay, ThongKeBLL.TruyVanMoi),
                    tk.TongTienKhoang(new DateTime(homNay.Year, homNay.Month, 1), homNay, ThongKeBLL.TruyVanMoi),
                    new KhuyenMaiBLL().LayDangChay(),
                    tk.ThuChiLoiNhuanDs("THANG", homNay.Year, ThongKeBLL.TruyVanMoi));
            }, out var d)) return;

        Dat("cho", d.ChoXacNhan.ToString("N0"), "Xác nhận để trừ kho, lập hóa đơn", d.ChoXacNhan > 0);
        Dat("huy", d.XinHuy.ToString("N0"), "Duyệt hoặc từ chối trước khi giao", d.XinHuy > 0);
        Dat("doitra", d.DoiTraCho.ToString("N0"), "Yêu cầu đổi / trả từ web", d.DoiTraCho > 0);
        Dat("hoan", d.ChoHoan.Count.ToString("N0"), $"Tổng {GiaoDien.Tien(d.ChoHoan.Sum(h => h.SoTien))}", d.ChoHoan.Count > 0);
        Dat("ton", d.TonThap.Count.ToString("N0"), "Lập phiếu nhập để bổ sung", d.TonThap.Count > 0);
        Dat("homnay", GiaoDien.TienGon(d.HomNay.Dongs[0].GiaTris[1]),
            $"{d.HomNay.Dongs[0].GiaTris[0]:N0} HĐ · tháng này {GiaoDien.TienGon(d.ThangNay.Dongs[0].GiaTris[1])}", false);
        Dat("km", d.KmDangChay.Count.ToString("N0"), "Gồm voucher, KM sản phẩm, hóa đơn", false);

        _tonThap.Nap(d.TonThap.OrderBy(s => s.SoLuong));
        _km.Nap(d.KmDangChay);
        _bieuDo.TieuDe = $"Thu - chi - lợi nhuận theo tháng năm {homNay.Year}";
        _bieuDo.Dat(d.ThuChi.Select(x => x.Ky[..2]).Select(t => "T" + t.TrimStart('0')).ToList(), new List<BieuDoCot.Chuoi>
        {
            new("Tổng thu", GiaoDien.Den, d.ThuChi.Select(x => x.TongThu).ToList()),
            new("Tổng chi (nhập hàng + hoàn trả)", GiaoDien.Xam, d.ThuChi.Select(x => x.TongChi).ToList()),
            new("Lợi nhuận", GiaoDien.Do, d.ThuChi.Select(x => x.LoiNhuan).ToList()),
        });
    }

    private void Dat(string khoa, string so, string phu, bool canXuLy)
    {
        var o = _o[khoa];
        o.SoLieu = so;
        o.Phu = phu;
        o.CanXuLy = canXuLy;
        o.Invalidate();
    }
}
