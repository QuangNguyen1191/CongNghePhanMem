"""Sinh file QL_BanThoiTrang_DuLieuMau.sql (du lieu mau cho CSDL v3).

Chay:  python3 gen_dulieumau.py > ../QL_BanThoiTrang_DuLieuMau.sql
Dung random.seed co dinh nen moi lan chay ra cung 1 bo du lieu.
Mo phong theo thu tu thoi gian: nhap hang truoc, ban sau, ton kho khong bao gio am.
"""
import json
import os
import random
from datetime import date, datetime, timedelta

random.seed(20261029)

# Mat khau mau cho moi tai khoan: 123456 (BCrypt cost 11)
HASH = "$2b$11$0Usou1Kfgr3Gm4VYw/NoL.LDRBbbGgVcn50Lzi2ryAg/kYHbWe.rS"

START, END = date(2024, 1, 2), date(2026, 10, 28)
out = []


def q(v):
    if v is None:
        return "NULL"
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, (int, float)):
        return f"{v:.2f}" if isinstance(v, float) else str(v)
    if isinstance(v, datetime):
        return f"'{v:%Y-%m-%d %H:%M:%S}'"
    if isinstance(v, date):
        return f"'{v:%Y-%m-%d}'"
    s = str(v).replace("'", "''")
    return f"N'{s}'" if any(ord(c) > 127 for c in s) else f"'{s}'"


def insert(table, cols, rows, chunk=50):
    if not rows:
        return
    out.append(f"\n-- {table}: {len(rows)} dong")
    for i in range(0, len(rows), chunk):
        part = rows[i:i + chunk]
        vals = ",\n  ".join("(" + ", ".join(q(v) for v in r) + ")" for r in part)
        out.append(f"INSERT INTO {table} ({', '.join(cols)}) VALUES\n  {vals};")


def money(x):
    return float(round(x))


# ------------------------------------------------------------------ danh muc
loai = [("LSP01", "Áo thun"), ("LSP02", "Áo sơ mi"), ("LSP03", "Áo khoác"),
        ("LSP04", "Quần jean"), ("LSP05", "Quần kaki"), ("LSP06", "Váy đầm")]

# San pham that cua UNIQLO Viet Nam (ten, gia, chat lieu, mo ta, mau, size, anh)
# lay ngay 02/11/2026 tu uniqlo.com/vn, luu o tools/uniqlo_catalog.json.
# Moi mau (ten_sp) -> nhieu SKU = mau sac x kich co; hinh_anh = anh dau tien cua mau sac.
# Anh nam o web-ban-hang/public/images/uniqlo/<uniqloId>/ (xem NGUON.md, ban quyen thuoc UNIQLO).
CATALOG = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "uniqlo_catalog.json"),
                         encoding="utf-8"))
sp = []  # dict
for m in CATALOG:
    for mau in m["mauSacs"]:
        for size in m["kichCos"]:
            i = len(sp) + 1
            gia = float(m["donGia"])
            sp.append(dict(ma=f"SP{i:03d}", ten=m["tenSp"], loai=m["maLoaiSp"], cl=m["chatLieu"], mo_ta=m["moTa"],
                           gia=gia, size=size, mau=mau["ten"], anh=mau["anh"][0], uniqlo=m["uniqloId"],
                           gia_nhap=money(gia * random.uniform(0.45, 0.55) / 1000) * 1000, ton=0))


def skus(ten_sp, mau=None):
    """Cac SKU cua 1 mau san pham (loc them theo mau sac neu can)."""
    return [s for s in sp if s["ten"] == ten_sp and (mau is None or s["mau"] == mau)]


sp_ngung = {skus("Áo Thun Mini", "Nâu")[-1]["ma"]}  # 1 SKU ngung ban de demo trang thai

