# -*- coding: utf-8 -*-
"""一键检查当前输入状态：谁持有鼠标捕获、前台是谁"""
import ctypes
import ctypes.wintypes as wt

user32 = ctypes.windll.user32


class GUITHREADINFO(ctypes.Structure):
    _fields_ = [
        ("cbSize", ctypes.c_uint),
        ("flags", ctypes.c_uint),
        ("hwndActive", ctypes.c_void_p),
        ("hwndFocus", ctypes.c_void_p),
        ("hwndCapture", ctypes.c_void_p),
        ("hwndMenuOwner", ctypes.c_void_p),
        ("hwndMoveSize", ctypes.c_void_p),
        ("hwndCaret", ctypes.c_void_p),
        ("rcCaret", wt.RECT),
    ]


def title(h):
    if not h:
        return ""
    buf = ctypes.create_unicode_buffer(256)
    user32.GetWindowTextW(h, buf, 256)
    return buf.value


def pid_of(h):
    pid = ctypes.c_ulong()
    user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
    return pid.value


fg = user32.GetForegroundWindow()
print("前台窗口: %r (pid=%d)" % (title(fg), pid_of(fg)))

tid = user32.GetWindowThreadProcessId(fg, None)
info = GUITHREADINFO()
info.cbSize = ctypes.sizeof(GUITHREADINFO)
if user32.GetGUIThreadInfo(tid, ctypes.byref(info)):
    if info.hwndCapture:
        print("鼠标捕获: 被持有! 捕获窗口=%r (pid=%d)" % (title(info.hwndCapture), pid_of(info.hwndCapture)))
    else:
        print("鼠标捕获: 无（正常）")
else:
    print("无法获取线程信息")
