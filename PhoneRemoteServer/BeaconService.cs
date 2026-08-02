using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PhoneRemoteServer;

/// <summary>UDP 广播：让手机 App 在同一 WiFi 下自动发现本服务（零配置）</summary>
public static class BeaconService
{
    public const int Port = 8767;
    private const string BeaconPayload = "PHONEREMOTE-BEACON";

    private static UdpClient? _udp;
    private static Timer? _timer;

    public static void Start()
    {
        if (_timer is not null) return;
        _udp = new UdpClient();
        _udp.EnableBroadcast = true;
        var payload = Encoding.UTF8.GetBytes(BeaconPayload);
        _timer = new Timer(_ =>
        {
            try
            {
                _udp?.Send(payload, payload.Length, new IPEndPoint(IPAddress.Broadcast, Port));
            }
            catch
            {
                // 广播失败不影响主服务
            }
        }, null, 0, 1000);
    }
}
