/* =====================================================================
   BO SUNG v2 cho CSDL QL_BanThoiTrang (chay SAU file QL_BanThoiTrang_goc.sql)
   Mo hinh: web ban hang (KH) + web staff (NV) + app desktop admin (QL).
   Chi THEM cot / trigger / index, khong xoa hay doi kieu cot nao.
   Chay lai nhieu lan van an toan (co kiem tra ton tai).

   1. KHACH_HANG: dia_chi, tinh_thanh  -> yeu cau 6 (khach TP HCM / Ha Noi)
   2. SAN_PHAM  : kich_co, mau_sac     -> thoi trang co size / mau,
                  moi size-mau la 1 dong SAN_PHAM (1 SKU)
   3. DON_DAT_HANG: ten_nguoi_nhan, tien_giam -> giao hang, in PDF don
   4. Trigger TAI_KHOAN: chan doi vai_tro lam lech NV / KH da gan
   5. Index phuc vu thong ke theo nhan vien, theo tinh thanh
   ===================================================================== */

USE QL_BanThoiTrang;
GO

/* ------------------------- 1. KHACH_HANG ---------------------------- */
IF COL_LENGTH('KHACH_HANG', 'dia_chi') IS NULL
    ALTER TABLE KHACH_HANG ADD dia_chi nvarchar(255) NULL;
IF COL_LENGTH('KHACH_HANG', 'tinh_thanh') IS NULL
    ALTER TABLE KHACH_HANG ADD tinh_thanh nvarchar(50) NULL;   -- vd: N'TP Hồ Chí Minh', N'Hà Nội'
GO

/* -------------------------- 2. SAN_PHAM ----------------------------- */
IF COL_LENGTH('SAN_PHAM', 'kich_co') IS NULL
    ALTER TABLE SAN_PHAM ADD kich_co varchar(10) NULL;          -- XS, S, M, L, XL, XXL, 28, 29...
IF COL_LENGTH('SAN_PHAM', 'mau_sac') IS NULL
    ALTER TABLE SAN_PHAM ADD mau_sac nvarchar(50) NULL;
GO

/* ------------------------ 3. DON_DAT_HANG --------------------------- */
IF COL_LENGTH('DON_DAT_HANG', 'ten_nguoi_nhan') IS NULL
    ALTER TABLE DON_DAT_HANG ADD ten_nguoi_nhan nvarchar(100) NULL;
IF COL_LENGTH('DON_DAT_HANG', 'tien_giam') IS NULL
    ALTER TABLE DON_DAT_HANG ADD tien_giam decimal(18,2) NOT NULL
        CONSTRAINT DF_DON_tien_giam DEFAULT (0)
        CONSTRAINT CK_DON_tien_giam CHECK (tien_giam >= 0);
GO

/* ---------- 4. TRIGGER: doi vai tro tai khoan phai khop NV / KH ----- */
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

/* ----------------------------- 5. INDEX ----------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HD_ma_nv' AND object_id = OBJECT_ID('HOA_DON'))
    CREATE INDEX IX_HD_ma_nv ON HOA_DON (ma_nv, ngay_lap);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KH_tinh_thanh' AND object_id = OBJECT_ID('KHACH_HANG'))
    CREATE INDEX IX_KH_tinh_thanh ON KHACH_HANG (tinh_thanh);
GO
