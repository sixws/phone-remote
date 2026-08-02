using Android.OS;
using Android.Views;
using Microsoft.Maui.Graphics;

namespace PhoneRemoteApp;

/// <summary>
/// 触控板原生手势跟踪（Android）：
/// 单指滑动=移动光标，轻点=左键，快速连点=双击，长按(≥0.5秒不动)=右键，双指滑动=滚轮。
/// 全部在原生触摸层处理，不经过 MAUI 手势框架，稳定可靠。
/// </summary>
public class GestureTracker : Java.Lang.Object, Android.Views.View.IOnTouchListener
{
    private const long LongPressMs = 500;    // 长按判定
    private const long TapWindowMs = 280;    // 双击判定窗口
    private const float SlopDp = 16f;        // 位移超过此值视为"动了"（dp）

    private readonly TouchpadView _view;
    private readonly Handler _handler = new(Looper.MainLooper!);
    private readonly float _density;

    private bool _down;
    private float _downX;
    private float _downY;
    private float _lastX;
    private float _lastY;
    private bool _moved;
    private bool _longPressFired;
    private long _lastTapTime;
    private bool _pendingTap;
    private bool _twoFinger;
    private float _twoLastY;

    public GestureTracker(TouchpadView view)
    {
        _view = view;
        _density = view.Handler?.MauiContext?.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
    }

    public bool OnTouch(Android.Views.View? v, Android.Views.MotionEvent? e)
    {
        if (v is null || e is null) return true; // 全部消费，避免与输入框抢事件

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                _down = true;
                _moved = false;
                _longPressFired = false;
                _twoFinger = false;
                _downX = e.GetX();
                _downY = e.GetY();
                _lastX = _downX;
                _lastY = _downY;
                _handler.RemoveCallbacksAndMessages(null);
             //   _handler.PostDelayed(CheckLongPress, LongPressMs);
                break;
                 
            case MotionEventActions.Move:
                if (!_down) break;
                if (e.PointerCount == 2)
                {
                    // 双指滑动 → 滚轮
                    if (!_twoFinger)
                    {
                        _twoFinger = true;
                        _twoLastY = e.GetY(1);
                    }
                    var dy = e.GetY(1) - _twoLastY;
                    _twoLastY = e.GetY(1);
                    if (Math.Abs(dy) > 0.5f)
                    {
                        FireScroll((int)(-dy * 12)); // 双指下滑 → 向下滚
                    }
                    _moved = true; // 双指不算长按
                }
                else if (e.PointerCount == 1)
                {
                    var x = e.GetX();
                    var y = e.GetY();
                    var dx = x - _lastX;
                    var dy = y - _lastY;
                    _lastX = x;
                    _lastY = y;
                    if (Math.Abs(x - _downX) + Math.Abs(y - _downY) > SlopDp * _density)
                    {
                        _moved = true; // 动了 → 取消长按候选
                    }
                    if (dx != 0 || dy != 0)
                    {
                        FireMove(dx, dy);
                    }
                }
                break;

            case MotionEventActions.PointerDown:
                _twoFinger = true;
                _twoLastY = e.GetY(e.ActionIndex);
                break;

            case MotionEventActions.PointerUp:
                _twoFinger = false;
                break;

            case MotionEventActions.Up:
                _handler.RemoveCallbacksAndMessages(null);
                if (_twoFinger)
                {
                    _twoFinger = false;
                    break;
                }
                if (_down && !_moved && !_longPressFired)
                {
                    FireTap();
                }
                _down = false;
                break;

            case MotionEventActions.Cancel:
                _handler.RemoveCallbacksAndMessages(null);
                _down = false;
                _twoFinger = false;
                break;
        }
        return true;
    }

    // ---------- 长按 → 右键 ----------

    private void CheckLongPress()
    {
        if (!_down || _moved || _twoFinger) return;
        _longPressFired = true;
        ClickSound.Play();
        _view.RaiseClick("right", "click");
    }

    // ---------- 轻点 → 单击/双击 ----------

    private void FireTap()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_pendingTap && now - _lastTapTime <= TapWindowMs)
        {
            // 第二击 → 双击
            _pendingTap = false;
            ClickSound.Play();
            _view.RaiseClick("left", "dblclick");
            return;
        }
        // 第一击：稍等 280ms 确认不是双击，再发单击
        _pendingTap = true;
        _lastTapTime = now;
        _handler.PostDelayed(() =>
        {
            if (!_pendingTap) return;
            _pendingTap = false;
            ClickSound.Play();
            _view.RaiseClick("left", "click");
        }, TapWindowMs);
    }

    private void FireMove(float dx, float dy) =>
        _view.RaiseMove(dx, dy);

    private void FireScroll(int delta) =>
        _view.RaiseScroll(delta);
}
