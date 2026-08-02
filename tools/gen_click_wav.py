# -*- coding: utf-8 -*-
"""生成手机端点击音效 click.wav：清脆的鼠标微动"咔哒"声
配方：1.5ms 宽带瞬态冲击 + 2.9kHz 簧片"嘀" + 950Hz 外壳共鸣 + 6.4kHz 清脆尾
（与 PhoneRemoteServer/ClickSound.cs 的合成公式一致）
"""
import math
import random
import struct
import wave

path = r"C:\Users\Administrator\.zcode\workspace\default\phone-remote\PhoneRemoteApp\Resources\Raw\click.wav"
sr = 44100
dur = 0.038
n = int(sr * dur)
rnd = random.Random(20260802)

samples = []
for i in range(n):
    t = i / sr
    # 1) 瞬态冲击：1.5ms 宽带噪声明（开关接触的"啪"）
    if t < 0.0015:
        snap = (rnd.random() * 2 - 1) * (1 - t / 0.0015) * 0.9
    else:
        snap = 0.0
    # 2) 主体"嘀"：2.9kHz 快速衰减（簧片振动）
    tick = math.sin(2 * math.pi * 2900 * t) * math.exp(-t / 0.0045) * 0.85
    # 3) 外壳共鸣：950Hz 稍慢（"嗒"的体感）
    body = math.sin(2 * math.pi * 950 * t) * math.exp(-t / 0.010) * 0.40
    # 4) 清脆尾：6.4kHz 极短（点击的"脆"感）
    spark = math.sin(2 * math.pi * 6400 * t) * math.exp(-t / 0.0025) * 0.35
    s = snap + tick + body + spark
    if t < 0.0005:  # 0.5ms 淡入，防爆音
        s *= t / 0.0005
    samples.append(s)

# 归一化到 0.75 峰值
peak = max(abs(s) for s in samples) or 1.0
scale = 0.75 / peak

frames = []
for s in samples:
    v = int(max(-1.0, min(1.0, s * scale)) * 32767)
    frames.append(struct.pack("<h", v))

with wave.open(path, "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(sr)
    w.writeframes(b"".join(frames))
print("已生成:", path, "时长 %.0fms" % (dur * 1000))
