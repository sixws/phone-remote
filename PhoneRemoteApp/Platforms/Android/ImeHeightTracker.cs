using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;

namespace PhoneRemoteApp;

/// <summary>
/// 监听安卓输入法（IME）的真实高度：键盘弹出时键盘预留区自动对准，
/// 与具体输入法（豆包/Gboard/搜狗等）和手机型号无关，换手机也不用改。
/// </summary>
public static class ImeHeightTracker
{
    private static bool _attached;

    /// <summary>输入法高度变化（单位 dp；0 = 键盘收起）</summary>
    public static event Action<int>? ImeHeightChanged;

    public static void Attach()
    {
        if (_attached) return;
        var activity = Platform.CurrentActivity;
        var decor = activity?.Window?.DecorView;
        if (decor is null) return;

        _attached = true;
        var density = decor.Resources?.DisplayMetrics?.Density ?? 1f;

        ViewCompat.SetOnApplyWindowInsetsListener(decor, new InsetsListener(density, imeDp =>
        {
            MainThread.BeginInvokeOnMainThread(() => ImeHeightChanged?.Invoke(imeDp));
        }));
    }

    private sealed class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly float _density;
        private readonly Action<int> _onImeChanged;

        public InsetsListener(float density, Action<int> onImeChanged)
        {
            _density = density;
            _onImeChanged = onImeChanged;
        }

        public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View v, WindowInsetsCompat insets)
        {
            var imePx = insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom;
            var imeDp = (int)(imePx / _density);
            _onImeChanged(imeDp);
            return insets; // 不消费，保持系统原有分发
        }
    }
}
