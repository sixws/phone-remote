namespace PhoneRemoteApp;

public partial class MainPage : ContentPage
{
    private readonly double _scrollFactor = 24; // 侧边栏滑动 1 像素 ≈ 滚轮 24 步（120 为一档）
    private string _lastSent = "";              // 已同步到电脑的文字（用于差值计算）
    private bool _syncScheduled;

    // 移动节流：触摸事件先累积，每 16ms 合并成一次请求发送（滑动更流畅，不再每个事件一个 HTTP）
    private double _accX, _accY;
    private readonly object _moveLock = new();
    private bool _moveTimerStarted;

    public MainPage()
    {
        InitializeComponent();

        DiscoveryService.ServerFound += OnServerFound;
        DiscoveryService.Start();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        if (Platform.CurrentActivity?.Window is { } window)
        {
            window.SetSoftInputMode(Android.Views.SoftInput.AdjustNothing);
        }
        // 锁定竖屏，横过来不乱布局
        if (Platform.CurrentActivity is { } activity)
        {
            activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
        }
        // 监听输入法真实高度：键盘弹出时预留区自动对准
        ImeHeightTracker.Attach();
        ImeHeightTracker.ImeHeightChanged += OnImeHeightChanged;
#endif

        // 尺寸集中配置（想改大小 → 改 LayoutConfig.cs，一行一个数字）
        Touchpad.MinimumHeightRequest = LayoutConfig.TouchpadMinHeight;
        ScrollSidebar.WidthRequest = LayoutConfig.SidebarWidth;
        LeftBtn.HeightRequest = LayoutConfig.ButtonHeight;
        MiddleBtn.HeightRequest = LayoutConfig.ButtonHeight;
        RightBtn.HeightRequest = LayoutConfig.ButtonHeight;

        // 键盘预留区默认高度（IME 实测出键盘高度后会被覆盖）
        SetDefaultReserveHeight();

        // 移动节流定时器（只启动一次）
        if (!_moveTimerStarted)
        {
            _moveTimerStarted = true;
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), DrainMoveQueue);
        }

        // 一进来键盘自动弹出（聚焦隐藏输入）
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () => HiddenInput.Focus());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
#if ANDROID
        ImeHeightTracker.ImeHeightChanged -= OnImeHeightChanged;
