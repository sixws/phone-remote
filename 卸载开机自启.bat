@echo off
setlocal
set "DST=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\PhoneRemoteServer.vbs"
if exist "%DST%" (
    del /Q "%DST%" >nul 2>&1
    echo 已卸载开机自启
) else (
    echo 当前未安装开机自启
)
pause