# ------------------------------------------------------------- tai khoan, NV
nv = [  # ma_nv, ma_tk, username, vai_tro, ho_ten, chuc_vu, ngay_vao, luong, sdt, email
    ("NV001", "TK001", "giamdoc", "ADMIN", "Nguyễn Minh Khang", "Giám đốc", date(2023, 6, 1), 30000000, "0901000001", "khang.nm@uniqlo-demo.vn"),
    ("NV002", "TK002", "quanly", "ADMIN", "Trần Thu Hà", "Quản lý cửa hàng", date(2023, 8, 15), 18000000, "0901000002", "ha.tt@uniqlo-demo.vn"),
    ("NV003", "TK003", "thungan1", "NHAN_VIEN", "Lê Hoàng Nam", "Thu ngân", date(2023, 12, 1), 9000000, "0901000003", "nam.lh@uniqlo-demo.vn"),
    ("NV004", "TK004", "thungan2", "NHAN_VIEN", "Phạm Ngọc Lan", "Thu ngân", date(2024, 3, 1), 9000000, "0901000004", "lan.pn@uniqlo-demo.vn"),
    ("NV005", "TK005", "thukho", "NHAN_VIEN", "Võ Đức Thịnh", "Thủ kho", date(2023, 12, 1), 10000000, "0901000005", "thinh.vd@uniqlo-demo.vn"),
    ("NV006", "TK006", "banhangonline", "NHAN_VIEN", "Đặng Mai Anh", "Nhân viên bán hàng online", date(2024, 2, 1), 9500000, "0901000006", "anh.dm@uniqlo-demo.vn"),
]
THU_NGAN = ["NV003", "NV004"]
NV_DON = ["NV006", "NV002"]

ho = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ"]
dem = ["Văn", "Thị", "Minh", "Ngọc", "Quốc", "Thanh", "Gia", "Bảo", "Hữu", "Thu"]
ten_ = ["An", "Bình", "Chi", "Dũng", "Giang", "Hạnh", "Huy", "Khoa", "Linh", "My", "Nhi", "Phúc",
        "Quân", "Sơn", "Tâm", "Trang", "Tú", "Uyên", "Vy", "Yến"]
tinh = (["TP Hồ Chí Minh"] * 8 + ["Hà Nội"] * 6 + ["Đà Nẵng"] * 3 + ["Cần Thơ"] * 2 + ["Hải Phòng"])
duong = {"TP Hồ Chí Minh": ["Nguyễn Trãi, Quận 1", "Lê Văn Sỹ, Quận 3", "Cách Mạng Tháng 8, Quận 10", "Phan Xích Long, Phú Nhuận"],
         "Hà Nội": ["Kim Mã, Ba Đình", "Xuân Thủy, Cầu Giấy", "Bà Triệu, Hai Bà Trưng"],
         "Đà Nẵng": ["Nguyễn Văn Linh, Hải Châu", "Ngô Quyền, Sơn Trà"],
         "Cần Thơ": ["30 Tháng 4, Ninh Kiều"], "Hải Phòng": ["Lạch Tray, Ngô Quyền"]}
kh = []
for i in range(20):
    t = tinh[i]
    co_tk = i < 15  # 15 khach co tai khoan web, 5 khach chi mua tai quay
    kh.append(dict(ma=f"KH{i+1:03d}", tk=f"TK{i+101:03d}" if co_tk else None, user=f"khach{i+1:02d}",
                   ten=f"{ho[i % 12]} {dem[i % 10]} {ten_[i]}", sdt=f"09{12000000 + i * 7919:08d}",
                   email=f"khach{i+1:02d}@gmail.com" if co_tk else None,
                   sinh=date(1985 + i % 18, 1 + i % 12, 1 + (i * 3) % 27), tinh=t,
                   dia_chi=f"{10 + i * 7} {random.choice(duong[t])}, {t}", tong=0.0))
kh_tk = [k for k in kh if k["tk"]]
# khach mua nhieu it khac nhau de co du 4 hang thanh vien
KH_W = [9, 7, 6, 5, 4, 4, 3, 3, 2, 2, 2, 1, 1, 1, 1, 2, 1, 1, 1, 1]

ncc = [("NCC01", "Công ty May mặc Việt Tiến", "07 Lê Minh Xuân, Tân Bình, TP Hồ Chí Minh", "02838640800", "lienhe@viettien-demo.vn", "0300401524", "HOP_TAC"),
       ("NCC02", "Tổng công ty May 10", "Sài Đồng, Long Biên, Hà Nội", "02438276923", "info@may10-demo.vn", "0100101308", "HOP_TAC"),
       ("NCC03", "Công ty Dệt may Thành Công", "36 Tây Thạnh, Tân Phú, TP Hồ Chí Minh", "02838153962", "sales@thanhcong-demo.vn", "0300398889", "HOP_TAC"),
       ("NCC04", "Công ty Thời trang Phương Đông", "12 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", "02363822111", None, None, "NGUNG_HOP_TAC")]

