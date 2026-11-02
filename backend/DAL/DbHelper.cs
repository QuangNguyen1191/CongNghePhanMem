using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dapper;
using DTO;
using Microsoft.Data.SqlClient;

namespace DAL;

// Moi cau SQL trong DAL deu chay qua cac ham o day.
// [CallerFilePath] / [CallerLineNumber] cho biet chinh xac file va dong trong DAL da goi cau SQL (yeu cau 1).
public static partial class DbHelper
{
    static DbHelper()
    {
        // cot ma_sp trong CSDL <-> thuoc tinh MaSp trong DTO
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public static List<T> Query<T>(string sql, object? thamSo = null, IDbTransaction? tx = null,
        [CallerFilePath] string file = "", [CallerLineNumber] int dong = 0, [CallerMemberName] string ham = "")
        => Chay(sql, thamSo, tx, file, dong, ham, cn => cn.Query<T>(sql, thamSo, tx).ToList(), kq => kq.Count);

    public static T? QueryFirstOrDefault<T>(string sql, object? thamSo = null, IDbTransaction? tx = null,
        [CallerFilePath] string file = "", [CallerLineNumber] int dong = 0, [CallerMemberName] string ham = "")
        => Chay(sql, thamSo, tx, file, dong, ham, cn => cn.QueryFirstOrDefault<T>(sql, thamSo, tx), kq => kq == null ? 0 : 1);

    public static T? ExecuteScalar<T>(string sql, object? thamSo = null, IDbTransaction? tx = null,
        [CallerFilePath] string file = "", [CallerLineNumber] int dong = 0, [CallerMemberName] string ham = "")
        => Chay(sql, thamSo, tx, file, dong, ham, cn => cn.ExecuteScalar<T>(sql, thamSo, tx), _ => 1);

    // Tra ve so dong bi anh huong
    public static int Execute(string sql, object? thamSo = null, IDbTransaction? tx = null,
        [CallerFilePath] string file = "", [CallerLineNumber] int dong = 0, [CallerMemberName] string ham = "")
        => Chay(sql, thamSo, tx, file, dong, ham, cn => cn.Execute(sql, thamSo, tx), kq => kq);

    private static T Chay<T>(string sql, object? thamSo, IDbTransaction? tx, string file, int dong, string ham,
        Func<IDbConnection, T> lam, Func<T, int> demDong)
    {
        var dongHo = Stopwatch.StartNew();
        SqlConnection? cnRieng = null;
        try
        {
            var cn = tx?.Connection ?? (cnRieng = KetNoi.Mo());
            var kq = lam(cn);
            dongHo.Stop();
            NhatKySql.Ghi(new NhatKySqlDTO
            {
                ThoiDiem = DateTime.Now,
                CauSql = RutGon(sql),
                ThamSo = MoTaThamSo(thamSo),
                File = Path.GetFileName(file),
                Dong = dong,
                HamGoi = ham,
                ThoiGianMs = dongHo.ElapsedMilliseconds,
                SoDong = demDong(kq)
            });
            return kq;
        }
        finally
        {
            cnRieng?.Dispose();
        }
    }

    // Chay nhieu cau SQL trong 1 giao dich: loi o bat ky buoc nao thi ROLLBACK het
    public static T GiaoDich<T>(Func<IDbTransaction, T> lam)
    {
        using var cn = KetNoi.Mo();
        using var tx = cn.BeginTransaction();
        try
        {
            var kq = lam(tx);
            tx.Commit();
            return kq;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public static void GiaoDich(Action<IDbTransaction> lam) => GiaoDich<bool>(tx => { lam(tx); return true; });

    // Sinh ma tiep theo: HD00199 -> HD00200. Khoa bang (UPDLOCK, HOLDLOCK) nen 2 nguoi lap cung luc khong trung ma.
    public static string SinhMa(string bang, string cot, string tienTo, int soChuSo, IDbTransaction tx)
    {
        if (!TenHopLe().IsMatch(bang) || !TenHopLe().IsMatch(cot) || !TenHopLe().IsMatch(tienTo))
            throw new ArgumentException("Ten bang / cot / tien to khong hop le");
        var sql = $"SELECT MAX({cot}) FROM {bang} WITH (UPDLOCK, HOLDLOCK) " +
                  $"WHERE {cot} LIKE @mau AND LEN({cot}) = @doDai";
        var maCuoi = ExecuteScalar<string?>(sql, new { mau = tienTo + "%", doDai = tienTo.Length + soChuSo }, tx);
        var so = maCuoi == null ? 0 : int.Parse(maCuoi[tienTo.Length..]);
        return tienTo + (so + 1).ToString(new string('0', soChuSo));
    }

    [GeneratedRegex("^[A-Za-z_]+$")]
    private static partial Regex TenHopLe();

    private static string RutGon(string sql) => Regex.Replace(sql.Trim(), @"\s+", " ");

    private static string MoTaThamSo(object? thamSo)
    {
        if (thamSo == null) return "";
        if (thamSo is DynamicParameters dp)
            return string.Join(", ", dp.ParameterNames.Select(t => $"@{t}={dp.Get<object?>(t)}"));
        return string.Join(", ", thamSo.GetType().GetProperties()
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p => $"@{p.Name}={(p.Name.Contains("MatKhau", StringComparison.OrdinalIgnoreCase) ? "***" : GiaTri(p.GetValue(thamSo)))}"));
    }

    private static string GiaTri(object? v) => v switch
    {
        null => "NULL",
        string s => $"'{s}'",
        System.Collections.IEnumerable e => "(" + string.Join(",", e.Cast<object>()) + ")",
        _ => v.ToString() ?? ""
    };
}
