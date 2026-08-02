using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace PhoneRemoteApp;

/// <summary>触控板事件参数</summary>
public class ClickArgs : EventArgs
{
    public ClickArgs(string button, string action)
    {
        Button = button;
        Action = action;
    }

    /// <summary>left / right</summary>
    public string Button { get; }

    /// <summary>click / dblclick</summary>
    public string Action { get; }
}

/// <summary>色点插值渐变（macOS 壁纸风）：5 个色点铺满整板，距离加权无缝过渡（静态背景，不耗 CPU）</summary>
public class AuroraDrawable : IDrawable
{
    private double _t = 0;   // 固定 0 = 静态；想恢复动态漂移就加定时器推进它

    // 5 个色点的"家"（相对位置）+ 各自的漂移幅度/相位
    private static readonly float[] HomeX = { 0.20f, 0.55f, 0.84f, 0.30f, 0.75f };
    private static readonly float[] HomeY = { 0.25f, 0.16f, 0.35f, 0.76f, 0.72f };
    private static readonly float[] DriftX = { 0.07f, 0.08f, 0.06f, 0.08f, 0.07f };
    private static readonly float[] DriftY = { 0.09f, 0.07f, 0.10f, 0.08f, 0.09f };
    private static readonly float[] Phase = { 0f, 1.3f, 2.4f, 3.6f, 4.7f };

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var w = dirtyRect.Width;
        var h = dirtyRect.Height;
        if (w <= 0 || h <= 0) return;

        var cols = AppState.PaletteColors;   // 每次重画读取最新配色（设置页改动即时生效）

        var minDim = MathF.Min(w, h);
        var eps = minDim * 0.26f * (minDim * 0.26f);   // 锐度：小=色块分明，大=融合成一片

        // 色点当前位置（缓慢漂移）
        Span<float> px = stackalloc float[5];
        Span<float> py = stackalloc float[5];
        for (var i = 0; i < 5; i++)
        {
            px[i] = (HomeX[i] + (float)Math.Sin(_t * 0.22 + Phase[i]) * DriftX[i]) * w;
            py[i] = (HomeY[i] + (float)Math.Cos(_t * 0.18 + Phase[i] * 0.8) * DriftY[i]) * h;
        }

        // 逐格：距离加权平均 5 个色点 → 无缝渐变（步长 3，觉得卡就改 4）
        const float step = 3f;
        for (var gy = 0f; gy < h; gy += step)
        {
            for (var gx = 0f; gx < w; gx += step)
            {
                double sr = 0, sg = 0, sb = 0, sw = 0;
                for (var i = 0; i < 5; i++)
                {
                    var dx = gx - px[i];
                    var dy = gy - py[i];
                    var wt = 1.0 / (dx * dx + dy * dy + eps);
                    sr += cols[i].Red * wt;
                    sg += cols[i].Green * wt;
                    sb += cols[i].Blue * wt;
                    sw += wt;
                }
                var r = (float)(sr / sw);
                var g = (float)(sg / sw);
                var b = (float)(sb / sw);

                // 鲜艳度提升（饱和度 ×1.3，跟 Demo 一致）
                var gray = (r + g + b) / 3f;
                r = Math.Clamp(gray + (r - gray) * 1.3f, 0f, 1f);
                g = Math.Clamp(gray + (g - gray) * 1.3f, 0f, 1f);
                b = Math.Clamp(gray + (b - gray) * 1.3f, 0f, 1f);

                // 轻微暗角（macOS 壁纸感）
                var nx = gx / w - 0.5f;
                var ny = gy / h - 0.5f;
                var vig = Math.Clamp(1.12f - MathF.Sqrt(nx * nx + ny * ny) * 1.0f, 0.55f, 1f);

                canvas.FillColor = Color.FromRgb(r * vig, g * vig, b * vig);
                canvas.FillRectangle(gx, gy, step, step);
            }
        }
    }
 }

/// <summary>
/// 触控板：滑动=移动光标，轻点=左键，连点两次=双击，长按(≥0.5秒)=右键，双指滑动=滚轮。
/// 手势由安卓原生触摸跟踪实现（Platforms/Android/GestureTracker），背景为流动的迷幻霓虹。
/// </summary>
public class TouchpadView : Border
{
    /// <summary>相对位移（像素）</summary>
    public event EventHandler<Point>? MoveRequested;

    /// <summary>点击事件</summary>
    public event EventHandler<ClickArgs>? ClickRequested;

    /// <summary>滚轮事件（正数=向上）</summary>
    public event EventHandler<int>? ScrollRequested;

    // 供原生手势跟踪（GestureTracker）触发事件
    public void RaiseMove(double dx, double dy) => MoveRequested?.Invoke(this, new Point((float)dx, (float)dy));
    public void RaiseClick(string button, string action) => ClickRequested?.Invoke(this, new ClickArgs(button, action));
    public void RaiseScroll(int delta) => ScrollRequested?.Invoke(this, delta);

    private readonly AuroraDrawable _aurora = new();

    public TouchpadView()
    {
        BackgroundColor = Color.FromArgb("#0a0a0a");
        StrokeShape = new RoundRectangle { CornerRadius = 10 };
        Stroke = Color.FromArgb("#1f1f1f");
        StrokeThickness = 1;
        Padding = 0;

        // 静态渐变背景：只画一次（省 CPU，滑动更顺）；设置页换配色时触发重画
        var gfx = new GraphicsView { Drawable = _aurora };
        Content = gfx;
        AppState.PaletteChanged += OnPaletteChanged;
    }

    /// <summary>设置页改了配色 → 重画一次静态背景</summary>
    private void OnPaletteChanged(object? sender, EventArgs e) => (Content as GraphicsView)?.Invalidate();
}
