using DTO;

namespace DAL;

// Yeu cau 1: ghi lai moi cau SQL da chay cung file + so dong trong DAL da goi no.
// Giu 500 cau gan nhat trong bo nho; app admin co man hinh "Nhat ky SQL" de xem.
public static class NhatKySql
{
    private const int ToiDa = 500;
    private static readonly LinkedList<NhatKySqlDTO> _ds = new();
    private static readonly object _khoa = new();
    private static long _tongSoCau;

    public static bool BatGhi { get; set; } = true;

    // In ra console cua WebApi / Output cua Visual Studio de thay ngay khi chay
    public static bool InRaConsole { get; set; }

    public static long TongSoCau => Interlocked.Read(ref _tongSoCau);

    // Dem so cau SQL trong 1 luong xu ly (1 request / 1 thao tac), khong bi lan voi request khac chay song song
    private static readonly AsyncLocal<BoDem?> _boDem = new();

    public sealed class BoDem : IDisposable
    {
        internal readonly BoDem? Cha;
        public int SoCau { get; internal set; }
        public NhatKySqlDTO? CauCuoi { get; internal set; }
        internal BoDem(BoDem? cha) { Cha = cha; }
        public void Dispose() { _boDem.Value = Cha; }
    }

    // using var dem = NhatKySql.BatDauDem(); ... dem.SoCau
    public static BoDem BatDauDem()
    {
        var dem = new BoDem(_boDem.Value);
        _boDem.Value = dem;
        return dem;
    }

    // Cau SQL ky thuat (vd kiem tra tai khoan con hoat dong moi request) van ghi nhat ky
    // nhung khong tinh vao so cau SQL cua thao tac dang dem (header X-So-Cau-Sql)
    public static IDisposable TamNgungDem()
    {
        var cu = _boDem.Value;
        _boDem.Value = null;
        return new KhoiPhuc(() => _boDem.Value = cu);
    }

    private sealed class KhoiPhuc : IDisposable
    {
        private readonly Action _lam;
        public KhoiPhuc(Action lam) => _lam = lam;
        public void Dispose() => _lam();
    }

    internal static void Ghi(NhatKySqlDTO dong)
    {
        Interlocked.Increment(ref _tongSoCau);
        for (var dem = _boDem.Value; dem != null; dem = dem.Cha) { dem.SoCau++; dem.CauCuoi = dong; }
        if (!BatGhi) return;
        lock (_khoa)
        {
            _ds.AddFirst(dong);
            if (_ds.Count > ToiDa) _ds.RemoveLast();
        }
        if (InRaConsole)
            Console.WriteLine($"[SQL] {dong.File}:{dong.Dong} {dong.HamGoi} ({dong.ThoiGianMs} ms, {dong.SoDong} dong)\n      {dong.CauSql}");
    }

    public static List<NhatKySqlDTO> LayGanNhat(int soLuong = 100)
    {
        lock (_khoa) return _ds.Take(soLuong).ToList();
    }

    public static void Xoa()
    {
        lock (_khoa) _ds.Clear();
    }
}
