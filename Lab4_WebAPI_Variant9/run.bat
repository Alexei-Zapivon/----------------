@echo off
chcp 65001 > nul
title Lab4 - Warehouse API
cd /d "%~dp0WarehouseApi"
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5000
echo.
echo  Starting Lab4_WebAPI_Variant9...
echo  Swagger UI: http://localhost:5000
echo.
dotnet run
pause
