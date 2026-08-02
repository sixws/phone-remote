# -*- coding: utf-8 -*-
"""查找可能拦截点击的全屏/置顶窗口 + 可疑进程"""
import ctypes
import ctypes.wintypes as wt

user32 = ctypes.windll.user32
GetWindowTextW = user32.GetWindowTextW
IsWindowVisible = user32.IsWindowVisible
GetWindowRect = user32.GetWindowRect
GetWindowLongW = user32.GetWindowLongW
GWL_EXSTYLE = -20
WS_EX_TOPMOST = 0x8

SW = user32.GetSystemMetrics(0)
SH = user32.GetSystemMetrics(1)
print("屏幕尺寸: %dx%d" % (SW, SH))

WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)


def proc(h, l):
    if not IsWindowVisible(h):
        return True
    buf = ctypes.create_unicode_buffer(256)
    GetWindowTextW(h, buf, 256)
    title = buf.value
    r = wt.RECT()
    GetWindowRect(h, ctypes.byref(r))
    w, ht = r.right - r.left, r.bottom - r.top
    topmost = bool(GetWindowLongW(h, GWL_EXSTYLE) & WS_EX_TOPMOST)
    # 有标题 或 接近全屏 或 置顶
    if title or (w >= SW * 0.9 and ht >= SH * 0.9) or (topmost and w > 100):
        print("窗口: %r  位置(%d,%d) 尺寸(%dx%d) 置顶=%s" % (title, r.left, r.top, w, ht, topmost))
    return True


EnumWindows = user32.EnumWindows
EnumWindows(WNDENUMPROC(proc), 0)

fg = user32.GetForegroundWindow()
buf = ctypes.create_unicode_buffer(256)
GetWindowTextW(fg, buf, 256)
print("当前前台窗口:", repr(buf.value))
