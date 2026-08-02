using System.Net.Sockets;
using System.Text;

namespace PhoneRemoteApp;

/// <summary>
/// 局域网自动发现：监听电脑端 UDP 广播（8767 端口），收到后自动连接，零配置。
/// </summary>
public static class DiscoveryService
{
    public const int BeaconPort = 8767;
    private static readonly byte[] BeaconMagic = Encoding.UTF8.GetBytes("PHONEREMOTE-BEACON");

    private static CancellationTokenSource? _cts;

    /// <summary>发现电脑端服务（参数为电脑 IP）</summary>
    public static event Action<string>? ServerFound;

    public static void Start()
    {
        if (_cts is not null) return;
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ListenLoop(_cts.Token));
    }

    public static void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private static async Task ListenLoop(CancellationToken token)
    {
        using var udp = new UdpClient(BeaconPort);
        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await udp.ReceiveAsync(token);
                if (result.Buffer.AsSpan().SequenceEqual(BeaconMagic))
                {
                    ServerFound?.Invoke(result.RemoteEndPoint.Address.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(500, token);
            }
        }
    }
}
