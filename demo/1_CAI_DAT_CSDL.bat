@echo off
rem ======================================================================
rem  Buoc 1: tao CSDL demo QL_BanThoiTrang (cau truc v3 + du lieu mau).
rem  - Tu tim may chu SQL: localhost, neu khong co thi .\SQLEXPRESS
rem  - Da co CSDL QL_BanThoiTrang thi GIU NGUYEN, khong xoa / ghi de.
rem  - Tao xong chay script kiem thu CSDL (56 test PASS / FAIL).
rem  Chi can chay 1 lan tren moi may.
rem ======================================================================
setlocal EnableDelayedExpansion
chcp 65001 >nul
cd /d "%~dp0.."
title Demo - Cai dat CSDL
echo.
echo ===== CAI DAT CSDL DEMO: QL_BanThoiTrang =====
echo.

rem ---- kiem tra phan mem can co
where sqlcmd >nul 2>nul
if errorlevel 1 (
    echo [LOI] Khong tim thay sqlcmd. Cai SQL Server ^(Express / Developer^) kem cong cu dong lenh,
    echo       hoac mo database\QL_BanThoiTrang_v3.sql roi database\QL_BanThoiTrang_DuLieuMau.sql bang SSMS va bam Execute.
    goto :ket
)
where dotnet >nul 2>nul || echo [CANH BAO] Chua cai .NET 8 SDK - can de chay WebApi va App Admin.
where npm >nul 2>nul || echo [CANH BAO] Chua cai Node.js - can de chay 2 web.

rem ---- tim may chu SQL
set "MAY="
for %%S in (localhost .\SQLEXPRESS) do (
    if not defined MAY (
        sqlcmd -S "%%S" -E -C -l 5 -b -Q "SET NOCOUNT ON" >nul 2>nul
        if not errorlevel 1 set "MAY=%%S"
    )
)
if not defined MAY (
    echo [LOI] Khong ket noi duoc SQL Server o localhost hay .\SQLEXPRESS ^(Windows Authentication^).
    echo       Kiem tra dich vu SQL Server dang chay ^(services.msc^).
    goto :ket
)
echo May chu SQL: %MAY%
(echo %MAY%)> "%~dp0may_chu.txt"

rem ---- da co CSDL thi giu nguyen
set "CO=0"
for /f "usebackq delims=" %%a in (`sqlcmd -S "%MAY%" -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'QL_BanThoiTrang') IS NULL THEN 0 ELSE 1 END"`) do set "CO=%%a"
if "%CO%"=="1" (
    echo.
    echo Da co CSDL QL_BanThoiTrang tren %MAY% - GIU NGUYEN, khong tao lai.
    echo Muon tro ve du lieu mau sach: chay 4_LAM_MOI_DU_LIEU.bat ^(co sao luu truoc^).
    goto :kiemthu
)

echo.
echo [1/2] Tao cau truc CSDL ^(23 bang^)...
sqlcmd -S "%MAY%" -E -C -I -b -f 65001 -i "database\QL_BanThoiTrang_v3.sql" >nul
if errorlevel 1 ( echo [LOI] Tao cau truc that bai. & goto :ket )
echo [2/2] Nap du lieu mau ^(18 san pham UNIQLO, 195 SKU, 250 hoa don, tai khoan demo^)...
sqlcmd -S "%MAY%" -E -C -I -b -f 65001 -i "database\QL_BanThoiTrang_DuLieuMau.sql" >nul
if errorlevel 1 ( echo [LOI] Nap du lieu mau that bai. & goto :ket )
echo Xong.

:kiemthu
echo.
echo Kiem thu CSDL ^(script chi doc, khong doi du lieu^)...
set "PASS=0" & set "FAIL=0"
sqlcmd -S "%MAY%" -E -C -I -W -f 65001 -i "database\KiemThu_QL_BanThoiTrang.sql" > "%TEMP%\kiemthu_csdl_demo.txt" 2>&1
for /f %%a in ('find /c "PASS" ^< "%TEMP%\kiemthu_csdl_demo.txt"') do set "PASS=%%a"
for /f %%a in ('find /c "FAIL" ^< "%TEMP%\kiemthu_csdl_demo.txt"') do set "FAIL=%%a"
echo Ket qua: %PASS% PASS, %FAIL% FAIL  ^(chi tiet: %TEMP%\kiemthu_csdl_demo.txt^)
echo.
echo Buoc tiep theo: chay 2_CHAY_DEMO.bat

:ket
echo.
if /i not "%~1"=="khongdung" pause
endlocal