# ---------------------------------------------------------------- khuyen mai
km = [("KM01", "Giảm giá mùa hè 2025", "SAN_PHAM", None, date(2025, 6, 1), date(2025, 8, 31)),
      ("KM02", "Hóa đơn từ 1 triệu giảm 5%", "HOA_DON", None, date(2024, 1, 1), date(2026, 12, 31)),
      ("KM03", "Voucher khách hàng mới", "VOUCHER", None, date(2024, 1, 1), date(2026, 12, 31)),
      ("KM04", "Ngày hội Black Friday 2025", "SAN_PHAM", 20.0, date(2025, 11, 28), date(2025, 11, 30)),
      ("KM05", "Ưu đãi giới hạn thu đông 2026", "SAN_PHAM", None, date(2026, 10, 20), date(2026, 12, 31)),
      # v3: tao tai khoan duoc giam 10% cho 3 don dau, toi da 100.000d / don
      ("KM06", "Ưu đãi thành viên mới", "THANH_VIEN_MOI", 10.0, date(2026, 1, 1), date(2027, 12, 31))]
km_tvm = [("KM06", 3, 10.0, 100000.0)]
km_sp = []
# KM01 (da ket thuc): ao thun + vay mua he. SP001 phai co trong KM01 (KiemThu_QL_BanThoiTrang.sql dung)
for ten, p in [("Áo Thun Mini", 10.0), ("AIRism Cotton Áo Thun Cổ Tròn", 10.0), ("Chân Váy Xếp Ly Vải Nylon", 15.0)]:
    km_sp += [("KM01", s["ma"], p) for s in skus(ten)]
# KM04 (da ket thuc): ao khoac
for ten in ["Áo Khoác Giả Lông Cừu Loại Dày Kéo Khóa", "Áo Khoác Giả Lông Cừu Cản Gió Kéo Khóa"]:
    km_sp += [("KM04", s["ma"], 20.0) for s in skus(ten)]
# KM05 (dang chay): dung muc giam that cua UNIQLO tai ngay lay du lieu (gia KM / gia goc)
for m in CATALOG:
    if m["giaKhuyenMai"]:
        p = round((1 - m["giaKhuyenMai"] / m["donGia"]) * 100, 2)
        km_sp += [("KM05", s["ma"], p) for s in skus(m["tenSp"])]
km_hd = [("KM02", 1000000.0, 5.0)]
voucher = [("KM03", "WELCOME10", 10.0), ("KM03", "FREESHIP5", 5.0)]
VOUCHER_PCT = {v[1]: v[2] for v in voucher}


def gia_ban(s, d):
    """Gia ban tai ngay d (co KM san pham thi giam), tra ve (gia, ma_km)."""
    for mk, ms, p in km_sp:
        k = next(x for x in km if x[0] == mk)
        if ms == s["ma"] and k[4] <= d <= k[5]:
            return money(s["gia"] * (100 - p) / 100), mk
    return s["gia"], None


def ap_km_hoa_don(tam_tinh, d):
    k = next(x for x in km if x[0] == "KM02")
    if k[4] <= d <= k[5] and tam_tinh >= km_hd[0][1]:
        return money(tam_tinh * km_hd[0][2] / 100), "KM02"
    return 0.0, None


# ------------------------------------------------------- mo phong theo ngay
phieu_nhap, ct_pn = [], []
hoa_don, ct_hd = [], []
don, ct_don = [], []
doi_tra, ct_dt = [], []
seq = {"PN": 0, "HD": 0, "DH": 0, "DT": 0}


def next_id(p):
    seq[p] += 1
    return f"{p}{seq[p]:05d}"


def nhap_hang(d, ds_sp, ma_ncc=None):
    ma = next_id("PN")
    ma_ncc = ma_ncc or random.choice(["NCC01", "NCC02", "NCC03"])
    tong = 0.0
    for s, sl in ds_sp:
        tt = s["gia_nhap"] * sl
        ct_pn.append((ma, s["ma"], sl, s["gia_nhap"], tt))
        s["ton"] += sl
        tong += tt
    phieu_nhap.append((ma, ma_ncc, "NV005", d, tong))


