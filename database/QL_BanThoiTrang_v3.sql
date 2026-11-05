/* =====================================================================
   CSDL: QL_BanThoiTrang - He thong quan ly ban hang thoi trang (do an CNPM)
   PHIEN BAN v3 (2026-11-04): ban day du = file goc + BoSung_v2 + BoSung_v3.
     Dung file nay khi tao CSDL moi. Cac cho them deu danh dau "v2" / "v3".
     v3: khach vang lai mua hang khong can dang ky (xac thuc SDT bang OTP),
         uu dai thanh vien moi. Phan v3 nam o cuoi file (giong het QL_BanThoiTrang_BoSung_v3.sql).
   Mo hinh: web ban hang (KH) + web staff (NV) + app desktop admin (QL).
   He QTCSDL: SQL Server (T-SQL)
   Huong thiet ke tai khoan: TAI_KHOAN la bang goc, NHAN_VIEN / KHACH_HANG
   tro toi TAI_KHOAN bang khoa ngoai ma_tk (quan he 1-1).
   Quy uoc:
     - Ma (ma_*) la varchar(20); ung dung tu sinh ma.
     - phan_tram_giam_gia luu theo thang 0-100 (15.00 = 15%).
     - SAN_PHAM.so_luong chinh la ton kho hien tai.
     - mat_khau luu chuoi BAM (hash), khong luu mat khau goc.
   13 chuc nang chinh -> bang:
     1 Loai SP: LOAI_SP | 2 Gio hang: GIO_HANG, CHI_TIET_GIO_HANG
     3 Tai khoan: TAI_KHOAN | 4 Don dat hang: DON_DAT_HANG, CHI_TIET_DON_DAT_HANG
     5 San pham: SAN_PHAM | 6 Khach hang: KHACH_HANG | 7 Nhan vien: NHAN_VIEN
     8 Nha cung cap: NHA_CUNG_CAP | 9 Nhap hang: PHIEU_NHAP, CHI_TIET_PHIEU_NHAP
     10 Hoa don: HOA_DON, CHI_TIET_HOA_DON
     11 Doi tra & hoan tien: DOI_TRA_HANG, CHI_TIET_DOI_TRA
     12 Khuyen mai: KHUYEN_MAI, CT_KM_VOUCHER, CT_KM_HOA_DON, CT_KM_SAN_PHAM,
                   CT_KM_THANH_VIEN_MOI (v3)
     14 Xac thuc SDT khach vang lai (v3): MA_OTP
     13 Bao cao & thong ke: truy van/view tu cac bang tren
   ===================================================================== */

IF DB_ID(N'QL_BanThoiTrang') IS NULL
    CREATE DATABASE QL_BanThoiTrang COLLATE Vietnamese_CI_AS;
GO
USE QL_BanThoiTrang;
GO

/* ---------------------------- 1. LOAI_SP ---------------------------- */
CREATE TABLE LOAI_SP (
    ma_loai_sp   varchar(20)   NOT NULL,
    ten_loai_sp  nvarchar(100) NOT NULL,
    CONSTRAINT PK_LOAI_SP PRIMARY KEY (ma_loai_sp),
    CONSTRAINT UQ_LOAI_SP_ten UNIQUE (ten_loai_sp)
);
GO

/* --------------------------- 3. TAI_KHOAN --------------------------- */
CREATE TABLE TAI_KHOAN (
    ma_tk          varchar(20)  NOT NULL,
    ten_dang_nhap  varchar(50)  NOT NULL,
    mat_khau       varchar(255) NOT NULL,                       -- chuoi hash
    vai_tro        varchar(20)  NOT NULL,
    trang_thai     varchar(20)  NOT NULL CONSTRAINT DF_TK_trang_thai DEFAULT ('HOAT_DONG'),
    ngay_tao       datetime2(0) NOT NULL CONSTRAINT DF_TK_ngay_tao   DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_TAI_KHOAN PRIMARY KEY (ma_tk),
    CONSTRAINT UQ_TK_ten_dang_nhap UNIQUE (ten_dang_nhap),
    CONSTRAINT CK_TK_vai_tro   CHECK (vai_tro IN ('ADMIN', 'NHAN_VIEN', 'KHACH_HANG')),
    CONSTRAINT CK_TK_trang_thai CHECK (trang_thai IN ('HOAT_DONG', 'KHOA'))
);
GO

/* --------------------------- 5. SAN_PHAM ---------------------------- */
CREATE TABLE SAN_PHAM (
    ma_sp        varchar(20)    NOT NULL,
    ten_sp       nvarchar(200)  NOT NULL,
    ma_loai_sp   varchar(20)    NOT NULL,
    chat_lieu    nvarchar(100)  NULL,
    mo_ta        nvarchar(1000) NULL,
    don_gia      decimal(18,2)  NOT NULL,
    so_luong     int            NOT NULL CONSTRAINT DF_SP_so_luong   DEFAULT (0),   -- ton kho
    trang_thai   varchar(20)    NOT NULL CONSTRAINT DF_SP_trang_thai DEFAULT ('DANG_BAN'),
    hinh_anh     nvarchar(255)  NULL,                                                -- tuy chon, cho website
    kich_co      varchar(10)    NULL,                                                -- v2: XS, S, M, L, XL, 28...
    mau_sac      nvarchar(50)   NULL,                                                -- v2: moi size-mau la 1 SKU
    CONSTRAINT PK_SAN_PHAM PRIMARY KEY (ma_sp),
    CONSTRAINT FK_SP_LOAI_SP FOREIGN KEY (ma_loai_sp) REFERENCES LOAI_SP (ma_loai_sp),
    CONSTRAINT CK_SP_don_gia   CHECK (don_gia >= 0),
    CONSTRAINT CK_SP_so_luong  CHECK (so_luong >= 0),
    CONSTRAINT CK_SP_trang_thai CHECK (trang_thai IN ('DANG_BAN', 'NGUNG_BAN'))
);
GO

