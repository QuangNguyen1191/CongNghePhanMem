namespace DTO;

// 1 dong cua bao cao dang bang: nhan dong (thang / quy / nam...) + cac cot gia tri
public class BangThongKeDTO
{
    public string TieuDe { get; set; } = "";
    public string CachTinh { get; set; } = "";      // TAI_SU_DUNG | TRUY_VAN_MOI
    public List<string> Cots { get; set; } = new(); // ten cot gia tri, cot cuoi la "Tổng cộng"
    public List<DongThongKeDTO> Dongs { get; set; } = new();
    public long ThoiGianMs { get; set; }
    public int SoCauTruyVan { get; set; }
}

public class DongThongKeDTO
{
    public string Nhan { get; set; } = "";
    public List<decimal> GiaTris { get; set; } = new();
}

// Thu - chi - loi nhuan theo ky
public class ThuChiDTO
{
    public string Ky { get; set; } = "";
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
    public decimal LoiNhuan => TongThu - TongChi;
}

// Dieu kien tim kiem nang cao: truong + toan tu + gia tri, ghep AND / OR
public class DieuKienTimDTO
{
    public string Truong { get; set; } = "";        // vd: tong_tien, ngay_lap
    public string ToanTu { get; set; } = "=";       // = > < >= <= LIKE
    public string GiaTri { get; set; } = "";
}

public class TimKiemNangCaoDTO
{
    public List<DieuKienTimDTO> DieuKiens { get; set; } = new();
    public bool KhopTatCa { get; set; } = true;     // true: AND (giao), false: OR (hop)
}

// 1 dong nhat ky SQL (yeu cau 1)
public class NhatKySqlDTO
{
    public DateTime ThoiDiem { get; set; }
    public string CauSql { get; set; } = "";
    public string ThamSo { get; set; } = "";
    public string File { get; set; } = "";
    public int Dong { get; set; }
    public string HamGoi { get; set; } = "";
    public long ThoiGianMs { get; set; }
    public int SoDong { get; set; }
}

// Ket qua doc file Excel (yeu cau 7): cac dong hop le de xem truoc + danh sach loi theo so dong
public class KetQuaDocExcelDTO<T>
{
    public List<T> HopLe { get; set; } = new();
    public List<LoiDongExcelDTO> Lois { get; set; } = new();
    public int TongSoDong { get; set; }
}

public class LoiDongExcelDTO
{
    public int Dong { get; set; }               // so dong trong file Excel (dong tieu de la 1)
    public string ThongBao { get; set; } = "";
}
