# -*- coding: utf-8 -*-
"""快速连击测试：向服务端连发 200 次点击，检测是否有失败/超时（排查点击失灵）"""
import json
import time
import urllib.request

BASE = "http://127.0.0.1:8766"
HEADERS = {"Content-Type": "application/json"}


def post(path, payload, timeout=3):
    req = urllib.request.Request(BASE + path, data=json.dumps(payload).encode("utf-8"), headers=HEADERS)
    return urllib.request.urlopen(req, timeout=timeout)


def get_cursor():
    with urllib.request.urlopen(BASE + "/api/cursor", timeout=3) as r:
        return json.loads(r.read())


def move_to(x, y):
    for _ in range(30):
        cx, cy = get_cursor()["x"], get_cursor()["y"]
        dx = max(-200, min(200, x - cx))
        dy = max(-200, min(200, y - cy))
        if abs(dx) <= 3 and abs(dy) <= 3:
            return
        post("/api/mouse", {"type": "move", "dx": dx, "dy": dy})
        time.sleep(0.01)


def main():
    # 先把光标挪到角落，避免误点屏幕上的东西
    move_to(20, 20)
    print("光标已移到 (20,20)，开始连击测试...")

    fails = 0
    times = []
    for i in range(200):
        t0 = time.time()
        try:
            post("/api/mouse", {"type": "button", "button": "left", "action": "click"}, timeout=3)
        except Exception as e:
            fails += 1
            if fails <= 5:
                print("  第 %d 次失败: %s" % (i, e))
        times.append(time.time() - t0)

    avg = sum(times) / len(times)
    print("结果: 成功 %d / 200, 失败 %d" % (200 - fails, fails))
    print("平均耗时: %.1fms, 最慢: %.1fms" % (avg * 1000, max(times) * 1000))


if __name__ == "__main__":
    main()
