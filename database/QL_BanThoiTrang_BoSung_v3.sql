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
