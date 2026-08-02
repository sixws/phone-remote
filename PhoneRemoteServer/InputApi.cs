using System.Runtime.InteropServices;

namespace PhoneRemoteServer;

/// <summary>
/// SendInput 输入注入（现代 API，Win11 兼容性优于旧 mouse_event）。
/// 鼠标与键盘共用一套结构体。
/// </summary>
internal static class InputApi
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static void Mouse(uint flags, uint mouseData = 0)
    {
        var input = new INPUT { type = INPUT_MOUSE };
        input.U.mi = new MOUSEINPUT { dwFlags = flags, mouseData = mouseData };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public static void Key(ushort vk, bool up = false)
    {
        var input = new INPUT { type = INPUT_KEYBOARD };
        input.U.ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = up ? KEYEVENTF_KEYUP : 0 };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public static void Unicode(char ch, bool up = false)
    {
        var input = new INPUT { type = INPUT_KEYBOARD };
        input.U.ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = (up ? KEYEVENTF_KEYUP : 0) | KEYEVENTF_UNICODE };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    // ---------- 结构体（x64 下 INPUT=40 字节，与 pinvoke.net 标准一致） ----------

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
