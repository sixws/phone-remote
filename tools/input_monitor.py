# -*- coding: utf-8 -*-
"""输入失灵监控：60秒内记录按键状态/前台窗口/置顶窗口，用于定位任务栏失灵原因"""
import ctypes
import ctypes.wintypes as wt
import time

user32 = ctypes.windll.user32
GetWindowTextW = user32.GetWindowTextW
IsWindowVisible = user32.IsWindowVisible
GetWindowRect = user32.GetWindowRect
GetWindowLongW = user32.GetWindowLongW
GWL_EXSTYLE = -20
WS_EX_TOPMOST = 0x8
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]

LOG = r"C:\Users\Administrator\.zcode\workspace\default\phone-remote\tools\monitor_log.txt"
WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)


def topmost_windows():
    found = []

    def proc(h, l):
        if IsWindowVisible(h) and (GetWindowLongW(h, GWL_EXSTYLE) & WS_EX_TOPMOST):
            buf = ctypes.create_unicode_buffer(256)
            GetWindowTextW(h, buf, 256)
            r = wt.RECT()
            GetWindowRect(h, ctypes.byref(r))
            if r.right - r.left > 50:
                found.append("%r(%d,%d,%d,%d)" % (buf.value, r.left, r.top, r.right, r.bottom))
        return True

    user32.EnumWindows(WNDENUMPROC(proc), 0)
    return found


def fg():
    h = user32.GetForegroundWindow()
    buf = ctypes.create_unicode_buffer(256)
    GetWindowTextW(h, buf, 256)
    return buf.value


def left_down():
    return bool(user32.GetAsyncKeyState(0x01) & 0x8000)


with open(LOG, "w", encoding="utf-8") as f:
    f.write("=== 监控开始：请把鼠标移到任务栏触发失灵，勿按 Win+L ===\n")
    f.flush()
    for i in range(120):  # 60 秒
        t = time.strftime("%H:%M:%S")
        line = "[%s] 左键=%s 前台=%r 置顶窗=%s" % (
            t, "按下" if left_down() else "松开", fg(), topmost_windows())
        f.write(line + "\n")
        f.flush()
        time.sleep(0.5)
    f.write("=== 监控结束 ===\n")
