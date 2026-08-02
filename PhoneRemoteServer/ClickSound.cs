using System.Runtime.InteropServices;

namespace PhoneRemoteServer;

/// <summary>电脑端鼠标点击声：保证每次点击都有"咔哒"反馈</summary>
public static class ClickSound
{
    private static readonly string WavPath = Path.Combine(Path.GetTempPath(), "phoneremote_click.wav");
    private static readonly bool Ready;

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    private const uint SND_FILENAME = 0x00020000;
    private const uint SND_ASYNC = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;

    static ClickSound()
    {
        try
        {
            GenerateWav();
            Ready = File.Exists(WavPath);
        }
        catch
        {
            Ready = false;
        }
    }

    public static void Play()
    {
        if (!Ready) return;
        PlaySound(WavPath, IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT);
    }

    /// <summary>生成约 38ms 清脆的鼠标微动"咔哒"声（瞬态冲击 + 2.9kHz 簧片音 + 外壳共鸣 + 高频脆尾）</summary>
    private static void GenerateWav()
    {
        const int sampleRate = 44100;
        const double duration = 0.038;
        var count = (int)(sampleRate * duration);
        var rnd = new Random(20260802);

        var samples = new double[count];
        for (var i = 0; i < count; i++)
        {
            var t = i / (double)sampleRate;

            // 1) 瞬态冲击：1.5ms 宽带噪声明（开关接触的"啪"）
            var snap = t < 0.0015 ? (rnd.NextDouble() * 2 - 1) * (1 - t / 0.0015) * 0.9 : 0.0;

            // 2) 主体"嘀"：2.9kHz 快速衰减（簧片振动）
            var tick = Math.Sin(2 * Math.PI * 2900 * t) * Math.Exp(-t / 0.0045) * 0.85;

            // 3) 外壳共鸣：950Hz 稍慢（"嗒"的体感）
            var body = Math.Sin(2 * Math.PI * 950 * t) * Math.Exp(-t / 0.010) * 0.40;

            // 4) 清脆尾：6.4kHz 极短（点击的"脆"感）
            var spark = Math.Sin(2 * Math.PI * 6400 * t) * Math.Exp(-t / 0.0025) * 0.35;

            var s = snap + tick + body + spark;
            if (t < 0.0005) s *= t / 0.0005;   // 0.5ms 淡入防爆音
            samples[i] = s;
        }

        // 归一化到 0.75 峰值
        var peak = 1e-9;
        foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
        var scale = 0.75 / peak;

        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms))
        {
            w.Write("RIFF"u8);
            w.Write(36 + count * 2);
            w.Write("WAVE"u8);
            w.Write("fmt "u8);
            w.Write(16);
            w.Write((short)1);        // PCM
            w.Write((short)1);        // 单声道
            w.Write(sampleRate);
            w.Write(sampleRate * 2);  // 字节率
            w.Write((short)2);        // 块对齐
            w.Write((short)16);       // 位深
            w.Write("data"u8);
            w.Write(count * 2);

            foreach (var s in samples)
            {
                var v = (short)Math.Clamp((int)(s * scale * 32767), short.MinValue, short.MaxValue);
                w.Write(v);
            }
        }
        File.WriteAllBytes(WavPath, ms.ToArray());
    }
}