/* --------------------------- 7. NHAN_VIEN --------------------------- */
CREATE TABLE NHAN_VIEN (
    ma_nv         varchar(20)   NOT NULL,
    ma_tk         varchar(20)   NOT NULL,
    ho_ten        nvarchar(100) NOT NULL,
    chuc_vu       nvarchar(50)  NULL,                      -- quan ly, thu ngan, thu kho...
    ngay_vao_lam  date          NULL,
    luong         decimal(18,2) NULL,
    sdt           varchar(15)   NULL,
    email         varchar(100)  NOT NULL,
    trang_thai    varchar(20)   NOT NULL CONSTRAINT DF_NV_trang_thai DEFAULT ('DANG_LAM'),
    CONSTRAINT PK_NHAN_VIEN PRIMARY KEY (ma_nv),
    CONSTRAINT FK_NV_TAI_KHOAN FOREIGN KEY (ma_tk) REFERENCES TAI_KHOAN (ma_tk),
    CONSTRAINT UQ_NV_ma_tk UNIQUE (ma_tk),
    CONSTRAINT UQ_NV_email UNIQUE (email),
    CONSTRAINT CK_NV_luong CHECK (luong IS NULL OR luong >= 0),
    CONSTRAINT CK_NV_trang_thai CHECK (trang_thai IN ('DANG_LAM', 'NGHI_VIEC'))
);
GO

/* -------------------------- 6. KHACH_HANG --------------------------- */
CREATE TABLE KHACH_HANG (
    ma_kh            varchar(20)   NOT NULL,
    ma_tk            varchar(20)   NULL,                   -- NULL: khach mua tai quay, chua co tai khoan
    ho_ten           nvarchar(100) NOT NULL,
    sdt              varchar(15)   NULL,
    email            varchar(100)  NULL,
    ngay_sinh        date          NULL,
    hang_thanh_vien  varchar(20)   NOT NULL CONSTRAINT DF_KH_hang DEFAULT ('DONG'),
    diem_tich_luy    int           NOT NULL CONSTRAINT DF_KH_diem DEFAULT (0),
    dia_chi          nvarchar(255) NULL,                   -- v2
    tinh_thanh       nvarchar(50)  NULL,                   -- v2: loc khach theo TP (yeu cau 6)
    CONSTRAINT PK_KHACH_HANG PRIMARY KEY (ma_kh),
    CONSTRAINT FK_KH_TAI_KHOAN FOREIGN KEY (ma_tk) REFERENCES TAI_KHOAN (ma_tk),
    CONSTRAINT CK_KH_hang CHECK (hang_thanh_vien IN ('DONG', 'BAC', 'VANG', 'KIM_CUONG')),
    CONSTRAINT CK_KH_diem CHECK (diem_tich_luy >= 0)
);
GO
-- UNIQUE tren cot cho phep NULL: dung filtered index (UNIQUE thuong chi cho 1 gia tri NULL)
CREATE UNIQUE INDEX UX_KH_ma_tk ON KHACH_HANG (ma_tk) WHERE ma_tk IS NOT NULL;
CREATE UNIQUE INDEX UX_KH_email ON KHACH_HANG (email) WHERE email IS NOT NULL;
GO

/* ------------------------ 8. NHA_CUNG_CAP --------------------------- */
CREATE TABLE NHA_CUNG_CAP (
    ma_ncc       varchar(20)   NOT NULL,
    ten_ncc      nvarchar(150) NOT NULL,
    dia_chi      nvarchar(255) NULL,
    sdt          varchar(15)   NULL,
    email        varchar(100)  NULL,
    ma_so_thue   varchar(20)   NULL,
    trang_thai   varchar(20)   NOT NULL CONSTRAINT DF_NCC_trang_thai DEFAULT ('HOP_TAC'),
    CONSTRAINT PK_NHA_CUNG_CAP PRIMARY KEY (ma_ncc),
    CONSTRAINT CK_NCC_trang_thai CHECK (trang_thai IN ('HOP_TAC', 'NGUNG_HOP_TAC'))
);
GO
CREATE UNIQUE INDEX UX_NCC_ma_so_thue ON NHA_CUNG_CAP (ma_so_thue) WHERE ma_so_thue IS NOT NULL;
GO

/* -------------------- 12. KHUYEN_MAI va chi tiet -------------------- */
CREATE TABLE KHUYEN_MAI (
    ma_km               varchar(20)   NOT NULL,
    ten_km              nvarchar(150) NOT NULL,
    hinh_thuc           varchar(20)   NOT NULL,            -- SAN_PHAM | HOA_DON | VOUCHER
    phan_tram_giam_gia  decimal(5,2)  NULL,                -- muc giam chung (neu co)
    ngay_bd             date          NOT NULL,
    ngay_kt             date          NOT NULL,
    CONSTRAINT PK_KHUYEN_MAI PRIMARY KEY (ma_km),
    CONSTRAINT CK_KM_hinh_thuc CHECK (hinh_thuc IN ('SAN_PHAM', 'HOA_DON', 'VOUCHER')),
    CONSTRAINT CK_KM_phan_tram CHECK (phan_tram_giam_gia IS NULL OR phan_tram_giam_gia BETWEEN 0 AND 100),
    CONSTRAINT CK_KM_ngay CHECK (ngay_kt >= ngay_bd)
);
GO