#endif
    }

    /// <summary>键盘收起/未实测时的预留区高度</summary>
    private void SetDefaultReserveHeight()
    {
#if WINDOWS
        KeyboardReserve.HeightRequest = LayoutConfig.WindowsReserveHeight;
#else
        var screenDp = DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density;
        KeyboardReserve.HeightRequest = Math.Min(LayoutConfig.KeyboardReserveMax, screenDp * LayoutConfig.KeyboardReserveRatio);
#endif
    }

    /// <summary>输入法真实高度变化：键盘弹出 → 预留区对准；收起 → 回默认</summary>
    private void OnImeHeightChanged(int imeDp)
    {
        if (imeDp > 0)
        {
            KeyboardReserve.HeightRequest = imeDp;
        }
        else
        {
            SetDefaultReserveHeight();
        }
    }

    // ---------- 自动发现连接 ----------

    private void OnServerFound(string ip)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (AppState.Connected && AppState.ServerIp == ip) return;
            AppState.Api.BaseUrl = $"http://{ip}:8766";
            var ok = await AppState.Api.PingAsync();
            AppState.Connected = ok;
            AppState.ServerIp = ok ? ip : null;
        });
    }

    // ---------- 触控板 ----------

    private void OnTouchpadMove(object? sender, Point p)
    {
        var s = AppState.Sensitivity;
        lock (_moveLock)
        {
            _accX += p.X * s;
            _accY += p.Y * s;
        }
    }

    /// <summary>每 16ms 把累积位移合并成一次请求发出（请求数大幅减少，滑动更跟手）</summary>
    private bool DrainMoveQueue()
    {
        double dx, dy;
        lock (_moveLock) { dx = _accX; dy = _accY; _accX = 0; _accY = 0; }
        if (dx != 0 || dy != 0)
        {
            _ = AppState.Api.SendMouseAsync(new { type = "move", dx = (int)dx, dy = (int)dy });
        }
        return true;
    }

    private void OnTouchpadClick(object? sender, ClickArgs e)
    {
        _ = AppState.Api.SendMouseAsync(new { type = "button", button = e.Button, action = e.Action });
    }

    private void OnTouchpadScroll(object? sender, int delta)
    {
        _ = AppState.Api.SendMouseAsync(new { type = "scroll", delta });
    }
  
    // ---------- 滚动侧边栏 ----------

    private double _lastSidebarY;

    private void OnSidebarPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastSidebarY = 0;
                break;
            case GestureStatus.Running:
                // 手指下滑(TotalY 增大) = 向下滚动 = 滚轮负值
                var delta = (int)((_lastSidebarY - e.TotalY) * _scrollFactor);
                _lastSidebarY = e.TotalY;
                if (delta != 0)
                {
                    _ = AppState.Api.SendMouseAsync(new { type = "scroll", delta });
                }
                break;
        }
    }

    // ---------- 三个按键（按下/松开 = 按下/抬起，支持拖拽） ----------
    // "松开"是防卡键的关键请求：失败会自动重试，避免电脑端一直以为按键按着

    private void OnLeftPressed(object? sender, EventArgs e)
    {
        ClickSound.Play();
        _ = AppState.Api.SendMouseAsync(new { type = "button", button = "left", action = "down" });
    }

    private void OnLeftReleased(object? sender, EventArgs e) => SendUpWithRetry("left");

    private void OnMiddlePressed(object? sender, EventArgs e)
    {
        ClickSound.Play();
        _ = AppState.Api.SendMouseAsync(new { type = "button", button = "middle", action = "down" });
    }

    private void OnMiddleReleased(object? sender, EventArgs e) => SendUpWithRetry("middle");

    private void OnRightPressed(object? sender, EventArgs e)
    {
        ClickSound.Play();
        _ = AppState.Api.SendMouseAsync(new { type = "button", button = "right", action = "down" });
    }

    private void OnRightReleased(object? sender, EventArgs e) => SendUpWithRetry("right");

    /// <summary>发送"松开"，失败重试最多 3 次（防止"按下"到了、"松开"丢了导致卡键）</summary>
    private void SendUpWithRetry(string button)
    {
        _ = Task.Run(async () =>
        {
            for (var i = 0; i < 3; i++)
            {
                if (await AppState.Api.SendMouseAsync(new { type = "button", button, action = "up" })) return;
                await Task.Delay(120);
            }
        });
    }

    // ---------- 隐藏输入：实时镜像打字（无输入条，回车不收起键盘） ----------
    // 防"跳字"设计：
    // 1) 所有按键请求排队串行发送（文字差值 + 回车），杜绝乱序导致电脑端删错字
    // 2) 发送失败自动重试；只有电脑确认收到后才推进基准文字，丢了会自动补差
    // 3) 大段删除先等 200ms：IME 拼音重启瞬间文字会"清空再恢复"，避免误删电脑端整段文字

    private string _targetText = "";          // 手机端当前文字（要同步到电脑的目标）
    private bool _enterPending;               // 排队的回车（等文字同步完再发）
    private bool _loopRunning;
    private bool _deleteGuard;                // 大段删除确认（只拦一次，避免长按退格变卡）
    private readonly object _syncLock = new();
    private Task _keyChain = Task.CompletedTask;   // 按键发送队列

    private void OnHiddenTextChanged(object? sender, TextChangedEventArgs e)
    {
        var raw = HiddenInput.Text ?? "";

        // 手机键盘回车 → 多行编辑器会插入换行 → 同步回车到电脑并移除换行
        // （多行编辑器回车永远不会触发"完成"，键盘因此不会消失）
        if (raw.EndsWith("\n") || raw.EndsWith("\r"))
        {
            HiddenInput.Text = raw.TrimEnd('\r', '\n');   // 移除换行会再触发一次 TextChanged
            lock (_syncLock) _enterPending = true;
            RequestSync();
            return;
        }

        if (_syncScheduled) return;
        _syncScheduled = true;

        // 50ms 防抖：把打字过程合并成一次差值同步，保证"逐字实时"
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
        {
            _syncScheduled = false;
            var text = HiddenInput.Text ?? "";
            lock (_syncLock)
            {
                if (text == _lastSent) return;
                _targetText = text;
            }
            RequestSync();
        });
    }

    /// <summary>启动同步循环（已在循环中就复用）</summary>
    private void RequestSync()
    {
        lock (_syncLock)
        {
            if (_loopRunning) return;
            _loopRunning = true;
        }
        _ = SyncLoopAsync();
    }

    /// <summary>串行同步：差值 → 排队发送 → 成功后推进基准 → 追上目标后再发回车</summary>
    private async Task SyncLoopAsync()
    {
        while (true)
        {
            string target, sent;
            lock (_syncLock) { target = _targetText; sent = _lastSent; }

            if (target != sent)
            {
                var common = CommonPrefixLength(sent, target);
                var bs = sent.Length - common;      // 手机删了哪些字 → 电脑退格
                var add = target.Substring(common); // 新增了什么字 → 电脑输入

                // 大段纯删除：疑似 IME 拼音重启瞬间清空，先等 200ms 看是否恢复（只拦一次）
                if (bs > 3 && add.Length == 0 && !_deleteGuard)
                {
                    _deleteGuard = true;
                    await Task.Delay(200);
                    continue;   // 重新对差：恢复了就不删，真删了照发
                }
                if (!(bs > 3 && add.Length == 0)) _deleteGuard = false;

                var ok = await QueueKeySendAsync(() => SendKeyWithRetryAsync(add, bs, false));
                if (!ok)
                {
                    lock (_syncLock) _loopRunning = false;
                    return;   // 网络故障：放弃本轮，下次输入自动重新对差
                }
                lock (_syncLock) _lastSent = target;   // 只有成功才推进基准
                continue;
            }

            _deleteGuard = false;

            // 文字追平后，发送排队的回车
            bool enter;
            lock (_syncLock) { enter = _enterPending; _enterPending = false; }
            if (enter)
            {
                var ok = await QueueKeySendAsync(() => SendKeyWithRetryAsync("", 0, true));
                if (!ok)
                {
                    lock (_syncLock) _loopRunning = false;
                    return;
                }
                continue;
            }

            lock (_syncLock) _loopRunning = false;
            return;
        }
    }

    /// <summary>按键请求排队串行发送（回车与文字差值按顺序，杜绝乱序跳字）</summary>
    private Task<bool> QueueKeySendAsync(Func<Task<bool>> send)
    {
        var tcs = new TaskCompletionSource<bool>();
        _keyChain = _keyChain.ContinueWith(async _ =>
        {
            try
            {
                var ok = await send();
                tcs.TrySetResult(ok);
            }
            catch
            {
                tcs.TrySetResult(false);
            }
        }, TaskScheduler.Default);
        return tcs.Task;
    }

    /// <summary>发送一次按键请求，失败重试最多 3 次</summary>
    private async Task<bool> SendKeyWithRetryAsync(string add, int bs, bool enter)
    {
        for (var i = 0; i < 3; i++)
        {
            if (await AppState.Api.SendKeyAsync(add, bs, enter)) return true;
            await Task.Delay(80);
        }
        return false;
    }

    private static int CommonPrefixLength(string a, string b)
    {
        var n = Math.Min(a.Length, b.Length);
        var i = 0;
        while (i < n && a[i] == b[i]) i++;
        return i;
    }

    // ---------- 顶部按钮 ----------

    private void OnKeyboardClicked(object? sender, EventArgs e) => HiddenInput.Focus();

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }
}
