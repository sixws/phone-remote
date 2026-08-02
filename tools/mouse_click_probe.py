# -*- coding: utf-8 -*-
"""实测物理鼠标：10 秒内采样，检测物理点击是否到达系统 + 键盘是否有卡键"""
import ctypes
import time

user32 = ctypes.windll.user32
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]

def down(vk):
    return bool(user32.GetAsyncKeyState(vk) & 0x8000)

keys = {"左键": 0x01, "右键": 0x02, "中键": 0x04, "Ctrl键": 0x11, "Shift键": 0x10, "Alt键": 0x12, "Win键": 0x5B}
seen = {k: False for k in keys}

print("=== 采样开始：请在 10 秒内用物理鼠标连续点击 5~10 次 ===", flush=True)
t0 = time.time()
while time.time() - t0 < 10:
    for name, vk in keys.items():
        if down(vk):
            seen[name] = True
    time.sleep(0.02)

print("--- 检测结果 ---")
print("物理鼠标点击已到达系统:", "左键" if seen["左键"] else "（没检测到左键按下！）")
for name in ["右键", "中键"]:
    print("   " + name + ":", "检测到" if seen[name] else "未检测到")
stuck = [k for k in ["Ctrl键", "Shift键", "Alt键", "Win键"] if seen[k]]
print("键盘卡键:", stuck if stuck else "无（Ctrl/Shift/Alt/Win 均正常）")
