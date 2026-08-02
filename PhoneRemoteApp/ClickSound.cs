namespace PhoneRemoteApp;

/// <summary>手机端鼠标点击声（Android 用 SoundPool 低延迟播放，重复点击不卡顿）</summary>
public static class ClickSound
{
#if ANDROID
    private static Android.Media.SoundPool? _pool;
    private static int _soundId = -1;
    private static bool _inited;

    private static void EnsureInit()
    {
        if (_inited) return;
        _inited = true;
        try
        {
            var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
            var afd = context.Assets?.OpenFd("click.wav");
            if (afd is null) return;
#pragma warning disable CS8602 // Android 绑定库的空值标注噪音
            var pool = new Android.Media.SoundPool.Builder().SetMaxStreams(4).Build();
            _pool = pool;
            _soundId = pool.Load(afd, 1);
#pragma warning restore CS8602
            afd.Close();
        }
        catch
        {
            // 音效加载失败不影响使用
        }
    }
#endif

    public static void Play()
    {
        if (!AppState.SoundOn) return;
#if ANDROID
        EnsureInit();
        try
        {
            if (_soundId >= 0)
            {
                _pool?.Play(_soundId, 0.55f, 0.55f, 1, 0, 1f);
            }
        }
        catch
        {
            // 播放失败忽略
        }
#endif
    }
}
