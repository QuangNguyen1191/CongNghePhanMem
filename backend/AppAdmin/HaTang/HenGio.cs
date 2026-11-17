using System.Text.RegularExpressions;

namespace AppAdmin.HaTang;

// Hen chay sau 1 khoang ngan: giu phim mui ten luot qua nhieu dong thi chi nap chi tiet cua dong cuoi
// (khong ban 1 cau SQL cho moi dong luot qua)
public sealed class HenGio : IDisposable
{
    private readonly System.Windows.Forms.Timer _dongHo;
    private Action? _viec;

    public HenGio(int miliGiay = 150)
    {
        _dongHo = new System.Windows.Forms.Timer { Interval = miliGiay };
        _dongHo.Tick += (_, _) =>
        {
            _dongHo.Stop();
            var v = _viec;
            _viec = null;
            v?.Invoke();
        };
    }

    public void Hen(Action viec)
    {
        _viec = viec;
        _dongHo.Stop();
        _dongHo.Start();
    }

    public void Dispose() => _dongHo.Dispose();
}

public static partial class TuKhoa
{
    // "HD00012", "hd12" -> la ma chung tu co tien to
    public static bool LaMa(string s, string tienTo) =>
        s.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase) && s.Length > tienTo.Length && s[tienTo.Length..].All(char.IsDigit);

    public static bool LaSo(string s) => s.Length > 0 && SoDienThoai().IsMatch(s);

    [GeneratedRegex(@"^[0-9+ .\-]+$")]
    private static partial Regex SoDienThoai();

    // Dieu kien tim nhanh dung chung cho TimKiemBLL.Tim... (truy van moi) va TimKiemBLL.Loc... (tai su dung)
    public static DieuKienTimDTO Dk(string truong, string toanTu, object giaTri) =>
        new() { Truong = truong, ToanTu = toanTu, GiaTri = giaTri is DateTime d ? d.ToString("dd/MM/yyyy") : Convert.ToString(giaTri, GiaoDien.Vn) ?? "" };
}
