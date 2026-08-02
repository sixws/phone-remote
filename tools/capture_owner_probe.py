# -*- coding: utf-8 -*-
"""捕获归属探针：监控前台线程是否持有鼠标捕获 + 找 '不支持 DisplayPort' 弹窗进程"""
import ctypes
import ctypes.wintypes as wt
import time

user32 = ctypes.windll.user32
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]
user32.GetForegroundWindow.restype = ctypes.c_void_p
user32.GetWindowThreadProcessId.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
user32.GetGUIThreadInfo.argtypes = [ctypes.c_uint, ctypes.c_void_p]

LOG = r"C:\Users\Administrator\.zcode\workspace\default\phone-remote\tools\capture_owner_log.txt"


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


def capture_owner():
    """返回（是否被捕获, 捕获窗口, 捕获窗口标题）"""
    h = user32.GetForegroundWindow()
    if not h:
        return False, 0, ""
    tid = user32.GetWindowThreadProcessId(h, None)
    info = GUITHREADINFO()
    info.cbSize = ctypes.sizeof(GUITHREADINFO)
    if not user32.GetGUIThreadInfo(tid, ctypes.byref(info)):
        return False, 0, ""
    if info.hwndCapture:
        return True, info.hwndCapture, title(info.hwndCapture)
    return False, 0, ""


def find_dp_popup():
    """找到 '不支持 DisplayPort' 弹窗的进程"""
    found = []

    def proc(h, l):
        if user32.IsWindowVisible(h):
            buf = ctypes.create_unicode_buffer(256)
            user32.GetWindowTextW(h, buf, 256)
            if "DisplayPort" in buf.value:
                found.append("%r pid=%d" % (buf.value, pid_of(h)))
        return True

    WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
    user32.EnumWindows(WNDENUMPROC(proc), 0)
    return found


def left_down():
    return bool(user32.GetAsyncKeyState(0x01) & 0x8000)


with open(LOG, "w", encoding="utf-8") as f:
    f.write("=== 捕获归属监控：请用【物理鼠标】点任务栏触发失灵，保持勿按 Win+L ===\n")
    f.write("DisplayPort 弹窗进程: %s\n" % find_dp_popup())
    f.flush()
    for i in range(140):  # 70 秒
        t = time.strftime("%H:%M:%S")
        captured, cap_hwnd, cap_title = capture_owner()
        fg = user32.GetForegroundWindow()
        line = "[%s] 左键=%s 捕获=%s(%r) 前台=%r(pid%d)" % (
            t,
            "按下" if left_down() else "松开",
            "有" if captured else "无",
            cap_title,
            title(fg),
            pid_of(fg),
        )
        f.write(line + "\n")
        f.flush()
        time.sleep(0.5)
    f.write("=== 监控结束 ===\n")
