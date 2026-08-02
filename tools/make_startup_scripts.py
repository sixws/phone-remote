# -*- coding: utf-8 -*-
"""生成开机自启脚本：
- 安装开机自启.bat（GBK）：把隐藏启动器写入系统启动文件夹
- 卸载开机自启.bat（GBK）：删除启动文件夹里的启动器
- 启动.bat（ASCII）：优先运行已发布的 exe（秒启），没有则编译运行
"""
import os

root = r"C:\Users\Administrator\.zcode\workspace\default\phone-remote"

install = r'''@echo off
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
'''

uninstall = r'''@echo off
setlocal
set "DST=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\PhoneRemoteServer.vbs"
if exist "%DST%" (
    del /Q "%DST%" >nul 2>&1
    echo 已卸载开机自启
) else (
    echo 当前未安装开机自启
)
pause
'''

launcher = r'''@echo off
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
'''

files = [
    (os.path.join(root, "安装开机自启.bat"), install, "gbk"),
    (os.path.join(root, "卸载开机自启.bat"), uninstall, "gbk"),
    (os.path.join(root, "启动.bat"), launcher, "ascii"),
]
for path, content, enc in files:
    with open(path, "w", encoding=enc, newline="\r\n") as f:
        f.write(content)
    print("已生成:", os.path.basename(path), "(" + enc + "+CRLF)")
