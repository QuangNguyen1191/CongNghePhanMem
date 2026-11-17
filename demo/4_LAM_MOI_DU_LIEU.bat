@echo off
rem ======================================================================
rem  Tro CSDL QL_BanThoiTrang ve du lieu mau sach (vd sau khi tap demo).
rem  AN TOAN: sao luu CSDL hien tai ra file .bak TRUOC khi xoa; sao luu loi thi dung lai, khong xoa.
rem  Phai go LAMMOI de xac nhan.
rem ======================================================================
setlocal
chcp 65001 >nul
cd /d "%~dp0.."
title Demo - Lam moi du lieu
if not exist "%~dp0may_chu.txt" ( echo Chua cai CSDL demo. Chay 1_CAI_DAT_CSDL.bat truoc. & pause & exit /b 1 )
set /p MAY=<"%~dp0may_chu.txt"
for /f "tokens=* delims= " %%a in ("%MAY%") do set "MAY=%%a"

echo.
echo CANH BAO: XOA toan bo du lieu hien tai cua QL_BanThoiTrang tren %MAY%
echo           ^(don, hoa don, khach... da nhap them^) va nap lai du lieu mau.
echo           CSDL hien tai se duoc sao luu ra file .bak truoc khi xoa.
echo           Nho tat demo ^(3_DUNG_DEMO.bat^) va App Admin truoc.
echo.
set "XN="
set /p XN=Go LAMMOI roi Enter de tiep tuc (Enter de huy):
if /i not "%XN%"=="LAMMOI" ( echo Da huy, khong thay doi gi. & pause & exit /b 0 )

echo.
echo [1/3] Sao luu CSDL hien tai...
sqlcmd -S "%MAY%" -E -C -b -W -h -1 -Q "SET NOCOUNT ON; IF DB_ID(N'QL_BanThoiTrang') IS NULL BEGIN PRINT N'(chua co CSDL, bo qua sao luu)'; RETURN; END; DECLARE @f nvarchar(400) = CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(300)) + N'\QL_BanThoiTrang_truoc_lam_moi_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + N'.bak'; BACKUP DATABASE QL_BanThoiTrang TO DISK = @f WITH COPY_ONLY, INIT; PRINT N'Da sao luu: ' + @f;"
if errorlevel 1 ( echo [LOI] Sao luu that bai - DUNG LAI, khong xoa gi. & pause & exit /b 1 )

echo [2/3] Xoa CSDL cu...
sqlcmd -S "%MAY%" -E -C -b -Q "IF DB_ID(N'QL_BanThoiTrang') IS NOT NULL BEGIN ALTER DATABASE QL_BanThoiTrang SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE QL_BanThoiTrang; END" >nul
if errorlevel 1 ( echo [LOI] Khong xoa duoc CSDL ^(con chuong trinh dang dung?^). Ban sao luu van con. & pause & exit /b 1 )

echo [3/3] Tao lai tu du lieu mau...
call "%~dp01_CAI_DAT_CSDL.bat" khongdung
pause