CREATE TABLE CT_KM_VOUCHER (
    ma_km               varchar(20)  NOT NULL,
    ma_voucher          varchar(30)  NOT NULL,
    phan_tram_giam_gia  decimal(5,2) NOT NULL,
    CONSTRAINT PK_CT_KM_VOUCHER PRIMARY KEY (ma_km, ma_voucher),
    CONSTRAINT UQ_CT_KM_VOUCHER_ma UNIQUE (ma_voucher),    -- ma voucher nhap tu khach phai duy nhat
    CONSTRAINT FK_KMV_KHUYEN_MAI FOREIGN KEY (ma_km) REFERENCES KHUYEN_MAI (ma_km),
    CONSTRAINT CK_KMV_phan_tram CHECK (phan_tram_giam_gia BETWEEN 0 AND 100)
);
GO

CREATE TABLE CT_KM_HOA_DON (
    ma_km                varchar(20)   NOT NULL,
    muc_ap_dung_hoa_don  decimal(18,2) NOT NULL,           -- hoa don tu muc nay tro len
    phan_tram_giam_gia   decimal(5,2)  NOT NULL,
    CONSTRAINT PK_CT_KM_HOA_DON PRIMARY KEY (ma_km, muc_ap_dung_hoa_don),
    CONSTRAINT FK_KMHD_KHUYEN_MAI FOREIGN KEY (ma_km) REFERENCES KHUYEN_MAI (ma_km),
    CONSTRAINT CK_KMHD_muc CHECK (muc_ap_dung_hoa_don >= 0),
    CONSTRAINT CK_KMHD_phan_tram CHECK (phan_tram_giam_gia BETWEEN 0 AND 100)
);
GO

CREATE TABLE CT_KM_SAN_PHAM (
    ma_km               varchar(20)  NOT NULL,
    ma_sp               varchar(20)  NOT NULL,
    phan_tram_giam_gia  decimal(5,2) NOT NULL,
    CONSTRAINT PK_CT_KM_SAN_PHAM PRIMARY KEY (ma_km, ma_sp),
    CONSTRAINT FK_KMSP_KHUYEN_MAI FOREIGN KEY (ma_km) REFERENCES KHUYEN_MAI (ma_km),
    CONSTRAINT FK_KMSP_SAN_PHAM   FOREIGN KEY (ma_sp) REFERENCES SAN_PHAM (ma_sp),
    CONSTRAINT CK_KMSP_phan_tram CHECK (phan_tram_giam_gia BETWEEN 0 AND 100)
);
GO

/* ------------------------- 2. GIO_HANG ------------------------------ */
CREATE TABLE GIO_HANG (
    ma_gio_hang    varchar(20)  NOT NULL,
    ma_kh          varchar(20)  NOT NULL,
    ngay_cap_nhat  datetime2(0) NOT NULL CONSTRAINT DF_GH_ngay DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_GIO_HANG PRIMARY KEY (ma_gio_hang),
    CONSTRAINT UQ_GH_ma_kh UNIQUE (ma_kh),                 -- moi khach 1 gio hang
    CONSTRAINT FK_GH_KHACH_HANG FOREIGN KEY (ma_kh) REFERENCES KHACH_HANG (ma_kh)
);
GO

CREATE TABLE CHI_TIET_GIO_HANG (
    ma_gio_hang  varchar(20) NOT NULL,
    ma_sp        varchar(20) NOT NULL,
    so_luong     int         NOT NULL,
    CONSTRAINT PK_CT_GIO_HANG PRIMARY KEY (ma_gio_hang, ma_sp),
    CONSTRAINT FK_CTGH_GIO_HANG FOREIGN KEY (ma_gio_hang) REFERENCES GIO_HANG (ma_gio_hang) ON DELETE CASCADE,
    CONSTRAINT FK_CTGH_SAN_PHAM FOREIGN KEY (ma_sp) REFERENCES SAN_PHAM (ma_sp),
    CONSTRAINT CK_CTGH_so_luong CHECK (so_luong > 0)
);
GO

/* ---------------------- 4. DON_DAT_HANG ----------------------------- */
CREATE TABLE DON_DAT_HANG (
    ma_don          varchar(20)   NOT NULL,
    ma_kh           varchar(20)   NOT NULL,
    ma_nv           varchar(20)   NULL,                    -- nhan vien xu ly (NULL khi cho xac nhan)
    ngay_dat        datetime2(0)  NOT NULL CONSTRAINT DF_DON_ngay DEFAULT (SYSDATETIME()),
    trang_thai      varchar(20)   NOT NULL CONSTRAINT DF_DON_trang_thai DEFAULT ('CHO_XAC_NHAN'),
    dia_chi_giao    nvarchar(255) NOT NULL,
    sdt_nhan        varchar(15)   NOT NULL,
    phuong_thuc_tt  varchar(20)   NOT NULL,
    ma_voucher      varchar(30)   NULL,
    tong_tien       decimal(18,2) NOT NULL CONSTRAINT DF_DON_tong DEFAULT (0),
    ten_nguoi_nhan  nvarchar(100) NULL,                    -- v2
    tien_giam       decimal(18,2) NOT NULL CONSTRAINT DF_DON_tien_giam DEFAULT (0),   -- v2
    CONSTRAINT PK_DON_DAT_HANG PRIMARY KEY (ma_don),
    CONSTRAINT FK_DON_KHACH_HANG FOREIGN KEY (ma_kh) REFERENCES KHACH_HANG (ma_kh),
    CONSTRAINT FK_DON_NHAN_VIEN  FOREIGN KEY (ma_nv) REFERENCES NHAN_VIEN (ma_nv),
    CONSTRAINT FK_DON_VOUCHER    FOREIGN KEY (ma_voucher) REFERENCES CT_KM_VOUCHER (ma_voucher),
    CONSTRAINT CK_DON_trang_thai CHECK (trang_thai IN
        ('CHO_XAC_NHAN', 'DA_XAC_NHAN', 'DANG_GIAO', 'HOAN_TAT', 'DA_HUY', 'TU_CHOI')),
    CONSTRAINT CK_DON_phuong_thuc CHECK (phuong_thuc_tt IN ('COD', 'CHUYEN_KHOAN', 'ONLINE')),
    CONSTRAINT CK_DON_tong CHECK (tong_tien >= 0),
    CONSTRAINT CK_DON_tien_giam CHECK (tien_giam >= 0)
);
GO