# nhap dau ky: moi SKU 10-15 cai, chia 3 phieu theo nha cung cap
for idx, ma_ncc in enumerate(["NCC01", "NCC02", "NCC03"]):
    nhap_hang(START, [(s, random.randint(10, 15)) for s in sp[idx::3]], ma_ncc)


def chon_gio(d, n_max=3):
    """Chon 1-n_max dong hang con ton, tra ve list (sp, so_luong)."""
    con = [s for s in sp if s["ton"] > 0 and s["ma"] not in sp_ngung]
    random.shuffle(con)
    gio = []
    for s in con[:random.randint(1, n_max)]:
        sl = min(s["ton"], random.choice([1, 1, 1, 1, 2]))
        gio.append((s, sl))
    return gio


def lap_hoa_don(d, gio, ma_nv, ma_kh, pttt, ma_don=None, trang_thai="DA_THANH_TOAN", voucher_ma=None):
    ma = next_id("HD")
    tam = 0.0
    lines = []
    for s, sl in gio:
        g, _ = gia_ban(s, d)
        tt = g * sl
        lines.append((ma, s["ma"], sl, g, tt))
        tam += tt
    if voucher_ma:
        giam, mkm = money(tam * VOUCHER_PCT[voucher_ma] / 100), "KM03"
    else:
        giam, mkm = ap_km_hoa_don(tam, d)
    if mkm is None:  # ghi nhan KM san pham neu co dong duoc giam
        mkm = next((gia_ban(s, d)[1] for s, _ in gio if gia_ban(s, d)[1]), None)
    tong = tam - giam
    ct_hd.extend(lines)
    hoa_don.append([ma, ma_don, ma_kh, ma_nv, d, trang_thai, mkm, giam, pttt, tong])
    if trang_thai != "DA_HUY":
        for s, sl in gio:
            s["ton"] -= sl
    if trang_thai == "DA_THANH_TOAN" and ma_kh:
        next(k for k in kh if k["ma"] == ma_kh)["tong"] += tong
    return ma, tam, giam, tong


d = START + timedelta(days=1)
while d <= END:
    # nhap bo sung dau moi thang cho SKU sap het
    if d.day == 1:
        thieu = [s for s in sp if s["ton"] < 4 and s["ma"] not in sp_ngung]
        if thieu:
            nhap_hang(d, [(s, random.randint(10, 20)) for s in thieu])
    # ban tai quay: trung binh ~0.15 hoa don/ngay, cuoi tuan dong hon
    p = 0.13 if d.weekday() < 5 else 0.22
    if d.month in (11, 12):
        p *= 1.4
    if random.random() < p:
        gio = chon_gio(d)
        if gio:
            ma_kh = random.choices([k["ma"] for k in kh] + [None], KH_W + [12])[0]
            trang = "DA_HUY" if random.random() < 0.03 else "DA_THANH_TOAN"
            lap_hoa_don(d, gio, random.choice(THU_NGAN), ma_kh,
                        random.choice(["TIEN_MAT", "TIEN_MAT", "CHUYEN_KHOAN"]), trang_thai=trang)
    # don online: ~0.06 don/ngay
    p_don = 0.7 if (END - d).days <= 10 else 0.06
    if random.random() < p_don:
        k = random.choices(kh_tk, KH_W[:len(kh_tk)])[0]
        gio = chon_gio(d, 3)
        if gio:
            ma_don = next_id("DH")
            ngay_dat = datetime(d.year, d.month, d.day, random.randint(8, 22), random.randint(0, 59))
            con_ngay = (END - d).days
            if con_ngay <= 3:
                tt_don = random.choice(["CHO_XAC_NHAN", "DA_XAC_NHAN"])
            elif con_ngay <= 10:
                tt_don = random.choice(["DANG_GIAO", "DANG_GIAO", "HOAN_TAT"])
            else:
                tt_don = random.choices(["HOAN_TAT", "DA_HUY", "TU_CHOI"], [85, 10, 5])[0]
            vc = "WELCOME10" if random.random() < 0.15 else None
            tam = sum(gia_ban(s, d)[0] * sl for s, sl in gio)
            giam = money(tam * VOUCHER_PCT[vc] / 100) if vc else 0.0
            # dia chi giao: thuong la dia chi cua khach; KH001 (TP HCM) co gui quà ra Ha Noi
            if k["ma"] == "KH001" and random.random() < 0.5:
                dc = "25 Kim Mã, Ba Đình, Hà Nội"
            else:
                dc = k["dia_chi"]
            ma_nv_don = None if tt_don == "CHO_XAC_NHAN" else random.choice(NV_DON)
            don.append([ma_don, k["ma"], ma_nv_don, ngay_dat, tt_don, dc, k["sdt"],
                        random.choice(["COD", "COD", "CHUYEN_KHOAN", "ONLINE"]), vc, tam - giam, k["ten"], giam])
            for s, sl in gio:
                g, _ = gia_ban(s, d)
                ct_don.append((ma_don, s["ma"], sl, g, g * sl))
            # xac nhan don -> tru ton + lap hoa don
            if tt_don in ("DA_XAC_NHAN", "DANG_GIAO", "HOAN_TAT"):
                ngay_hd = d + timedelta(days=1 if tt_don != "DA_XAC_NHAN" else 0)
                ngay_hd = min(ngay_hd, END)
                pt = don[-1][7]
                pt_hd = pt if pt != "COD" else "COD"
                trang_hd = "DA_THANH_TOAN" if (tt_don == "HOAN_TAT" or pt != "COD") else "CHO_THANH_TOAN"
                lap_hoa_don(ngay_hd, gio, ma_nv_don, k["ma"], pt_hd, ma_don=ma_don,
                            trang_thai=trang_hd, voucher_ma=vc)
    d += timedelta(days=1)

