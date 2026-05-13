@echo off
chcp 65001 > nul
title Lab5 - Warehouse Client
cd /d "%~dp0"
echo.
echo  Starting Lab5_Client_Variant9...
echo  Make sure Lab4 API is running at http://localhost:5000
echo.
dotnet run
pause
