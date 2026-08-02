"""打字注入探测窗：接收系统键盘输入并写入日志（验证 SendInput 注入用）"""
import threading
import time
import tkinter as tk

LOG = r"C:\Users\Administrator\.zcode\workspace\default\phone-remote\tools\typing_log.txt"
collected = []

root = tk.Tk()
root.title("Typing Probe")
entry = tk.Entry(root, font=("Arial", 16), width=40)
entry.pack(padx=20, pady=20)
entry.focus_force()  # 强制获取键盘焦点


def on_key(event):
    if event.char:
        collected.append(event.char)
    else:
        collected.append(f"<{event.keysym}>")  # 回车等无字符键记录键名
    with open(LOG, "w", encoding="utf-8") as f:
        f.write("".join(collected))


entry.bind("<Key>", on_key)


def close_later():
    time.sleep(15)
    root.destroy()


threading.Thread(target=close_later, daemon=True).start()
root.mainloop()
