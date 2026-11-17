@echo off
rem ======================================================================
rem  Buoc 2: chay toan bo demo bang 1 lan bam
rem    WebApi (5212) + web ban hang (5173) + web staff (5174) + App Admin
rem  Lan dau tu cai thu vien (npm install, NuGet restore): mat vai phut.
rem  Doi xong tu mo trinh duyet. Tat demo: 3_DUNG_DEMO.bat
rem ======================================================================
setlocal
chcp 65001 >nul
cd /d "%~dp0.."
title Demo - Khoi dong

if not exist "%~dp0may_chu.txt" (
    echo Chua cai CSDL demo. Chay 1_CAI_DAT_CSDL.bat truoc.
    pause
    exit /b 1
)
set /p MAY=<"%~dp0may_chu.txt"
for /f "tokens=* delims= " %%a in ("%MAY%") do set "MAY=%%a"

rem WebApi va App Admin cung doc bien nay -> tro dung may chu SQL cua may nay
set "ConnectionStrings__QLBanThoiTrang=Server=%MAY%;Database=QL_BanThoiTrang;Trusted_Connection=True;TrustServerCertificate=True"
echo May chu SQL: %MAY%
echo.

echo [1/4] WebApi ^(cong 5212^)...
start "Demo - WebApi 5212" cmd /k "cd /d backend\WebApi && dotnet run --launch-profile http"
echo [2/4] Web ban hang ^(cong 5173^)...
start "Demo - Web ban hang 5173" cmd /k "cd /d web-ban-hang && (if not exist node_modules npm install) && npm run dev"
echo [3/4] Web staff ^(cong 5174^)...
start "Demo - Web staff 5174" cmd /k "cd /d web-staff && (if not exist node_modules npm install) && npm run dev"

echo.
echo Dang doi WebApi san sang ^(lan dau co the mat 1-3 phut^)...
call :doi http://localhost:5212/api/san-pham WebApi || goto :loi

echo [4/4] App Admin...
tasklist /fi "imagename eq AppAdmin.exe" | find /i "AppAdmin.exe" >nul
if errorlevel 1 (
    start "Demo - App Admin" /min cmd /c "dotnet run --project backend\AppAdmin"
) else (
    echo App Admin dang mo san, khong mo them.
)

echo Dang doi 2 web san sang...
call :doi http://localhost:5173/ "Web ban hang" || goto :loi
call :doi http://localhost:5174/ "Web staff" || goto :loi
start "" http://localhost:5173
start "" http://localhost:5174

echo.
echo ================= DEMO DA SAN SANG =================
echo  Web ban hang : http://localhost:5173   ^(khach01 / 123456^)
echo  Web staff    : http://localhost:5174   ^(thungan1, banhangonline, thukho / 123456^)
echo  App Admin    : cua so rieng           ^(giamdoc hoac quanly / 123456^)
echo  Swagger API  : http://localhost:5212/swagger
echo  Kich ban demo: demo\README.md
echo  Tat demo     : demo\3_DUNG_DEMO.bat
echo ====================================================
echo.
pause
exit /b 0

:loi
echo.
echo [LOI] Qua thoi gian doi. Xem thong bao loi trong cac cua so "Demo - ..." vua mo.
pause
exit /b 1

rem ---- doi 1 dia chi tra loi (toi da ~4 phut). Dung curl co san tu Windows 10.
:doi
set /a SOLAN=0
:doi_lap
curl -s -f -o nul "%~1" >nul 2>nul && ( echo   %~2: san sang & exit /b 0 )
set /a SOLAN+=1
if %SOLAN% geq 80 exit /b 1
rem cho 3 giay (ping thay cho timeout: timeout loi khi dau vao bi chuyen huong)
ping -n 4 127.0.0.1 >nul
goto :doi_lap
