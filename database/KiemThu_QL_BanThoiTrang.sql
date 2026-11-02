/* =====================================================================
   KIEM THU CSDL QL_BanThoiTrang v2
   Chay SAU khi da nap QL_BanThoiTrang_DuLieuMau.sql.
   Moi test ghi PASS / FAIL vao #ket_qua, cuoi file in tong ket.
   Phan B: moi lenh sai deu chay trong transaction roi ROLLBACK,
           nen chay xong du lieu khong doi.
     A. Tinh nhat quan du lieu (ton kho, tong tien, diem...)
     B. Rang buoc: CHECK, UNIQUE, khoa ngoai, trigger phai chan du lieu sai
     C. Cau truy van thong ke yeu cau 5 va 6 + doi chieu so lieu
   ===================================================================== */
USE QL_BanThoiTrang;
SET NOCOUNT ON;
IF OBJECT_ID('tempdb..#ket_qua') IS NOT NULL DROP TABLE #ket_qua;
CREATE TABLE #ket_qua (stt int IDENTITY, nhom char(1), ten nvarchar(200), ket_qua varchar(4), ghi_chu nvarchar(400));

/* ============================ A. NHAT QUAN ============================ */
-- A1. Ton kho = nhap - ban (hoa don khong huy) + hang doi/tra ve kho - hang doi giao di
WITH tinh AS (
    SELECT s.ma_sp, s.so_luong,
           ISNULL((SELECT SUM(so_luong) FROM CHI_TIET_PHIEU_NHAP c WHERE c.ma_sp = s.ma_sp), 0)
         - ISNULL((SELECT SUM(c.so_luong) FROM CHI_TIET_HOA_DON c JOIN HOA_DON h ON h.ma_hoa_don = c.ma_hoa_don
                   WHERE c.ma_sp = s.ma_sp AND h.trang_thai <> 'DA_HUY'), 0)
         + ISNULL((SELECT SUM(c.so_luong) FROM CHI_TIET_DOI_TRA c JOIN DOI_TRA_HANG d ON d.ma_doi_tra = c.ma_doi_tra
                   WHERE c.ma_sp = s.ma_sp AND d.trang_thai = 'HOAN_TAT'), 0)
         - ISNULL((SELECT SUM(c.so_luong) FROM CHI_TIET_DOI_TRA c JOIN DOI_TRA_HANG d ON d.ma_doi_tra = c.ma_doi_tra
                   WHERE c.ma_sp_doi = s.ma_sp AND d.trang_thai = 'HOAN_TAT'), 0) AS ton_tinh
    FROM SAN_PHAM s)
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'Tồn kho SAN_PHAM.so_luong khớp nhập - bán + đổi trả',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' sản phẩm lệch')
FROM tinh WHERE so_luong <> ton_tinh;

-- A2. Thanh tien tung dong = so luong x don gia
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'thanh_tien = so_luong x đơn giá (hóa đơn, đơn, phiếu nhập)',
       CASE WHEN SUM(loi) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(SUM(loi), N' dòng sai')
FROM (SELECT COUNT(*) loi FROM CHI_TIET_HOA_DON WHERE thanh_tien <> so_luong * don_gia_ban
      UNION ALL SELECT COUNT(*) FROM CHI_TIET_DON_DAT_HANG WHERE thanh_tien <> so_luong * don_gia
      UNION ALL SELECT COUNT(*) FROM CHI_TIET_PHIEU_NHAP WHERE thanh_tien <> so_luong * don_gia_nhap) x;

-- A3. Tong tien chung tu = tong chi tiet - tien giam
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'HOA_DON.tong_tien = tổng chi tiết - tien_giam',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' hóa đơn lệch')
FROM HOA_DON h
WHERE h.tong_tien <> (SELECT SUM(thanh_tien) FROM CHI_TIET_HOA_DON c WHERE c.ma_hoa_don = h.ma_hoa_don) - h.tien_giam;

INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'DON_DAT_HANG.tong_tien = tổng chi tiết - tien_giam',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' đơn lệch')
FROM DON_DAT_HANG d
WHERE d.tong_tien <> (SELECT SUM(thanh_tien) FROM CHI_TIET_DON_DAT_HANG c WHERE c.ma_don = d.ma_don) - d.tien_giam;

INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'PHIEU_NHAP.tong_tien = tổng chi tiết',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' phiếu lệch')
FROM PHIEU_NHAP p
WHERE p.tong_tien <> (SELECT SUM(thanh_tien) FROM CHI_TIET_PHIEU_NHAP c WHERE c.ma_phieu_nhap = p.ma_phieu_nhap);

-- A4. Hoa don lap tu don: cung khach, chi khi don da xac nhan
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'Hóa đơn từ đơn online: cùng khách, đơn đã xác nhận',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' hóa đơn sai')
FROM HOA_DON h JOIN DON_DAT_HANG d ON d.ma_don = h.ma_don
WHERE h.ma_kh <> d.ma_kh
   -- don bi nhan vien huy sau khi xac nhan thi hoa don cung DA_HUY: hop le
   OR (d.trang_thai NOT IN ('DA_XAC_NHAN', 'DANG_GIAO', 'HOAN_TAT') AND NOT (d.trang_thai = 'DA_HUY' AND h.trang_thai = 'DA_HUY'));

-- A5. San pham doi/tra phai nam trong hoa don goc, khong vuot so luong da mua
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'Sản phẩm đổi trả nằm trong hóa đơn gốc, không vượt số lượng mua',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' dòng sai')
FROM CHI_TIET_DOI_TRA c JOIN DOI_TRA_HANG d ON d.ma_doi_tra = c.ma_doi_tra
LEFT JOIN CHI_TIET_HOA_DON h ON h.ma_hoa_don = d.ma_hoa_don AND h.ma_sp = c.ma_sp
WHERE h.ma_sp IS NULL OR c.so_luong > h.so_luong;

-- A6. Diem tich luy = tong hoa don da thanh toan / 10.000
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'Điểm tích lũy = tổng hóa đơn đã thanh toán / 10.000',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' khách lệch')
FROM KHACH_HANG k
WHERE k.diem_tich_luy <> ISNULL((SELECT CAST(FLOOR(SUM(tong_tien) / 10000) AS int) FROM HOA_DON h
                                 WHERE h.ma_kh = k.ma_kh AND h.trang_thai = 'DA_THANH_TOAN'), 0);

-- A7. Vai tro tai khoan khop voi NV / KH
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'Mỗi tài khoản gắn đúng 1 NV hoặc 1 KH, đúng vai trò',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' tài khoản sai')
FROM TAI_KHOAN t
LEFT JOIN NHAN_VIEN n ON n.ma_tk = t.ma_tk
LEFT JOIN KHACH_HANG k ON k.ma_tk = t.ma_tk
WHERE (n.ma_nv IS NULL AND k.ma_kh IS NULL) OR (n.ma_nv IS NOT NULL AND k.ma_kh IS NOT NULL)
   OR (n.ma_nv IS NOT NULL AND t.vai_tro NOT IN ('ADMIN', 'NHAN_VIEN'))
   OR (k.ma_kh IS NOT NULL AND t.vai_tro <> 'KHACH_HANG');

-- A8. Mat khau luu dang hash BCrypt, khong luu mat khau goc
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
SELECT 'A', N'mat_khau là chuỗi hash BCrypt',
       CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END, CONCAT(COUNT(*), N' tài khoản không phải hash')
FROM TAI_KHOAN WHERE mat_khau NOT LIKE '$2_$__$%' OR LEN(mat_khau) <> 60;

