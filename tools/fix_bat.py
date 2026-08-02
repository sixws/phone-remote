# -*- coding: utf-8 -*-
"""把 启动.bat 写成纯 ASCII + CRLF（bat 内不含中文，杜绝编码问题），
chcp 65001 让 .NET 程序的 UTF-8 中文正常显示"""
content = """@echo off
setlocal
chcp 65001 >nul
title Phone Remote Server
cd /d %~dp0
echo ==============================================
echo   Phone Remote Server - PC side service
echo   First launch compiles, please wait...
echo ==============================================
echo.
dotnet run --project PhoneRemoteServer --no-launch-profile
echo.
echo Server stopped.
pause
"""
path = r"C:\Users\Administrator\.zcode\workspace\default\phone-remote\启动.bat"
with open(path, "w", encoding="ascii", newline="\r\n") as f:
    f.write(content)
print("已写入 ASCII+CRLF:", path)