# ---------------------------------------------------------------- doi tra
hd_dtt = [h for h in hoa_don if h[5] == "DA_THANH_TOAN" and h[2] and h[4] < date(2026, 10, 1)]
random.shuffle(hd_dtt)
kich_ban = ["TRA_HOAN_TAT"] * 4 + ["DOI_HOAN_TAT"] * 3 + ["TRA_TU_CHOI", "TRA_CHO_DUYET"]
for kb, h in zip(kich_ban, hd_dtt):
    ma = next_id("DT")
    dong = next(c for c in ct_hd if c[0] == h[0])
    s = next(x for x in sp if x["ma"] == dong[1])
    ngay = h[4] + timedelta(days=random.randint(2, 10))
    loai_dt = "TRA" if kb.startswith("TRA") else "DOI"
    if kb.endswith("CHO_DUYET"):
        ngay = date(2026, 10, 27)
    trang = {"HOAN_TAT": "HOAN_TAT", "TU_CHOI": "TU_CHOI", "CHO_DUYET": "CHO_DUYET"}[kb.split("_", 1)[1]]
    ma_nv_dt = None if trang == "CHO_DUYET" else "NV002"
    ly_do = {"TRA": "Sản phẩm không vừa, khách muốn trả lại", "DOI": "Khách muốn đổi sang size khác"}[loai_dt]
    if trang == "TU_CHOI":
        ly_do = "Sản phẩm đã qua sử dụng, quá hạn đổi trả"
    tien_hoan = dong[3] * 1 if (loai_dt == "TRA" and trang == "HOAN_TAT") else 0.0
    ngay_hoan = ngay if tien_hoan else None
    doi_tra.append((ma, h[0], ma_nv_dt, loai_dt, ly_do, ngay, trang, tien_hoan, ngay_hoan))
    sp_doi = None
    if loai_dt == "DOI":
        cung_mau = [x for x in sp if x["ten"] == s["ten"] and x["ma"] != s["ma"] and x["ton"] > 0]
        sp_doi = cung_mau[0] if cung_mau else None
    ct_dt.append((ma, s["ma"], 1, sp_doi["ma"] if sp_doi else None))
    if trang == "HOAN_TAT":
        s["ton"] += 1                      # hang tra / doi ve kho
        if sp_doi:
            sp_doi["ton"] -= 1             # hang doi giao cho khach

