@echo off
chcp 65001 > nul
echo Starting Lab6 - three windows will open...
echo.

echo [1/3] Starting Processor...
start "Lab6 Processor" cmd /k "chcp 65001 > nul && cd /d %~dp0Lab6_Processor && dotnet run"
timeout /t 4 /nobreak > nul

echo [2/3] Starting Distributor...
start "Lab6 Distributor" cmd /k "chcp 65001 > nul && cd /d %~dp0Lab6_Distributor && dotnet run"
timeout /t 4 /nobreak > nul

echo [3/3] Starting Publisher...
start "Lab6 Publisher" cmd /k "chcp 65001 > nul && cd /d %~dp0Lab6_Publisher && dotnet run"

echo.
echo All three apps started. Close this window.
pause
