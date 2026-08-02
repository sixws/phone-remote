namespace PhoneRemoteApp;

/// <summary>全局共享状态（主界面与设置页互通）</summary>
public static class AppState
{
    public static ApiClient Api { get; } = new();

    /// <summary>当前连接的电脑 IP（null = 未连接）</summary>
    public static string? ServerIp { get; set; }

    public static bool Connected { get; set; }

    public static double Sensitivity
    {
        get => Preferences.Default.Get("sensitivity", 1.0);
        set => Preferences.Default.Set("sensitivity", value);
    }

    public static bool SoundOn
    {
        get => Preferences.Default.Get("sound_on", true);
        set => Preferences.Default.Set("sound_on", value);
    }

    /// <summary>触控板背景配色：5 个色点的颜色（默认熔岩落日）</summary>
    private static readonly string[] DefaultPalette = { "#ff3d00", "#ff7a00", "#ff2d78", "#c2185b", "#7b1fa2" };

    /// <summary>配色被修改时触发（触控板据此重画静态背景）</summary>
    public static event EventHandler? PaletteChanged;

    /// <summary>读取/保存 5 个色点的十六进制颜色（设置页可改，触控板每次重画读取）</summary>
    public static string[] PaletteHex
    {
        get
        {
            var list = new string[5];
            for (var i = 0; i < 5; i++)
                list[i] = Preferences.Default.Get($"palette{i}", DefaultPalette[i]);
            return list;
        }
        set
        {
            for (var i = 0; i < 5; i++)
                Preferences.Default.Set($"palette{i}", value.Length > i ? value[i] : DefaultPalette[i]);
            PaletteChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>5 个色点的 Color 数组（触控板直接取用）</summary>
    public static Color[] PaletteColors
    {
        get
        {
            var hex = PaletteHex;
            var cols = new Color[5];
            for (var i = 0; i < cols.Length; i++)
                cols[i] = Color.FromArgb(hex[i]);
            return cols;
        }
    }
}
