namespace PhoneRemoteApp;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        RefreshSwatches();
        SensSlider.Value = AppState.Sensitivity;
        SoundSwitch.IsToggled = AppState.SoundOn;
        DiscoveryService.ServerFound += OnServerFound;
        _ = UpdateStatusAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DiscoveryService.ServerFound -= OnServerFound;
    }

    private void OnServerFound(string ip) =>
        MainThread.BeginInvokeOnMainThread(() => _ = UpdateStatusAsync());

    private static Brush Dot(string hex) => new SolidColorBrush(Color.FromArgb(hex));

    private async Task UpdateStatusAsync()
    {
        if (AppState.Api.BaseUrl.Length == 0)
        {
            StatusDot.Fill = Dot("#999");
            StatusText.Text = "正在寻找电脑…";
            return;
        }
        var ok = await AppState.Api.PingAsync();
        AppState.Connected = ok;
        if (ok)
        {
            StatusDot.Fill = Dot("#2ecc71");
            StatusText.Text = $"已连接 {AppState.ServerIp}";
        }
        else
        {
            StatusDot.Fill = Dot("#e74c3c");
            StatusText.Text = "未连接（电脑端服务未启动？）";
        }
    }

    private async void OnRescanClicked(object? sender, EventArgs e)
    {
        StatusDot.Fill = Dot("#f1c40f");
        StatusText.Text = "正在重新扫描…";

        // 先探测已知 IP，再等待广播
        if (AppState.Api.BaseUrl.Length > 0 && await AppState.Api.PingAsync())
        {
            AppState.Connected = true;
            await UpdateStatusAsync();
            return;
        }
        StatusText.Text = "等待电脑端广播…";
        await Task.Delay(2500);
        await UpdateStatusAsync();
    }

    private void OnSensChanged(object? sender, ValueChangedEventArgs e) => AppState.Sensitivity = e.NewValue;

    private void OnSoundToggled(object? sender, ToggledEventArgs e) => AppState.SoundOn = e.Value;

    // ---------- 触控板背景配色 ----------

    private static readonly Dictionary<string, string[]> Palettes = new()
    {
        ["p0"] = new[] { "#2fe0c8", "#8a5cff", "#ffb84d", "#ff6bd6", "#4dc9ff" },   // 弥散霓虹
        ["p1"] = new[] { "#00f5ff", "#ff00e5", "#baff00", "#0072ff", "#ff9a00" },   // 赛博荧光
        ["p2"] = new[] { "#ff3d00", "#ff7a00", "#ff2d78", "#c2185b", "#7b1fa2" },   // 熔岩落日
        ["p3"] = new[] { "#00e5a0", "#00b0ff", "#5cffb0", "#40c4ff", "#a0e8ff" },   // 冰岛极光
        ["p4"] = new[] { "#ff5252", "#ffd740", "#69f0ae", "#40c4ff", "#e040fb" },   // 经典彩虹
    };

    private static readonly Random Rnd = new();

    private void OnPresetClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string key) return;

        string[] cols;
        if (key == "rand")
        {
            cols = new string[5];
            for (var i = 0; i < 5; i++) cols[i] = RandomVividHex();
        }
        else if (Palettes.TryGetValue(key, out var p))
        {
            cols = p;
        }
        else return;

        AppState.PaletteHex = cols;
        RefreshSwatches();
    }

    private static string RandomVividHex() => HsvToHex(Rnd.NextDouble() * 360, 0.85 + Rnd.NextDouble() * 0.15, 1.0);

    private static string HsvToHex(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        (double R, double G, double B) rgb = h switch
        {
            < 60 => (c, x, 0),
            < 120 => (x, c, 0),
            < 180 => (0, c, x),
            < 240 => (0, x, c),
            < 300 => (x, 0, c),
            _ => (c, 0, x),
        };
        static string Hx(double ch) => ((int)Math.Round(ch * 255)).ToString("X2");
        return $"#{Hx(rgb.R + m)}{Hx(rgb.G + m)}{Hx(rgb.B + m)}";
    }

    /// <summary>刷新当前配色的 5 个色块预览</summary>
    private void RefreshSwatches()
    {
        SwatchRow.Children.Clear();
        foreach (var hex in AppState.PaletteHex)
        {
            SwatchRow.Children.Add(new BoxView
            {
                WidthRequest = 26,
                HeightRequest = 26,
                CornerRadius = 6,
                Color = Color.FromArgb(hex),
                VerticalOptions = LayoutOptions.Center
            });
        }
    }

    /// <summary>一键恢复：重启电脑资源管理器（解除任务栏吞掉的鼠标捕获）</summary>
    private async void OnRecoverClicked(object? sender, EventArgs e)
    {
        var ok = await AppState.Api.RecoverAsync();
        StatusText.Text = ok ? "已发送恢复指令，资源管理器正在重启…" : "发送失败：检查连接";
        StatusDot.Fill = Dot(ok ? "#f1c40f" : "#e74c3c");
    }
}
