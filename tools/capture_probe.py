# -*- coding: utf-8 -*-
"""终极诊断：监控光标下实际窗口/前台/全屏覆盖层，定位点击被谁吞掉"""
import ctypes
import ctypes.wintypes as wt
import time

user32 = ctypes.windll.user32
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]
user32.GetCursorPos.argtypes = [ctypes.POINTER(wt.POINT)]
user32.WindowFromPoint.restype = ctypes.c_void_p
user32.WindowFromPoint.argtypes = [wt.POINT]
user32.GetForegroundWindow.restype = ctypes.c_void_p
user32.GetWindowThreadProcessId.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]

LOG = r"C:\Users\Administrator\.zcode\workspace\default\phone-remote\tools\capture_log.txt"


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


def class_of(h):
    buf = ctypes.create_unicode_buffer(256)
    user32.GetClassNameW(h, buf, 256)
    return buf.value


def cursor_pos():
    p = wt.POINT()
    user32.GetCursorPos(ctypes.byref(p))
    return p.x, p.y


def under_cursor():
    x, y = cursor_pos()
    h = user32.WindowFromPoint(wt.POINT(x, y))
    return h, title(h), class_of(h), pid_of(h)


def foreground():
    h = user32.GetForegroundWindow()
    return h, title(h), class_of(h), pid_of(h)


def fullscreen_windows():
    found = []

    def proc(h, l):
        if user32.IsWindowVisible(h):
            r = wt.RECT()
            user32.GetWindowRect(h, ctypes.byref(r))
            w, ht = r.right - r.left, r.bottom - r.top
            if w >= 2000 and ht >= 1200:
                found.append("%r class=%s pid=%d" % (title(h), class_of(h), pid_of(h)))
        return True

    WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
    user32.EnumWindows(WNDENUMPROC(proc), 0)
    return found


def left_down():
    return bool(user32.GetAsyncKeyState(0x01) & 0x8000)


with open(LOG, "w", encoding="utf-8") as f:
    f.write("=== 监控开始：请把鼠标移到任务栏触发失灵，保持状态勿按 Win+L ===\n")
    f.flush()
    for i in range(140):  # 70 秒
        t = time.strftime("%H:%M:%S")
        uc = under_cursor()
        fg = foreground()
        line = "[%s] 左键=%s 光标=(%d,%d) 光标下=%r/%s(pid%d) 前台=%r/%s(pid%d) 全屏窗=%s" % (
            t,
            "按下" if left_down() else "松开",
            uc[0][0] if False else cursor_pos()[0], cursor_pos()[1],
            title(uc[0]), class_of(uc[0]), pid_of(uc[0]),
            title(fg[0]), class_of(fg[0]), pid_of(fg[0]),
            fullscreen_windows(),
        )
        f.write(line + "\n")
        f.flush()
        time.sleep(0.5)
    f.write("=== 监控结束 ===\n")
