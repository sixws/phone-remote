using System.Runtime.InteropServices;

namespace PhoneRemoteServer;

/// <summary>通过 SendInput 的 Unicode 注入向当前聚焦窗口打字（支持中文等任意字符）</summary>
public static class KeyboardControl
{
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const ushort VK_BACK = 0x08;
    private const ushort VK_RETURN = 0x0D;

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
        foreach (var ch in text)
        {
            if (char.IsSurrogate(ch)) continue; // 代理对无法单字符注入，跳过

            var inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = KEYEVENTF_UNICODE };
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP };
            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    /// <summary>发送 count 个退格键（手机删字时电脑同步删除）</summary>
    public static void Backspace(int count)
    {
        for (var i = 0; i < count; i++)
        {
            SendVk(VK_BACK);
        }
    }

    /// <summary>发送回车键（手机按回车时电脑同步回车）</summary>
    public static void Enter() => SendVk(VK_RETURN);

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
