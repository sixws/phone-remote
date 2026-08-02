@echo off
setlocal
set "DST=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\PhoneRemoteServer.vbs"
> "%DST%" (
    echo Set ws = CreateObject("Wscript.Shell"^)
    echo ws.Run """%~dp0server\PhoneRemoteServer.exe""", 0, False
)
if exist "%DST%" (
    echo 已安装开机自启：服务将在下次开机/重新登录时自动后台启动
    echo 如需卸载，运行 卸载开机自启.bat
) else (
    echo 安装失败：请检查权限或路径
)
pause
