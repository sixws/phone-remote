namespace PhoneRemoteServer;

/// <summary>
/// 按键状态看门狗：防止"按下"请求成功但"松开"请求丢失导致的系统卡键。
/// - 重复按下同键 → 先补一次抬起
/// - 按下超过 3 秒未松开 → 自动抬起
/// - 服务启动时 → 强制抬起所有键（修复上次遗留的卡键）
/// </summary>
public static class ButtonState
{
    private static readonly Dictionary<string, DateTime> DownSince = new();
    private static readonly object Sync = new();
    private static Timer? _watchdog;

    public static void Start()
    {
        _watchdog = new Timer(_ => Sweep(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
    }

    /// <summary>服务启动时修复遗留卡键</summary>
    public static void ReleaseAllOnStartup()
    {
        foreach (var b in new[] { "left", "middle", "right" })
        {
            MouseControl.Button(b, "up");
        }
        Log("启动时已强制抬起全部按键（修复可能存在的卡键）");
    }

    public static void Down(string button)
    {
        lock (Sync)
        {
            if (DownSince.ContainsKey(button))
            {
                // 重复按下：先补一次抬起，再重新按下
                MouseControl.Button(button, "up");
                Log($"检测到重复按下 {button}，已补发抬起");
            }
            DownSince[button] = DateTime.Now;
        }
    }

    public static void Up(string button)
    {
        lock (Sync)
        {
            DownSince.Remove(button);
        }
    }

    private static void Sweep()
    {
        lock (Sync)
        {
            foreach (var (button, time) in DownSince.ToList())
            {
                if ((DateTime.Now - time).TotalSeconds <= 3) continue;
                MouseControl.Button(button, "up");
                DownSince.Remove(button);
                Log($"看门狗自动抬起 {button}（按下超过 3 秒）");
            }
        }
    }

    private static void Log(string message)
    {
        try
        {
            var log = Path.Combine(AppContext.BaseDirectory, "server.log");
            File.AppendAllText(log, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // 日志失败不影响功能
        }
    }
}
