@echo off
rem Tat demo: dung WebApi (5212), web ban hang (5173), web staff (5174) va App Admin do 2_CHAY_DEMO.bat mo.
rem App Admin mo tu truoc (khong phai do demo mo) thi giu nguyen.
chcp 65001 >nul
echo Dang tat demo...
powershell -NoProfile -Command "$ids = Get-NetTCPConnection -State Listen -LocalPort 5212,5173,5174 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique; foreach ($i in $ids) { Stop-Process -Id $i -Force -ErrorAction SilentlyContinue }"
taskkill /fi "WINDOWTITLE eq Demo - *" /t /f >nul 2>nul
echo Da tat WebApi, 2 web va App Admin cua demo. Du lieu da luu trong CSDL.
ping -n 3 127.0.0.1 >nul