CREATE TABLE CHI_TIET_DON_DAT_HANG (
    ma_don      varchar(20)   NOT NULL,
    ma_sp       varchar(20)   NOT NULL,
    so_luong    int           NOT NULL,
    don_gia     decimal(18,2) NOT NULL,
    thanh_tien  decimal(18,2) NOT NULL,
    CONSTRAINT PK_CT_DON_DAT_HANG PRIMARY KEY (ma_don, ma_sp),
    CONSTRAINT FK_CTDON_DON FOREIGN KEY (ma_don) REFERENCES DON_DAT_HANG (ma_don),
    CONSTRAINT FK_CTDON_SAN_PHAM FOREIGN KEY (ma_sp) REFERENCES SAN_PHAM (ma_sp),
    CONSTRAINT CK_CTDON_so_luong CHECK (so_luong > 0),
    CONSTRAINT CK_CTDON_don_gia CHECK (don_gia >= 0),
    CONSTRAINT CK_CTDON_thanh_tien CHECK (thanh_tien >= 0)
);
GO

/* ------------------------- 10. HOA_DON ------------------------------ */
CREATE TABLE HOA_DON (
    ma_hoa_don      varchar(20)   NOT NULL,
    ma_don          varchar(20)   NULL,                    -- NULL: ban truc tiep tai quay
    ma_kh           varchar(20)   NULL,                    -- NULL: khach vang lai
    ma_nv           varchar(20)   NOT NULL,                -- hoa don luon do nhan vien lap
    ngay_lap        date          NOT NULL CONSTRAINT DF_HD_ngay DEFAULT (CAST(GETDATE() AS date)),
    trang_thai      varchar(20)   NOT NULL CONSTRAINT DF_HD_trang_thai DEFAULT ('CHO_THANH_TOAN'),
    ma_km           varchar(20)   NULL,                    -- khuyen mai ap dung (de thong ke hieu qua)
    tien_giam       decimal(18,2) NOT NULL CONSTRAINT DF_HD_tien_giam DEFAULT (0),
    phuong_thuc_tt  varchar(20)   NOT NULL,
    tong_tien       decimal(18,2) NOT NULL CONSTRAINT DF_HD_tong DEFAULT (0),   -- sau khi giam
    CONSTRAINT PK_HOA_DON PRIMARY KEY (ma_hoa_don),
    CONSTRAINT FK_HD_DON_DAT_HANG FOREIGN KEY (ma_don) REFERENCES DON_DAT_HANG (ma_don),
    CONSTRAINT FK_HD_KHACH_HANG   FOREIGN KEY (ma_kh)  REFERENCES KHACH_HANG (ma_kh),
    CONSTRAINT FK_HD_NHAN_VIEN    FOREIGN KEY (ma_nv)  REFERENCES NHAN_VIEN (ma_nv),
    CONSTRAINT FK_HD_KHUYEN_MAI   FOREIGN KEY (ma_km)  REFERENCES KHUYEN_MAI (ma_km),
    CONSTRAINT CK_HD_trang_thai CHECK (trang_thai IN ('CHO_THANH_TOAN', 'DA_THANH_TOAN', 'DA_HUY')),
    CONSTRAINT CK_HD_phuong_thuc CHECK (phuong_thuc_tt IN ('TIEN_MAT', 'COD', 'CHUYEN_KHOAN', 'ONLINE')),
    CONSTRAINT CK_HD_tien_giam CHECK (tien_giam >= 0),
    CONSTRAINT CK_HD_tong CHECK (tong_tien >= 0)
);
GO
CREATE UNIQUE INDEX UX_HD_ma_don ON HOA_DON (ma_don) WHERE ma_don IS NOT NULL;  -- 1 don -> toi da 1 hoa don
GO

CREATE TABLE CHI_TIET_HOA_DON (
    ma_hoa_don   varchar(20)   NOT NULL,
    ma_sp        varchar(20)   NOT NULL,
    so_luong     int           NOT NULL,
    don_gia_ban  decimal(18,2) NOT NULL,
    thanh_tien   decimal(18,2) NOT NULL,
    CONSTRAINT PK_CT_HOA_DON PRIMARY KEY (ma_hoa_don, ma_sp),
    CONSTRAINT FK_CTHD_HOA_DON FOREIGN KEY (ma_hoa_don) REFERENCES HOA_DON (ma_hoa_don),
    CONSTRAINT FK_CTHD_SAN_PHAM FOREIGN KEY (ma_sp) REFERENCES SAN_PHAM (ma_sp),
    CONSTRAINT CK_CTHD_so_luong CHECK (so_luong > 0),
    CONSTRAINT CK_CTHD_don_gia CHECK (don_gia_ban >= 0),
    CONSTRAINT CK_CTHD_thanh_tien CHECK (thanh_tien >= 0)
);
GO

