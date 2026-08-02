@echo off
setlocal
chcp 65001 >nul
title Phone Remote Server
cd /d %~dp0
if exist "%~dp0server\PhoneRemoteServer.exe" (
    "%~dp0server\PhoneRemoteServer.exe"
) else (
    echo First launch compiles, please wait...
    dotnet run --project PhoneRemoteServer --no-launch-profile
)
echo.
echo Server stopped.
pause