# ------------------------------------------------------------ gio hang hien tai
gio_hang = [("GH001", "KH002", datetime(2026, 10, 27, 20, 15)), ("GH002", "KH005", datetime(2026, 10, 28, 9, 40)),
            ("GH003", "KH011", datetime(2026, 10, 26, 22, 5))]
ct_gio = [("GH001", skus("Áo Sơ Mi Vải Linen Cao Cấp", "Be")[1]["ma"], 1),
          ("GH001", skus("Quần Smart Pants", "Đen")[1]["ma"], 1),
          ("GH002", skus("AIRism Cotton Áo Thun Cổ Tròn", "Trắng")[2]["ma"], 2),
          ("GH003", skus("Áo Khoác Vải Cotton Canvas", "Đen")[2]["ma"], 1),
          ("GH003", skus("Quần Jeans Baggy Ống Cong Barrel", "Xanh Dương")[1]["ma"], 1)]

# ------------------------------------------- v3: khach vang lai dat tren web
# Them SAU mo phong (khong dung random) nen khong lam doi phan du lieu phia tren.
#   KH021: da mua vang lai 1 don (hoan tat) + 1 don dang cho xac nhan -> demo tra cuu don, dang ky gan don cu
#   KH022: 2 don da xac nhan nhung khong nhan hang (bom hang) + 1 don COD dang cho -> demo canh bao rui ro o web staff
kh_vl = [dict(ma="KH021", tk=None, user=None, ten="Đặng Thu Trang", sdt="0987000021", email="trang.dt.demo@gmail.com",
              sinh=None, tinh="TP Hồ Chí Minh", dia_chi="88 Phan Xích Long, Phú Nhuận, TP Hồ Chí Minh", tong=0.0),
         dict(ma="KH022", tk=None, user=None, ten="Ngô Bảo Long", sdt="0987000022", email=None,
              sinh=None, tinh="Hà Nội", dia_chi="15 Bà Triệu, Hai Bà Trưng, Hà Nội", tong=0.0)]
kh.extend(kh_vl)
don_vl, ct_don_vl = [], []


def don_vang_lai(k, ngay_dat, trang, pttt, gio, ma_nv=None):
    ma_don = next_id("DH")
    d0 = ngay_dat.date()
    tam = sum(gia_ban(s, d0)[0] * sl for s, sl in gio)
    giam, mkm = ap_km_hoa_don(tam, d0)
    if mkm is None:
        mkm = next((gia_ban(s, d0)[1] for s, _ in gio if gia_ban(s, d0)[1]), None)
    don_vl.append([ma_don, k["ma"], ma_nv, ngay_dat, trang, k["dia_chi"], k["sdt"], pttt, None, tam - giam, k["ten"],
                   giam, "VANG_LAI", k["email"], mkm])
    for s, sl in gio:
        g, _ = gia_ban(s, d0)
        ct_don_vl.append((ma_don, s["ma"], sl, g, g * sl))
    if trang in ("HOAN_TAT", "DA_HUY"):  # da duoc nhan vien xac nhan -> co hoa don
        trang_hd = "DA_THANH_TOAN" if trang == "HOAN_TAT" else "DA_HUY"
        lap_hoa_don(d0 + timedelta(days=1), gio, ma_nv, k["ma"], pttt, ma_don=ma_don, trang_thai=trang_hd)
    return ma_don


def sku_con(ten, mau=None):
    return next(s for s in skus(ten, mau) if s["ton"] >= 2 and s["ma"] not in sp_ngung)


trang, long_ = kh_vl
don_vang_lai(trang, datetime(2026, 9, 12, 20, 30), "HOAN_TAT", "COD",
             [(sku_con("Áo Sơ Mi Vải Linen Cao Cấp"), 1), (sku_con("Chân Váy Satin"), 1)], "NV006")
don_vang_lai(trang, datetime(2026, 10, 28, 9, 15), "CHO_XAC_NHAN", "CHUYEN_KHOAN",
             [(sku_con("AIRism Cotton Áo Thun Cổ Tròn"), 2)])
don_vang_lai(long_, datetime(2026, 8, 2, 21, 5), "DA_HUY", "COD",
             [(sku_con("Áo Khoác Vải Cotton Canvas"), 1)], "NV006")