/* ------------------- 11. DOI_TRA_HANG (+ hoan tien) ------------------ */
CREATE TABLE DOI_TRA_HANG (
    ma_doi_tra    varchar(20)   NOT NULL,
    ma_hoa_don    varchar(20)   NOT NULL,
    ma_nv         varchar(20)   NULL,                      -- NULL khi khach moi gui yeu cau
    loai          varchar(10)   NOT NULL,                  -- DOI | TRA
    ly_do         nvarchar(500) NULL,
    ngay_doi_tra  date          NOT NULL CONSTRAINT DF_DT_ngay DEFAULT (CAST(GETDATE() AS date)),
    trang_thai    varchar(20)   NOT NULL CONSTRAINT DF_DT_trang_thai DEFAULT ('CHO_DUYET'),
    tien_hoan     decimal(18,2) NOT NULL CONSTRAINT DF_DT_tien_hoan DEFAULT (0),
    ngay_hoan     date          NULL,
    CONSTRAINT PK_DOI_TRA_HANG PRIMARY KEY (ma_doi_tra),
    CONSTRAINT FK_DT_HOA_DON  FOREIGN KEY (ma_hoa_don) REFERENCES HOA_DON (ma_hoa_don),
    CONSTRAINT FK_DT_NHAN_VIEN FOREIGN KEY (ma_nv) REFERENCES NHAN_VIEN (ma_nv),
    CONSTRAINT CK_DT_loai CHECK (loai IN ('DOI', 'TRA')),
    CONSTRAINT CK_DT_trang_thai CHECK (trang_thai IN ('CHO_DUYET', 'DA_DUYET', 'TU_CHOI', 'HOAN_TAT')),
    CONSTRAINT CK_DT_tien_hoan CHECK (tien_hoan >= 0)
);
GO

CREATE TABLE CHI_TIET_DOI_TRA (
    ma_doi_tra  varchar(20) NOT NULL,
    ma_sp       varchar(20) NOT NULL,                      -- san pham tra / doi (phai nam trong hoa don goc: kiem tra o ung dung)
    so_luong    int         NOT NULL,
    ma_sp_doi   varchar(20) NULL,                          -- san pham nhan lai khi loai = DOI
    CONSTRAINT PK_CT_DOI_TRA PRIMARY KEY (ma_doi_tra, ma_sp),
    CONSTRAINT FK_CTDT_DOI_TRA FOREIGN KEY (ma_doi_tra) REFERENCES DOI_TRA_HANG (ma_doi_tra),
    CONSTRAINT FK_CTDT_SAN_PHAM FOREIGN KEY (ma_sp) REFERENCES SAN_PHAM (ma_sp),
    CONSTRAINT FK_CTDT_SP_DOI FOREIGN KEY (ma_sp_doi) REFERENCES SAN_PHAM (ma_sp),
    CONSTRAINT CK_CTDT_so_luong CHECK (so_luong > 0)
);
GO

/* ----------------------- 9. PHIEU_NHAP ------------------------------ */
CREATE TABLE PHIEU_NHAP (
    ma_phieu_nhap  varchar(20)   NOT NULL,
    ma_ncc         varchar(20)   NOT NULL,
    ma_nv          varchar(20)   NOT NULL,
    ngay_nhap      date          NOT NULL CONSTRAINT DF_PN_ngay DEFAULT (CAST(GETDATE() AS date)),
    tong_tien      decimal(18,2) NOT NULL CONSTRAINT DF_PN_tong DEFAULT (0),
    CONSTRAINT PK_PHIEU_NHAP PRIMARY KEY (ma_phieu_nhap),
    CONSTRAINT FK_PN_NHA_CUNG_CAP FOREIGN KEY (ma_ncc) REFERENCES NHA_CUNG_CAP (ma_ncc),
    CONSTRAINT FK_PN_NHAN_VIEN FOREIGN KEY (ma_nv) REFERENCES NHAN_VIEN (ma_nv),
    CONSTRAINT CK_PN_tong CHECK (tong_tien >= 0)
);
GO

CREATE TABLE CHI_TIET_PHIEU_NHAP (
    ma_phieu_nhap  varchar(20)   NOT NULL,
    ma_sp          varchar(20)   NOT NULL,
    so_luong       int           NOT NULL,
    don_gia_nhap   decimal(18,2) NOT NULL,
    thanh_tien     decimal(18,2) NOT NULL,
    CONSTRAINT PK_CT_PHIEU_NHAP PRIMARY KEY (ma_phieu_nhap, ma_sp),
    CONSTRAINT FK_CTPN_PHIEU_NHAP FOREIGN KEY (ma_phieu_nhap) REFERENCES PHIEU_NHAP (ma_phieu_nhap),
    CONSTRAINT FK_CTPN_SAN_PHAM FOREIGN KEY (ma_sp) REFERENCES SAN_PHAM (ma_sp),
    CONSTRAINT CK_CTPN_so_luong CHECK (so_luong > 0),
    CONSTRAINT CK_CTPN_don_gia CHECK (don_gia_nhap >= 0),
    CONSTRAINT CK_CTPN_thanh_tien CHECK (thanh_tien >= 0)
);
GO

