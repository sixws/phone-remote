using System.Runtime.InteropServices;
using System.Text;

namespace PhoneRemoteServer;

/// <summary>通过 SendInput 的 Unicode 注入向当前聚焦窗口打字（支持中文等任意字符）</summary>
public static class KeyboardControl
{
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const ushort VK_BACK = 0x08;
    private const ushort VK_RETURN = 0x0D;

    // 注入锁：键盘/粘贴操作全部串行，防止并发请求互相打断导致吞字
    private static readonly object InjectLock = new();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    /// <summary>把整段文字作为按键事件序列注入（跳过代理对字符，如部分 emoji）</summary>
    public static void TypeText(string text)
    {
        lock (InjectLock)
        {
            // 预热 + 字间留 3ms（正常情况下已不再走这里，保留作为兜底）
            Thread.Sleep(12);
            foreach (var ch in text)
            {
                if (char.IsSurrogate(ch)) continue; // 代理对无法单字符注入，跳过

                var inputs = new INPUT[2];
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].U.ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = KEYEVENTF_UNICODE };
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].U.ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP };
                SendInput(2, inputs, Marshal.SizeOf<INPUT>());
                Thread.Sleep(3);
            }
        }
    }

    /// <summary>终极方案：所有文字都走剪贴板粘贴 —— 微信对逐字注入/高速输入一律吞字，粘贴是唯一不丢字的方式</summary>
    public static void TypeByClipboard(string text)
    {
        if (text.Length == 0) return;
        lock (InjectLock)
        {
            SetClipboardText(text);
            Thread.Sleep(30);   // 等剪贴板就绪，目标程序能读到

            // Ctrl+V
            var inputs = new INPUT[4];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = new KEYBDINPUT { wVk = 0x11, wScan = 0, dwFlags = 0 };               // Ctrl 按下
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki = new KEYBDINPUT { wVk = 0x56, wScan = 0, dwFlags = 0 };               // V 按下
            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].U.ki = new KEYBDINPUT { wVk = 0x56, wScan = 0, dwFlags = KEYEVENTF_KEYUP }; // V 抬起
            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].U.ki = new KEYBDINPUT { wVk = 0x11, wScan = 0, dwFlags = KEYEVENTF_KEYUP }; // Ctrl 抬起
            SendInput(4, inputs, Marshal.SizeOf<INPUT>());

            Thread.Sleep(150);  // 关键：等微信把文字完全插入，再做下一步（退格/下次粘贴），否则会被吞
        }
    }

    /// <summary>Win32 剪贴板写入 Unicode 文本（控制台进程无需 STA；重试避开微信等占用剪贴板）</summary>
    private static void SetClipboardText(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (!TryOpenClipboard()) continue;
            try
            {
                EmptyClipboard();
                var bytes = Encoding.Unicode.GetBytes(text + "\0");
                var h = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
                var p = GlobalLock(h);
                Marshal.Copy(bytes, 0, p, bytes.Length);
                GlobalUnlock(h);
                SetClipboardData(CF_UNICODETEXT, h);
                CloseClipboard();
                return;
            }
            catch
            {
                CloseClipboard();
            }
        }
    }

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();
    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    private static bool TryOpenClipboard()
    {
        for (var i = 0; i < 10; i++)
        {
            if (OpenClipboard(IntPtr.Zero)) return true;
            Thread.Sleep(20);
        }
        return false;
    }

    /// <summary>发送 count 个退格键（手机删字时电脑同步删除；逐个间隔防吞）</summary>
    public static void Backspace(int count)
    {
        lock (InjectLock)
        {
            for (var i = 0; i < count; i++)
            {
                SendVk(VK_BACK);
                Thread.Sleep(8);
            }
        }
    }

    /// <summary>发送回车键（手机按回车时电脑同步回车）</summary>
    public static void Enter()
    {
        lock (InjectLock) SendVk(VK_RETURN);
    }

    private static void SendVk(ushort vk)
    {
        var inputs = new INPUT[2];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = 0 };
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].U.ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = KEYEVENTF_KEYUP };
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }
}
