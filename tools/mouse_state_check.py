# -*- coding: utf-8 -*-
"""诊断：检查系统层面鼠标左键是否被判定为"一直按下"（卡键状态）"""
import ctypes
import time

user32 = ctypes.windll.user32
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]
user32.GetAsyncKeyState.restype = ctypes.c_short

VK_LBUTTON = 0x01
VK_RBUTTON = 0x02
VK_MBUTTON = 0x04


def state(vk):
    return bool(user32.GetAsyncKeyState(vk) & 0x8000)  # 最高位 = 当前是否按下


print("=== 连续采样 6 次（请勿碰鼠标）===")
for i in range(6):
    print("左键:%s 右键:%s 中键:%s" % (
        "按下" if state(VK_LBUTTON) else "松开",
        "按下" if state(VK_RBUTTON) else "松开",
        "按下" if state(VK_MBUTTON) else "松开",
    ))
    time.sleep(0.3)

# 发一个完整的"点击"（按下+抬起），用于解除可能存在的单击锁定类状态
print("=== 发送一次完整点击（按下+抬起）===")
user32.mouse_event(0x0002, 0, 0, 0, 0)  # LEFT down
time.sleep(0.06)
user32.mouse_event(0x0004, 0, 0, 0, 0)  # LEFT up
time.sleep(0.3)
print("左键:", "按下" if state(VK_LBUTTON) else "松开")