/* ============================ B. RANG BUOC ============================ */
-- Moi test: chay lenh sai trong transaction. Neu bi chan -> PASS, neu lot qua -> FAIL.
DECLARE @tests TABLE (stt int IDENTITY, ten nvarchar(200), lenh nvarchar(max));
INSERT @tests (ten, lenh) VALUES
 (N'CHECK: đơn giá sản phẩm âm',            N'UPDATE SAN_PHAM SET don_gia = -1 WHERE ma_sp = ''SP001'''),
 (N'CHECK: tồn kho âm',                     N'UPDATE SAN_PHAM SET so_luong = -1 WHERE ma_sp = ''SP001'''),
 (N'CHECK: trạng thái sản phẩm sai',        N'UPDATE SAN_PHAM SET trang_thai = ''HET_HANG'' WHERE ma_sp = ''SP001'''),
 (N'CHECK: vai trò tài khoản sai',          N'INSERT TAI_KHOAN (ma_tk, ten_dang_nhap, mat_khau, vai_tro) VALUES (''TKX'', ''x'', ''x'', ''KHACH'')'),
 (N'UNIQUE: trùng tên đăng nhập',           N'INSERT TAI_KHOAN (ma_tk, ten_dang_nhap, mat_khau, vai_tro) VALUES (''TKX'', ''giamdoc'', ''x'', ''ADMIN'')'),
 (N'UNIQUE: trùng tên loại sản phẩm',       N'INSERT LOAI_SP VALUES (''LSPX'', N''Áo thun'')'),
 (N'UNIQUE: 2 khách dùng chung 1 tài khoản', N'UPDATE KHACH_HANG SET ma_tk = ''TK101'' WHERE ma_kh = ''KH002'''),
 (N'UNIQUE: trùng email khách hàng',        N'UPDATE KHACH_HANG SET email = ''khach01@gmail.com'' WHERE ma_kh = ''KH002'''),
 (N'UNIQUE: 1 khách có 2 giỏ hàng',         N'INSERT GIO_HANG (ma_gio_hang, ma_kh) VALUES (''GHX'', ''KH002'')'),
 (N'UNIQUE: 1 đơn lập 2 hóa đơn',           N'INSERT HOA_DON (ma_hoa_don, ma_don, ma_nv, phuong_thuc_tt) SELECT TOP 1 ''HDX'', ma_don, ''NV003'', ''COD'' FROM HOA_DON WHERE ma_don IS NOT NULL'),
 (N'UNIQUE: trùng mã voucher',              N'INSERT CT_KM_VOUCHER VALUES (''KM01'', ''WELCOME10'', 5)'),
 (N'TRIGGER: NV gắn tài khoản vai trò KHACH_HANG', N'INSERT NHAN_VIEN (ma_nv, ma_tk, ho_ten, email) VALUES (''NVX'', ''TK102'', N''Test'', ''x@x.vn'')'),
 (N'TRIGGER: KH gắn tài khoản vai trò NHAN_VIEN',  N'UPDATE KHACH_HANG SET ma_tk = ''TK003'' WHERE ma_kh = ''KH016'''),
 (N'TRIGGER v2: đổi vai trò TK khách thành NHAN_VIEN', N'UPDATE TAI_KHOAN SET vai_tro = ''NHAN_VIEN'' WHERE ma_tk = ''TK101'''),
 (N'TRIGGER v2: đổi vai trò TK nhân viên thành KHACH_HANG', N'UPDATE TAI_KHOAN SET vai_tro = ''KHACH_HANG'' WHERE ma_tk = ''TK003'''),
 (N'NOT NULL: hóa đơn không có nhân viên lập', N'INSERT HOA_DON (ma_hoa_don, ma_nv, phuong_thuc_tt) VALUES (''HDX'', NULL, ''TIEN_MAT'')'),
 (N'FK: đơn dùng voucher không tồn tại',    N'UPDATE DON_DAT_HANG SET ma_voucher = ''KHONGCO'' WHERE ma_don = ''DH00001'''),
 (N'FK: xóa loại SP còn sản phẩm',          N'DELETE LOAI_SP WHERE ma_loai_sp = ''LSP01'''),
 (N'FK: chi tiết hóa đơn với SP không tồn tại', N'INSERT CHI_TIET_HOA_DON VALUES (''HD00001'', ''SPX'', 1, 1, 1)'),
 (N'FK: xóa nhà cung cấp đã có phiếu nhập', N'DELETE NHA_CUNG_CAP WHERE ma_ncc = ''NCC01'''),
 (N'CHECK: khuyến mãi ngày kết thúc trước ngày bắt đầu', N'INSERT KHUYEN_MAI VALUES (''KMX'', N''Test'', ''HOA_DON'', NULL, ''2026-12-31'', ''2026-01-01'')'),
 (N'CHECK: phần trăm giảm > 100',           N'UPDATE CT_KM_SAN_PHAM SET phan_tram_giam_gia = 150 WHERE ma_km = ''KM01'' AND ma_sp = ''SP001'''),
 (N'CHECK v2: tiền giảm đơn hàng âm',       N'UPDATE DON_DAT_HANG SET tien_giam = -1 WHERE ma_don = ''DH00001'''),
 (N'CHECK: trạng thái hóa đơn sai',         N'UPDATE HOA_DON SET trang_thai = ''XONG'' WHERE ma_hoa_don = ''HD00001'''),
 (N'CHECK: phương thức thanh toán đơn sai', N'UPDATE DON_DAT_HANG SET phuong_thuc_tt = ''TIEN_MAT'' WHERE ma_don = ''DH00001'''),
 (N'CHECK: số lượng chi tiết giỏ hàng = 0', N'UPDATE CHI_TIET_GIO_HANG SET so_luong = 0'),
 (N'CHECK: loại đổi trả sai',               N'UPDATE DOI_TRA_HANG SET loai = ''HUY'' WHERE ma_doi_tra = ''DT00001''');