/* ----------------------------- INDEX -------------------------------- */
CREATE INDEX IX_SP_ma_loai_sp   ON SAN_PHAM (ma_loai_sp);
CREATE INDEX IX_SP_ten_sp       ON SAN_PHAM (ten_sp);
CREATE INDEX IX_DON_ma_kh       ON DON_DAT_HANG (ma_kh, trang_thai);
CREATE INDEX IX_HD_ma_kh        ON HOA_DON (ma_kh);
CREATE INDEX IX_HD_ngay_lap     ON HOA_DON (ngay_lap);
CREATE INDEX IX_HD_ma_km        ON HOA_DON (ma_km);
CREATE INDEX IX_DT_ma_hoa_don   ON DOI_TRA_HANG (ma_hoa_don);
CREATE INDEX IX_PN_ma_ncc       ON PHIEU_NHAP (ma_ncc);
CREATE INDEX IX_PN_ngay_nhap    ON PHIEU_NHAP (ngay_nhap);
CREATE INDEX IX_CTHD_ma_sp      ON CHI_TIET_HOA_DON (ma_sp);
CREATE INDEX IX_HD_ma_nv        ON HOA_DON (ma_nv, ngay_lap);     -- v2
CREATE INDEX IX_KH_tinh_thanh   ON KHACH_HANG (tinh_thanh);       -- v2
GO

/* ------------- TRIGGER: vai tro tai khoan phai khop NV / KH ---------- */
CREATE TRIGGER TR_NHAN_VIEN_vai_tro ON NHAN_VIEN
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN TAI_KHOAN t ON t.ma_tk = i.ma_tk
               WHERE t.vai_tro NOT IN ('ADMIN', 'NHAN_VIEN'))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, N'Tai khoan cua nhan vien phai co vai tro ADMIN hoac NHAN_VIEN.', 1;
    END
END;
GO

CREATE TRIGGER TR_KHACH_HANG_vai_tro ON KHACH_HANG
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN TAI_KHOAN t ON t.ma_tk = i.ma_tk
               WHERE t.vai_tro <> 'KHACH_HANG')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, N'Tai khoan cua khach hang phai co vai tro KHACH_HANG.', 1;
    END
END;
GO

/* ---------- TRIGGER (v2): doi vai tro tai khoan phai khop NV / KH ----- */
CREATE OR ALTER TRIGGER TR_TAI_KHOAN_vai_tro ON TAI_KHOAN
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(vai_tro) RETURN;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN NHAN_VIEN n ON n.ma_tk = i.ma_tk
               WHERE i.vai_tro NOT IN ('ADMIN', 'NHAN_VIEN'))
       OR EXISTS (SELECT 1 FROM inserted i
               JOIN KHACH_HANG k ON k.ma_tk = i.ma_tk
               WHERE i.vai_tro <> 'KHACH_HANG')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50003, N'Khong the doi vai tro: tai khoan dang gan voi nhan vien / khach hang khac vai tro.', 1;
    END
END;
GO

/* ---------- VIEW: so luong SP theo loai (thay cot LOAI_SP.so_luong) --- */
CREATE VIEW V_LOAI_SP_THONG_KE AS
SELECT l.ma_loai_sp,
       l.ten_loai_sp,
       COUNT(s.ma_sp)                AS so_san_pham,
       COALESCE(SUM(s.so_luong), 0)  AS tong_ton_kho
FROM LOAI_SP l
LEFT JOIN SAN_PHAM s ON s.ma_loai_sp = l.ma_loai_sp
GROUP BY l.ma_loai_sp, l.ten_loai_sp;
GO

/* =====================================================================
   BO SUNG v3 cho CSDL QL_BanThoiTrang (chay SAU QL_BanThoiTrang_v2.sql)
   Muc dich: KHACH VANG LAI mua hang tren web khong can dang ky (yeu cau giao vien 04/11/2026)
             + UU DAI THANH VIEN MOI khi khach tao tai khoan.
   Chi THEM bang / cot / rang buoc, khong xoa du lieu. Chay lai nhieu lan van an toan.

   1. MA_OTP (bang moi)       : ma OTP xac thuc so dien thoai (luu ban bam, han dung, so lan nhap sai)
   2. KHACH_HANG              : sdt_da_xac_thuc + UNIQUE tren sdt (1 SDT = 1 khach, de gom don va gan tai khoan)
   3. DON_DAT_HANG            : nguon (TAI_KHOAN | VANG_LAI), email_nhan, ma_km (KM da ap dung luc dat)
   4. KHUYEN_MAI.hinh_thuc    : them THANH_VIEN_MOI
      CT_KM_THANH_VIEN_MOI (bang moi): giam X% cho N don dau dat bang tai khoan, toi da Y dong / don
   5. DON_DAT_HANG: yeu cau huy (yeu_cau_huy, ly_do_huy, ngay_yeu_cau_huy, phan_hoi_huy, ngay_huy, nguoi_huy)
      HOAN_TIEN (bang moi): don da tra truoc bi huy -> cho hoan tien
   Sau file nay CSDL co 23 bang.
   ===================================================================== */

USE QL_BanThoiTrang;
GO

