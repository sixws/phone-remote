using System.Runtime.InteropServices;

namespace PhoneRemoteServer;

/// <summary>通过 SendInput 控制鼠标（现代 API，Win11 任务栏兼容）</summary>
public static class MouseControl
{
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public static (int X, int Y) GetCursor()
    {
        GetCursorPos(out POINT p);
        return (p.X, p.Y);
    }

    /// <summary>相对移动（不越界安全）</summary>
    public static void MoveRelative(int dx, int dy)
    {
        var (x, y) = GetCursor();
        SetCursorPos(x + dx, y + dy);
    }

    /// <summary>
    /// 按键：button = left/middle/right，action = down/up/click/dblclick。
    /// 按下与松开之间保留 45~55ms 真实间隔（模拟物理点击），
    /// 避免 Win11 任务栏对"零间隔注入点击"处理异常导致卡住输入。
    /// </summary>
    public static void Button(string button, string action)
    {
        uint down = button switch
        {
            "left" => MOUSEEVENTF_LEFTDOWN,
            "right" => MOUSEEVENTF_RIGHTDOWN,
            "middle" => MOUSEEVENTF_MIDDLEDOWN,
            _ => 0,
        };
        uint up = button switch
        {
            "left" => MOUSEEVENTF_LEFTUP,
            "right" => MOUSEEVENTF_RIGHTUP,
            "middle" => MOUSEEVENTF_MIDDLEUP,
            _ => 0,
        };
        switch (action)
        {
            case "down":
                InputApi.Mouse(down);
                break;
            case "up":
                InputApi.Mouse(up);
                break;
            case "click":
                InputApi.Mouse(down);
                Thread.Sleep(50);
                InputApi.Mouse(up);
                break;
            case "dblclick":
                InputApi.Mouse(down);
                Thread.Sleep(45);
                InputApi.Mouse(up);
                Thread.Sleep(55);
                InputApi.Mouse(down);
                Thread.Sleep(45);
                InputApi.Mouse(up);
                break;
        }
    }

    /// <summary>滚轮：正数=向上滚，负数=向下滚（一档约 120）</summary>
    public static void Scroll(int delta) => InputApi.Mouse(MOUSEEVENTF_WHEEL, (uint)delta);
}
