@echo off
setlocal
echo ==============================================
echo   正在卸载 Remote Mouse（流氓输入钩子软件）
echo   请确认本窗口是以【管理员身份】运行
echo ==============================================

echo [1/4] 停止并删除服务...
sc stop RemoteMouseService >nul 2>&1
sc delete RemoteMouseService >nul 2>&1

echo [2/4] 结束残留进程...
taskkill /F /IM RemoteMouseCore.exe >nul 2>&1
taskkill /F /IM RemoteMouse.exe >nul 2>&1
taskkill /F /IM RemoteMouseService.exe >nul 2>&1

echo [3/4] 删除程序文件夹...
rmdir /S /Q "C:\Program Files (x86)\Remote Mouse" >nul 2>&1
rmdir /S /Q "C:\Program Files\Remote Mouse" >nul 2>&1

echo [4/4] 清理注册表卸载项...
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RemoteMouse_is1" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\RemoteMouse_is1" /f >nul 2>&1
reg delete "HKCU\Software\RemoteMouse" /f >nul 2>&1

echo.
echo ==============================================
echo   卸载完成！建议重启电脑
echo ==============================================
pause