/* --------------------------- 1. MA_OTP ------------------------------ */
IF OBJECT_ID('MA_OTP') IS NULL
CREATE TABLE MA_OTP (
    ma_otp       bigint       IDENTITY(1,1) NOT NULL,
    sdt          varchar(15)  NOT NULL,
    muc_dich     varchar(20)  NOT NULL,                     -- MUA_HANG | TRA_CUU | DANG_KY (ghi nhan, dung chung 1 phien)
    ma_bam       varchar(64)  NOT NULL,                     -- SHA-256 (hex) cua "sdt:ma", khong luu ma goc
    ngay_tao     datetime2(0) NOT NULL CONSTRAINT DF_OTP_ngay_tao DEFAULT (SYSDATETIME()),
    han_dung     datetime2(0) NOT NULL,
    so_lan_sai   int          NOT NULL CONSTRAINT DF_OTP_so_lan_sai DEFAULT (0),
    da_dung      bit          NOT NULL CONSTRAINT DF_OTP_da_dung DEFAULT (0),
    dia_chi_ip   varchar(45)  NULL,                         -- gioi han so lan gui theo IP
    CONSTRAINT PK_MA_OTP PRIMARY KEY (ma_otp),
    CONSTRAINT CK_OTP_muc_dich CHECK (muc_dich IN ('MUA_HANG', 'TRA_CUU', 'DANG_KY')),
    CONSTRAINT CK_OTP_so_lan_sai CHECK (so_lan_sai >= 0),
    CONSTRAINT CK_OTP_han CHECK (han_dung > ngay_tao)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OTP_sdt' AND object_id = OBJECT_ID('MA_OTP'))
    CREATE INDEX IX_OTP_sdt ON MA_OTP (sdt, ngay_tao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OTP_ip' AND object_id = OBJECT_ID('MA_OTP'))
    CREATE INDEX IX_OTP_ip ON MA_OTP (dia_chi_ip, ngay_tao);
GO

/* ------------------------- 2. KHACH_HANG ---------------------------- */
IF COL_LENGTH('KHACH_HANG', 'sdt_da_xac_thuc') IS NULL
    ALTER TABLE KHACH_HANG ADD sdt_da_xac_thuc bit NOT NULL CONSTRAINT DF_KH_sdt_xac_thuc DEFAULT (0);
GO
-- 1 so dien thoai chi thuoc 1 khach (BLL da kiem tra tu v2, nay khoa them o CSDL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_KH_sdt' AND object_id = OBJECT_ID('KHACH_HANG'))
    CREATE UNIQUE INDEX UX_KH_sdt ON KHACH_HANG (sdt) WHERE sdt IS NOT NULL;
GO

/* ------------------------ 3. DON_DAT_HANG --------------------------- */
IF COL_LENGTH('DON_DAT_HANG', 'nguon') IS NULL
    ALTER TABLE DON_DAT_HANG ADD nguon varchar(20) NOT NULL
        CONSTRAINT DF_DON_nguon DEFAULT ('TAI_KHOAN')
        CONSTRAINT CK_DON_nguon CHECK (nguon IN ('TAI_KHOAN', 'VANG_LAI'));
IF COL_LENGTH('DON_DAT_HANG', 'email_nhan') IS NULL
    ALTER TABLE DON_DAT_HANG ADD email_nhan varchar(100) NULL;
IF COL_LENGTH('DON_DAT_HANG', 'ma_km') IS NULL
    ALTER TABLE DON_DAT_HANG ADD ma_km varchar(20) NULL
        CONSTRAINT FK_DON_KHUYEN_MAI FOREIGN KEY REFERENCES KHUYEN_MAI (ma_km);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DON_nguon' AND object_id = OBJECT_ID('DON_DAT_HANG'))
    CREATE INDEX IX_DON_nguon ON DON_DAT_HANG (nguon, trang_thai);
GO

/* ------------- 4. KHUYEN_MAI: hinh thuc THANH_VIEN_MOI --------------- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_KM_hinh_thuc'
           AND definition NOT LIKE '%THANH_VIEN_MOI%')
    ALTER TABLE KHUYEN_MAI DROP CONSTRAINT CK_KM_hinh_thuc;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_KM_hinh_thuc')
    ALTER TABLE KHUYEN_MAI ADD CONSTRAINT CK_KM_hinh_thuc
        CHECK (hinh_thuc IN ('SAN_PHAM', 'HOA_DON', 'VOUCHER', 'THANH_VIEN_MOI'));
GO

IF OBJECT_ID('CT_KM_THANH_VIEN_MOI') IS NULL
CREATE TABLE CT_KM_THANH_VIEN_MOI (
    ma_km               varchar(20)   NOT NULL,
    so_don_dau          int           NOT NULL,              -- ap dung cho N don dau dat bang tai khoan
    phan_tram_giam_gia  decimal(5,2)  NOT NULL,
    giam_toi_da         decimal(18,2) NULL,                  -- NULL: khong gioi han
    CONSTRAINT PK_CT_KM_THANH_VIEN_MOI PRIMARY KEY (ma_km),
    CONSTRAINT FK_KMTV_KHUYEN_MAI FOREIGN KEY (ma_km) REFERENCES KHUYEN_MAI (ma_km),
    CONSTRAINT CK_KMTV_so_don CHECK (so_don_dau BETWEEN 1 AND 10),
    CONSTRAINT CK_KMTV_phan_tram CHECK (phan_tram_giam_gia > 0 AND phan_tram_giam_gia <= 100),
    CONSTRAINT CK_KMTV_toi_da CHECK (giam_toi_da IS NULL OR giam_toi_da > 0)
);
GO

/* ---- 5. CSDL DA CO DU LIEU (nang cap tu v2): them san chuong trinh uu dai thanh vien moi ----
   CSDL moi tao (chua co tai khoan) thi bo qua, vi file du lieu mau da co KM06. */
IF EXISTS (SELECT 1 FROM TAI_KHOAN)
   AND NOT EXISTS (SELECT 1 FROM KHUYEN_MAI WHERE hinh_thuc = 'THANH_VIEN_MOI')
BEGIN
    DECLARE @ma varchar(20) = 'KM' + RIGHT('0' + CAST(1 + ISNULL(
        (SELECT MAX(CAST(SUBSTRING(ma_km, 3, 10) AS int)) FROM KHUYEN_MAI WHERE ma_km LIKE 'KM[0-9][0-9]'), 0) AS varchar(10)), 2);
    INSERT INTO KHUYEN_MAI (ma_km, ten_km, hinh_thuc, phan_tram_giam_gia, ngay_bd, ngay_kt)
    VALUES (@ma, N'Ưu đãi thành viên mới', 'THANH_VIEN_MOI', 10, '2026-01-01', '2027-12-31');
    INSERT INTO CT_KM_THANH_VIEN_MOI (ma_km, so_don_dau, phan_tram_giam_gia, giam_toi_da) VALUES (@ma, 3, 10, 100000);
    PRINT N'Da them chuong trinh uu dai thanh vien moi ' + @ma;
END
GO

/* ---- 6. YEU CAU HUY DON + HOAN TIEN (bo sung 04/11/2026) ------------------
   Khach muon huy don DA_XAC_NHAN: gui yeu cau, nhan vien duyet / tu choi.
   Huy theo yeu cau cua khach truoc khi giao KHONG tinh la bom hang.
   Don da thanh toan truoc (chuyen khoan / online) bi huy -> 1 dong HOAN_TIEN cho ke toan hoan.  */
IF COL_LENGTH('DON_DAT_HANG', 'yeu_cau_huy') IS NULL
    ALTER TABLE DON_DAT_HANG ADD yeu_cau_huy varchar(20) NULL        -- NULL | CHO_DUYET | DA_DUYET | TU_CHOI
        CONSTRAINT CK_DON_yeu_cau_huy CHECK (yeu_cau_huy IN ('CHO_DUYET', 'DA_DUYET', 'TU_CHOI'));
IF COL_LENGTH('DON_DAT_HANG', 'ly_do_huy') IS NULL
    ALTER TABLE DON_DAT_HANG ADD ly_do_huy nvarchar(255) NULL;
IF COL_LENGTH('DON_DAT_HANG', 'ngay_yeu_cau_huy') IS NULL
    ALTER TABLE DON_DAT_HANG ADD ngay_yeu_cau_huy datetime2(0) NULL;
IF COL_LENGTH('DON_DAT_HANG', 'phan_hoi_huy') IS NULL
    ALTER TABLE DON_DAT_HANG ADD phan_hoi_huy nvarchar(255) NULL;     -- ly do cua hang tu choi yeu cau huy
IF COL_LENGTH('DON_DAT_HANG', 'ngay_huy') IS NULL
    ALTER TABLE DON_DAT_HANG ADD ngay_huy datetime2(0) NULL;
IF COL_LENGTH('DON_DAT_HANG', 'nguoi_huy') IS NULL
    ALTER TABLE DON_DAT_HANG ADD nguoi_huy varchar(20) NULL           -- KHACH: khach tu huy / xin huy duoc duyet
        CONSTRAINT CK_DON_nguoi_huy CHECK (nguoi_huy IN ('KHACH', 'NHAN_VIEN'));
GO

IF OBJECT_ID('HOAN_TIEN') IS NULL
CREATE TABLE HOAN_TIEN (
    ma_hoan_tien  varchar(20)   NOT NULL,
    ma_don        varchar(20)   NOT NULL,
    ma_hoa_don    varchar(20)   NOT NULL,
    so_tien       decimal(18,2) NOT NULL,
    phuong_thuc   varchar(20)   NOT NULL,                     -- hoan qua kenh khach da tra (CHUYEN_KHOAN | ONLINE)
    trang_thai    varchar(20)   NOT NULL CONSTRAINT DF_HT_trang_thai DEFAULT ('CHO_HOAN'),
    ngay_tao      datetime2(0)  NOT NULL CONSTRAINT DF_HT_ngay_tao DEFAULT (SYSDATETIME()),
    ngay_hoan     datetime2(0)  NULL,
    ma_nv         varchar(20)   NULL,                         -- nhan vien xac nhan da hoan
    ghi_chu       nvarchar(255) NULL,
    CONSTRAINT PK_HOAN_TIEN PRIMARY KEY (ma_hoan_tien),
    CONSTRAINT UQ_HT_don UNIQUE (ma_don),                     -- 1 don chi hoan 1 lan
    CONSTRAINT FK_HT_DON FOREIGN KEY (ma_don) REFERENCES DON_DAT_HANG (ma_don),
    CONSTRAINT FK_HT_HOA_DON FOREIGN KEY (ma_hoa_don) REFERENCES HOA_DON (ma_hoa_don),
    CONSTRAINT FK_HT_NHAN_VIEN FOREIGN KEY (ma_nv) REFERENCES NHAN_VIEN (ma_nv),
    CONSTRAINT CK_HT_so_tien CHECK (so_tien > 0),
    CONSTRAINT CK_HT_trang_thai CHECK (trang_thai IN ('CHO_HOAN', 'DA_HOAN')),
    CONSTRAINT CK_HT_da_hoan CHECK ((trang_thai = 'CHO_HOAN' AND ngay_hoan IS NULL)
                                 OR (trang_thai = 'DA_HOAN' AND ngay_hoan IS NOT NULL))
);
GO
