using BLL;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tests;

// Test tich hop chay tren CSDL that co du lieu mau (database/QL_BanThoiTrang_DuLieuMau.sql).
// Doi chuoi ket noi bang bien moi truong QLBTT_KET_NOI neu can.
public static class MoiTruong
{
    public static readonly string ChuoiKetNoi = Environment.GetEnvironmentVariable("QLBTT_KET_NOI")
        ?? "Server=localhost;Database=QL_BanThoiTrang;User Id=sa;Password=Strong!Pass123;TrustServerCertificate=True";

    private static bool _da;
    public static void KhoiTao()
    {
        if (_da) return;
        HeThongBLL.CauHinh(ChuoiKetNoi);
        _da = true;
    }
}

public abstract class TestCoSo
{
    protected TestCoSo() => MoiTruong.KhoiTao();

    protected static DTO.SanPhamDTO SanPhamConHang(int toiThieu = 5, int boQua = 0) =>
        new SanPhamBLL().LayTatCa(true).Where(s => s.SoLuong >= toiThieu).OrderByDescending(s => s.SoLuong).Skip(boQua).First();

    protected static int Ton(string maSp) => new SanPhamBLL().LayTheoMa(maSp).SoLuong;

    protected static DTO.PhienDangNhapDTO DangNhap(string ten, DTO.NoiDangNhap noi) =>
        new TaiKhoanBLL().DangNhap(ten, "123456", noi);
}