DECLARE @i int = 1, @n int = (SELECT COUNT(*) FROM @tests), @ten nvarchar(200), @lenh nvarchar(max);
WHILE @i <= @n
BEGIN
    SELECT @ten = ten, @lenh = lenh FROM @tests WHERE stt = @i;
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC sp_executesql @lenh;
        ROLLBACK TRANSACTION;
        INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu) VALUES ('B', @ten, 'FAIL', N'Lệnh sai KHÔNG bị chặn');
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu) VALUES ('B', @ten, 'PASS', LEFT(ERROR_MESSAGE(), 400));
    END CATCH
    SET @i += 1;
END

-- B. Truong hop HOP LE phai chay duoc
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT KHACH_HANG (ma_kh, ma_tk, ho_ten) VALUES ('KHX1', NULL, N'Khách lẻ 1'), ('KHX2', NULL, N'Khách lẻ 2');
    UPDATE TAI_KHOAN SET trang_thai = 'KHOA' WHERE ma_tk = 'TK101';   -- doi cot khac vai_tro: trigger cho qua
    ROLLBACK TRANSACTION;
    INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
    VALUES ('B', N'Hợp lệ: nhiều khách tại quầy không tài khoản; khóa tài khoản', 'PASS', N'Chạy được như mong muốn');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu)
    VALUES ('B', N'Hợp lệ: nhiều khách tại quầy không tài khoản; khóa tài khoản', 'FAIL', LEFT(ERROR_MESSAGE(), 400));
END CATCH

