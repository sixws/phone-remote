using Android.Content;
using Android.Views;
using Microsoft.Maui.Platform;

namespace PhoneRemoteApp;

/// <summary>
/// 触控板平台视图：直接接管 DispatchTouchEvent，保证每一次触摸都先经过手势跟踪器，
/// 不会被 MAUI 框架或子视图拦截/覆盖（上一版用 SetOnTouchListener 会被覆盖导致失灵）。
/// </summary>
public class TouchpadGroup : ContentViewGroup
{
    private readonly GestureTracker _tracker;

    public TouchpadGroup(Context context, GestureTracker tracker) : base(context)
    {
        _tracker = tracker;
    }

    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e is not null)
        {
            _tracker.OnTouch(this, e);
            return true; // 完全消费，手势全部由我们处理
        }
        return base.DispatchTouchEvent(e);
    }
}
