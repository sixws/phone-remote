using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace PhoneRemoteApp;

/// <summary>触控板自定义 Handler：使用接管触摸分发的原生视图，替代 MAUI 手势</summary>
public class TouchpadViewHandler : BorderHandler
{
    private GestureTracker? _tracker;

    protected override ContentViewGroup CreatePlatformView()
    {
        var context = MauiContext?.Context ?? throw new InvalidOperationException("MauiContext 不可用");
        _tracker = new GestureTracker((TouchpadView)VirtualView);
        return new TouchpadGroup(context, _tracker);
    }

    protected override void DisconnectHandler(ContentViewGroup platformView)
    {
        base.DisconnectHandler(platformView!);
    }
}