/* ====================== C. THONG KE YEU CAU 5, 6 ====================== */
-- Doi chieu: tong theo thang = tong theo quy = tong theo nam = tong A->B toan bo
DECLARE @tong_all decimal(18,2) = (SELECT SUM(tong_tien) FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN');
DECLARE @tong_thang decimal(18,2) = (SELECT SUM(t) FROM (SELECT SUM(tong_tien) t FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN'
                                       GROUP BY YEAR(ngay_lap), MONTH(ngay_lap)) x);
DECLARE @tong_quy decimal(18,2) = (SELECT SUM(t) FROM (SELECT SUM(tong_tien) t FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN'
                                     GROUP BY YEAR(ngay_lap), DATEPART(QUARTER, ngay_lap)) x);
DECLARE @tong_nv decimal(18,2) = (SELECT SUM(t) FROM (SELECT SUM(tong_tien) t FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN'
                                    GROUP BY ma_nv, YEAR(ngay_lap)) x);
DECLARE @tong_sp decimal(18,2) = (SELECT SUM(c.thanh_tien) FROM CHI_TIET_HOA_DON c JOIN HOA_DON h ON h.ma_hoa_don = c.ma_hoa_don
                                  WHERE h.trang_thai = 'DA_THANH_TOAN');
DECLARE @tong_giam decimal(18,2) = (SELECT SUM(tien_giam) FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN');
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu) VALUES
 ('C', N'5a/5b: tổng theo tháng = theo quý = cả kỳ', CASE WHEN @tong_all = @tong_thang AND @tong_all = @tong_quy THEN 'PASS' ELSE 'FAIL' END,
       CONCAT(N'Cả kỳ ', FORMAT(@tong_all, 'N0'), N' đ')),
 ('C', N'5c: tổng theo nhân viên x năm = cả kỳ', CASE WHEN @tong_all = @tong_nv THEN 'PASS' ELSE 'FAIL' END, NULL),
 ('C', N'5e: tổng theo sản phẩm - tiền giảm = cả kỳ', CASE WHEN @tong_sp - @tong_giam = @tong_all THEN 'PASS' ELSE 'FAIL' END,
       CONCAT(N'Tổng tiền hàng ', FORMAT(@tong_sp, 'N0'), N' đ, giảm ', FORMAT(@tong_giam, 'N0'), N' đ'));

-- 6: HOP (UNION) phai bang OR; GIAO (INTERSECT) phai la tap con cua HOP
DECLARE @hop int = (SELECT COUNT(*) FROM (
            SELECT ma_kh FROM KHACH_HANG WHERE tinh_thanh = N'TP Hồ Chí Minh'
            UNION SELECT ma_kh FROM KHACH_HANG WHERE tinh_thanh = N'Hà Nội') x);
DECLARE @or int = (SELECT COUNT(*) FROM KHACH_HANG WHERE tinh_thanh = N'TP Hồ Chí Minh' OR tinh_thanh = N'Hà Nội');
DECLARE @giao int = (SELECT COUNT(*) FROM (
            SELECT ma_kh FROM KHACH_HANG WHERE tinh_thanh = N'TP Hồ Chí Minh'
            INTERSECT SELECT ma_kh FROM DON_DAT_HANG WHERE dia_chi_giao LIKE N'%Hà Nội%') x);
INSERT #ket_qua (nhom, ten, ket_qua, ghi_chu) VALUES
 ('C', N'6: HỢP (UNION) cho cùng kết quả với OR', CASE WHEN @hop = @or AND @hop > 0 THEN 'PASS' ELSE 'FAIL' END,
       CONCAT(@hop, N' khách TP HCM hoặc Hà Nội')),
 ('C', N'6: GIAO (INTERSECT) có dữ liệu để demo', CASE WHEN @giao > 0 THEN 'PASS' ELSE 'FAIL' END,
       CONCAT(@giao, N' khách ở TP HCM có đơn giao ra Hà Nội'));

/* ============================== TONG KET ============================== */
SELECT stt, nhom, ket_qua, ten, ghi_chu FROM #ket_qua ORDER BY stt;
SELECT N'TỔNG KẾT' AS muc, SUM(CASE WHEN ket_qua = 'PASS' THEN 1 ELSE 0 END) AS pass,
       SUM(CASE WHEN ket_qua = 'FAIL' THEN 1 ELSE 0 END) AS fail, COUNT(*) AS tong
FROM #ket_qua;
GO

/* =============== C2. KET QUA CAC CAU THONG KE (de xem so) =============== */
-- 5a. Tong tien hoa don tu ngay A den ngay B
DECLARE @A date = '2025-01-01', @B date = '2025-06-30';
SELECT N'5a' AS yc, @A AS tu_ngay, @B AS den_ngay, COUNT(*) AS so_hoa_don, SUM(tong_tien) AS tong_tien
FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN' AND ngay_lap BETWEEN @A AND @B;

-- 5b. Thu - chi - loi nhuan theo quy cua tung nam
WITH thu AS (SELECT YEAR(ngay_lap) nam, DATEPART(QUARTER, ngay_lap) quy, SUM(tong_tien) tien
             FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN' GROUP BY YEAR(ngay_lap), DATEPART(QUARTER, ngay_lap)),
     chi AS (SELECT nam, quy, SUM(tien) tien FROM (
                 SELECT YEAR(ngay_nhap) nam, DATEPART(QUARTER, ngay_nhap) quy, tong_tien tien FROM PHIEU_NHAP
                 UNION ALL
                 SELECT YEAR(ngay_hoan), DATEPART(QUARTER, ngay_hoan), tien_hoan FROM DOI_TRA_HANG
                 WHERE trang_thai = 'HOAN_TAT' AND tien_hoan > 0) x GROUP BY nam, quy),
     ky AS (SELECT nam, quy FROM thu UNION SELECT nam, quy FROM chi)
SELECT N'5b' AS yc, k.nam, k.quy, ISNULL(t.tien, 0) AS tong_thu, ISNULL(c.tien, 0) AS tong_chi,
       ISNULL(t.tien, 0) - ISNULL(c.tien, 0) AS loi_nhuan
FROM ky k LEFT JOIN thu t ON t.nam = k.nam AND t.quy = k.quy LEFT JOIN chi c ON c.nam = k.nam AND c.quy = k.quy
ORDER BY k.nam, k.quy;

-- 5c. Doanh thu theo nhan vien va nam: cot NV1, NV2, ... Tong cong (PIVOT dong)
DECLARE @cot nvarchar(max) = (SELECT STRING_AGG(QUOTENAME(ma_nv), ',') WITHIN GROUP (ORDER BY ma_nv)
                              FROM (SELECT DISTINCT ma_nv FROM HOA_DON) x);
DECLARE @cong nvarchar(max) = (SELECT STRING_AGG('ISNULL(' + QUOTENAME(ma_nv) + ',0)', '+') WITHIN GROUP (ORDER BY ma_nv)
                               FROM (SELECT DISTINCT ma_nv FROM HOA_DON) x);
DECLARE @hien nvarchar(max) = (SELECT STRING_AGG('ISNULL(' + QUOTENAME(ma_nv) + ',0) AS ' + QUOTENAME(ma_nv), ',')
                               WITHIN GROUP (ORDER BY ma_nv) FROM (SELECT DISTINCT ma_nv FROM HOA_DON) x);
DECLARE @sql nvarchar(max) = N'SELECT N''5c'' AS yc, nam, ' + @hien + N', ' + @cong + N' AS tong_cong
FROM (SELECT YEAR(ngay_lap) nam, ma_nv, tong_tien FROM HOA_DON WHERE trang_thai = ''DA_THANH_TOAN'') s
PIVOT (SUM(tong_tien) FOR ma_nv IN (' + @cot + N')) p ORDER BY nam;';
EXEC sp_executesql @sql;

-- 5d. Doanh thu theo khach hang va quy cua 1 nam (top 5 khach + gop con lai, khach le rieng)
DECLARE @nam int = 2025;
WITH x AS (
    SELECT CASE WHEN h.ma_kh IS NULL THEN N'Khách vãng lai'
                WHEN h.ma_kh IN (SELECT TOP 5 ma_kh FROM HOA_DON WHERE trang_thai = 'DA_THANH_TOAN' AND YEAR(ngay_lap) = @nam
                                 AND ma_kh IS NOT NULL GROUP BY ma_kh ORDER BY SUM(tong_tien) DESC) THEN h.ma_kh
                ELSE N'Khách khác' END AS khach,
           DATEPART(QUARTER, h.ngay_lap) quy, h.tong_tien
    FROM HOA_DON h WHERE h.trang_thai = 'DA_THANH_TOAN' AND YEAR(h.ngay_lap) = @nam)
SELECT N'5d' AS yc, khach, ISNULL([1], 0) AS Q1, ISNULL([2], 0) AS Q2, ISNULL([3], 0) AS Q3, ISNULL([4], 0) AS Q4,
       ISNULL([1], 0) + ISNULL([2], 0) + ISNULL([3], 0) + ISNULL([4], 0) AS tong_cong
FROM x PIVOT (SUM(tong_tien) FOR quy IN ([1], [2], [3], [4])) p
ORDER BY tong_cong DESC;

-- 5e. Theo san pham va quy (giam doc xem): dong la quy, cot la loai SP de vua man hinh
SET @cot = (SELECT STRING_AGG(QUOTENAME(ten_loai_sp), ',') WITHIN GROUP (ORDER BY ma_loai_sp) FROM LOAI_SP);
SET @cong = (SELECT STRING_AGG('ISNULL(' + QUOTENAME(ten_loai_sp) + ',0)', '+') WITHIN GROUP (ORDER BY ma_loai_sp) FROM LOAI_SP);
SET @hien = (SELECT STRING_AGG('ISNULL(' + QUOTENAME(ten_loai_sp) + ',0) AS ' + QUOTENAME(ten_loai_sp), ',')
             WITHIN GROUP (ORDER BY ma_loai_sp) FROM LOAI_SP);
SET @sql = N'SELECT N''5e'' AS yc, ky, ' + @hien + N', ' + @cong + N' AS tong_cong
FROM (SELECT CONCAT(YEAR(h.ngay_lap), ''-Q'', DATEPART(QUARTER, h.ngay_lap)) ky, l.ten_loai_sp, c.thanh_tien
      FROM CHI_TIET_HOA_DON c JOIN HOA_DON h ON h.ma_hoa_don = c.ma_hoa_don
      JOIN SAN_PHAM s ON s.ma_sp = c.ma_sp JOIN LOAI_SP l ON l.ma_loai_sp = s.ma_loai_sp
      WHERE h.trang_thai = ''DA_THANH_TOAN'') src
PIVOT (SUM(thanh_tien) FOR ten_loai_sp IN (' + @cot + N')) p ORDER BY ky;';
EXEC sp_executesql @sql;

-- 6. Tim kiem nang cao: hoa don co tong tien >= 3.000.000
SELECT N'6' AS yc, ma_hoa_don, ngay_lap, ma_kh, tong_tien FROM HOA_DON
WHERE trang_thai = 'DA_THANH_TOAN' AND tong_tien >= 3000000 ORDER BY tong_tien DESC;

-- 6. Khach TP HCM va Ha Noi: HOP
SELECT N'6-HOP' AS yc, ma_kh, ho_ten, tinh_thanh FROM KHACH_HANG WHERE tinh_thanh = N'TP Hồ Chí Minh'
UNION
SELECT N'6-HOP', ma_kh, ho_ten, tinh_thanh FROM KHACH_HANG WHERE tinh_thanh = N'Hà Nội';

-- 6. GIAO: khach vua mua tai quay vua dat online
SELECT N'6-GIAO' AS yc, k.ma_kh, k.ho_ten FROM KHACH_HANG k
WHERE k.ma_kh IN (SELECT ma_kh FROM HOA_DON WHERE ma_don IS NULL AND ma_kh IS NOT NULL
                  INTERSECT SELECT ma_kh FROM DON_DAT_HANG);
GO
