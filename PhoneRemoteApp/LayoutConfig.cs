namespace PhoneRemoteApp;

/// <summary>
/// 布局尺寸集中配置：想改 UI 尺寸，只改这里，一行一个数字，不碰任何逻辑。
/// 触控板等元素若已在 XAML 里手写尺寸，则以手写为准（自适应只设下限，不覆盖手动值）。
/// </summary>
public static class LayoutConfig
{
    /// <summary>触控板最小高度（dp）：屏幕再小也不会比这更矮（默认占满剩余空间）</summary>
    public const double TouchpadMinHeight = 280;

    /// <summary>右侧滚轮条宽度（dp）</summary>
    public const double SidebarWidth = 44;

    /// <summary>三键高度（dp）</summary>
    public const double ButtonHeight = 48;

    /// <summary>键盘预留区回退比例（IME 实测失败/收起时）：屏幕高度 × 此比例</summary>
    public const double KeyboardReserveRatio = 0.42;

    /// <summary>键盘预留区上限（dp）</summary>
    public const double KeyboardReserveMax = 420;

    /// <summary>Windows 调试时键盘预留区高度（dp，窗口固定 860 高）</summary>
    public const double WindowsReserveHeight = 360;
}
