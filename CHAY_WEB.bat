@echo off
chcp 65001 >nul
rem Chay WebApi (cong 5212), web ban hang (5173), web staff (5174). Lan dau se npm install, mat vai phut.
cd /d "%~dp0"
start "WebApi - cong 5212" cmd /k "cd /d backend\WebApi && dotnet run --launch-profile http"
start "Web ban hang - cong 5173" cmd /k "cd /d web-ban-hang && (if not exist node_modules npm install) && npm run dev"
start "Web staff - cong 5174" cmd /k "cd /d web-staff && (if not exist node_modules npm install) && npm run dev"
echo Dang khoi dong 3 cua so. Doi WebApi bao "Now listening on http://localhost:5212" va 2 web bao "Local: http://localhost:517x".
echo Web ban hang: http://localhost:5173
echo Web staff   : http://localhost:5174   (dang nhap thungan1 / 123456)
echo Swagger API : http://localhost:5212/swagger
pause
