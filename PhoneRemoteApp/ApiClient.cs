using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PhoneRemoteApp;

/// <summary>与电脑端 PhoneRemoteServer 通信的 HTTP 客户端（免鉴权）</summary>
public class ApiClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };

    public string BaseUrl { get; set; } = "";

    public async Task<bool> PingAsync()
    {
        try
        {
            using var resp = await _http.GetAsync($"{BaseUrl}/api/ping");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> SendMouseAsync(object payload) => PostAsync("/api/mouse", payload);

    /// <summary>实时镜像打字：先退格（删字同步）→ 输入文字 → 可选回车</summary>
    public Task<bool> SendKeyAsync(string text, int backspaces = 0, bool enter = false) =>
        PostAsync("/api/key", new { text, backspaces, enter });

    /// <summary>一键恢复：重启电脑资源管理器，解除任务栏卡住的鼠标捕获</summary>
    public Task<bool> RecoverAsync() => PostAsync("/api/recover", new { });

    private async Task<bool> PostAsync(string path, object payload)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{path}") { Content = content };
            using var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