don_vang_lai(long_, datetime(2026, 9, 20, 22, 40), "DA_HUY", "COD",
             [(sku_con("Quần Jeans Baggy Ống Cong Barrel"), 1), (sku_con("Áo Thun Dáng Boxy Ngắn"), 2)], "NV006")
don_vang_lai(long_, datetime(2026, 10, 28, 7, 50), "CHO_XAC_NHAN", "COD",
             [(sku_con("Áo Khoác Vải Cotton Canvas"), 2)])

# ------------------------------------------------------- hang thanh vien, diem
for k in kh:
    k["diem"] = int(k["tong"] // 10000)          # 10.000d = 1 diem
    t = k["tong"]
    k["hang"] = "KIM_CUONG" if t >= 15_000_000 else "VANG" if t >= 8_000_000 else "BAC" if t >= 3_000_000 else "DONG"

# ------------------------------------------------------------------- xuat SQL
assert all(s["ton"] >= 0 for s in sp), "ton kho am"
out.insert(0, f"""/* =====================================================================
   DU LIEU MAU cho CSDL QL_BanThoiTrang v3  (sinh tu tools/gen_dulieumau.py)
   Chay SAU QL_BanThoiTrang_v3.sql (hoac v2 + BoSung_v3), tren CSDL con trong.
   Khoang thoi gian: {START:%d/%m/%Y} - {END:%d/%m/%Y}
   Mat khau moi tai khoan: 123456 (luu dang BCrypt hash)
     giamdoc, quanly   -> ADMIN (app desktop admin)
     thungan1, thungan2, thukho, banhangonline -> NHAN_VIEN (web staff)
     khach01 .. khach15 -> KHACH_HANG (web ban hang); KH016-KH020 khach tai quay, khong co TK
     KH021, KH022: khach vang lai dat tren web (SDT 0987000021, 0987000022), khong co TK
   Quy uoc diem: 10.000d tren hoa don da thanh toan = 1 diem.
   Hang: < 3tr DONG, 3-8tr BAC, 8-15tr VANG, >= 15tr KIM_CUONG.
   So luong: {len(sp)} SP, {len(kh)} KH, {len(nv)} NV, {len(phieu_nhap)} phieu nhap, {len(don)} don dat hang,
             {len(hoa_don)} hoa don, {len(doi_tra)} phieu doi tra
   ===================================================================== */
USE QL_BanThoiTrang;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF EXISTS (SELECT 1 FROM TAI_KHOAN)
    THROW 50100, N'CSDL da co du lieu. Hay chay tren CSDL moi tao.', 1;
BEGIN TRANSACTION;""")

insert("LOAI_SP", ["ma_loai_sp", "ten_loai_sp"], loai)
insert("SAN_PHAM", ["ma_sp", "ten_sp", "ma_loai_sp", "chat_lieu", "mo_ta", "don_gia", "so_luong", "trang_thai",
                    "hinh_anh", "kich_co", "mau_sac"],
       [(s["ma"], s["ten"], s["loai"], s["cl"], s["mo_ta"],
         s["gia"], s["ton"], "NGUNG_BAN" if s["ma"] in sp_ngung else "DANG_BAN",
         s["anh"], s["size"], s["mau"]) for s in sp])
tk = [(n[1], n[2], HASH, n[3], "HOAT_DONG", datetime(2024, 1, 1, 8, 0)) for n in nv]
tk += [(k["tk"], k["user"], HASH, "KHACH_HANG", "KHOA" if k["ma"] == "KH015" else "HOAT_DONG",
        datetime(2024, 1, 1, 9, 0) + timedelta(days=i * 13)) for i, k in enumerate(kh_tk)]
insert("TAI_KHOAN", ["ma_tk", "ten_dang_nhap", "mat_khau", "vai_tro", "trang_thai", "ngay_tao"], tk)
insert("NHAN_VIEN", ["ma_nv", "ma_tk", "ho_ten", "chuc_vu", "ngay_vao_lam", "luong", "sdt", "email", "trang_thai"],
       [(n[0], n[1], n[4], n[5], n[6], float(n[7]), n[8], n[9], "DANG_LAM") for n in nv])
insert("KHACH_HANG", ["ma_kh", "ma_tk", "ho_ten", "sdt", "email", "ngay_sinh", "hang_thanh_vien", "diem_tich_luy",
                      "dia_chi", "tinh_thanh"],
       [(k["ma"], k["tk"], k["ten"], k["sdt"], k["email"], k["sinh"], k["hang"], k["diem"], k["dia_chi"], k["tinh"])
        for k in kh if k not in kh_vl])
insert("KHACH_HANG", ["ma_kh", "ma_tk", "ho_ten", "sdt", "email", "ngay_sinh", "hang_thanh_vien", "diem_tich_luy",
                      "dia_chi", "tinh_thanh", "sdt_da_xac_thuc"],
       [(k["ma"], k["tk"], k["ten"], k["sdt"], k["email"], k["sinh"], k["hang"], k["diem"], k["dia_chi"], k["tinh"], True)
        for k in kh_vl])
insert("NHA_CUNG_CAP", ["ma_ncc", "ten_ncc", "dia_chi", "sdt", "email", "ma_so_thue", "trang_thai"], ncc)
insert("KHUYEN_MAI", ["ma_km", "ten_km", "hinh_thuc", "phan_tram_giam_gia", "ngay_bd", "ngay_kt"], km)
insert("CT_KM_VOUCHER", ["ma_km", "ma_voucher", "phan_tram_giam_gia"], voucher)
insert("CT_KM_HOA_DON", ["ma_km", "muc_ap_dung_hoa_don", "phan_tram_giam_gia"], km_hd)
insert("CT_KM_SAN_PHAM", ["ma_km", "ma_sp", "phan_tram_giam_gia"], km_sp)
insert("CT_KM_THANH_VIEN_MOI", ["ma_km", "so_don_dau", "phan_tram_giam_gia", "giam_toi_da"], km_tvm)
insert("GIO_HANG", ["ma_gio_hang", "ma_kh", "ngay_cap_nhat"], gio_hang)
insert("CHI_TIET_GIO_HANG", ["ma_gio_hang", "ma_sp", "so_luong"], ct_gio)
insert("DON_DAT_HANG", ["ma_don", "ma_kh", "ma_nv", "ngay_dat", "trang_thai", "dia_chi_giao", "sdt_nhan",
                        "phuong_thuc_tt", "ma_voucher", "tong_tien", "ten_nguoi_nhan", "tien_giam"], don)
insert("CHI_TIET_DON_DAT_HANG", ["ma_don", "ma_sp", "so_luong", "don_gia", "thanh_tien"], ct_don)
insert("DON_DAT_HANG", ["ma_don", "ma_kh", "ma_nv", "ngay_dat", "trang_thai", "dia_chi_giao", "sdt_nhan",
                        "phuong_thuc_tt", "ma_voucher", "tong_tien", "ten_nguoi_nhan", "tien_giam",
                        "nguon", "email_nhan", "ma_km"], don_vl)
insert("CHI_TIET_DON_DAT_HANG", ["ma_don", "ma_sp", "so_luong", "don_gia", "thanh_tien"], ct_don_vl)
insert("HOA_DON", ["ma_hoa_don", "ma_don", "ma_kh", "ma_nv", "ngay_lap", "trang_thai", "ma_km", "tien_giam",
                   "phuong_thuc_tt", "tong_tien"], hoa_don)
insert("CHI_TIET_HOA_DON", ["ma_hoa_don", "ma_sp", "so_luong", "don_gia_ban", "thanh_tien"], ct_hd)
insert("DOI_TRA_HANG", ["ma_doi_tra", "ma_hoa_don", "ma_nv", "loai", "ly_do", "ngay_doi_tra", "trang_thai",
                        "tien_hoan", "ngay_hoan"], doi_tra)
insert("CHI_TIET_DOI_TRA", ["ma_doi_tra", "ma_sp", "so_luong", "ma_sp_doi"], ct_dt)
insert("PHIEU_NHAP", ["ma_phieu_nhap", "ma_ncc", "ma_nv", "ngay_nhap", "tong_tien"], phieu_nhap)
insert("CHI_TIET_PHIEU_NHAP", ["ma_phieu_nhap", "ma_sp", "so_luong", "don_gia_nhap", "thanh_tien"], ct_pn)
out.append("\nCOMMIT TRANSACTION;\nPRINT N'Da nap du lieu mau.';\nGO")
print("\n".join(out))
