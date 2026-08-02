using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using PhoneRemoteServer;

// 控制台 UTF-8 输出（中文提示）
Console.OutputEncoding = Encoding.UTF8;

const string TestPageHtml = """
<!doctype html>
<html lang="zh">
<head>
<meta charset="utf-8"><title>打字注入测试</title>
</head>
<body style="font-family:system-ui;padding:40px;background:#0f1420;color:#e8ecf4">
<h2>⌨️ 打字注入测试页</h2>
<p>点击下方输入框使其聚焦，然后从手机发送文字，文字应出现在这里：</p>
<input id="t" autofocus placeholder="聚焦后接收手机文字"
       style="width:90%;font-size:22px;padding:12px;margin-top:12px">
<script>document.getElementById('t').focus();</script>
</body>
</html>
""";

var lanIp = GetLanIp();
BeaconService.Start(); // UDP 广播，手机 App 自动发现
ButtonState.ReleaseAllOnStartup(); // 修复上次会话可能遗留的卡键
ButtonState.Start(); // 按键看门狗（防卡键）

Console.WriteLine("======================================================");
Console.WriteLine("  📱 手机遥控器服务已启动");
Console.WriteLine("  手机与电脑在同一 WiFi 时，打开 App 自动连接");
Console.WriteLine($"  服务地址： http://{lanIp}:8766");
Console.WriteLine("  按 Ctrl+C 退出");
Console.WriteLine("======================================================");

// 开机自启（隐藏窗口）时看不到输出，写日志便于排查
try
{
    var log = Path.Combine(AppContext.BaseDirectory, "server.log");
    File.AppendAllText(log, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 服务启动 IP={lanIp} 端口=8766{Environment.NewLine}");
}
catch
{
    // 日志失败不影响服务
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8766");
var app = builder.Build();

// ---------- API ----------

app.MapGet("/api/ping", () => Results.Ok(new { ok = true }));

// 读取当前光标坐标（也用于自检）
app.MapGet("/api/cursor", () =>
{
    var (x, y) = MouseControl.GetCursor();
    return Results.Ok(new { x, y });
});

app.MapPost("/api/mouse", (MouseCommand cmd) =>
{
    switch (cmd.Type)
    {
        case "move":
            var (dx, dy) = Rules.NormalizeMove(cmd.Dx, cmd.Dy);
            MouseControl.MoveRelative(dx, dy);
            return Results.Ok(new { ok = true });

        case "button":
            if (!Rules.IsValidButton(cmd.Button) || !Rules.IsValidAction(cmd.Action))
            {
                return Results.BadRequest(new { error = "无效的 button/action，应为 left/middle/right 与 down/up/click/dblclick" });
            }
            if (cmd.Action == "down")
            {
                ButtonState.Down(cmd.Button!);
            }
            else if (cmd.Action == "up")
            {
                ButtonState.Up(cmd.Button!);
            }
            MouseControl.Button(cmd.Button!, cmd.Action!);
            if (cmd.Action is "down" or "click" or "dblclick")
            {
                ClickSound.Play(); // 电脑端鼠标点击声
            }
            return Results.Ok(new { ok = true });

        case "scroll":
            MouseControl.Scroll(Rules.NormalizeScroll(cmd.Delta));
            return Results.Ok(new { ok = true });

        default:
            return Results.BadRequest(new { error = $"未知命令类型：{cmd.Type}" });
    }
});

// 实时镜像打字：先退格（删字同步）→ 再输入文字 → 可选回车
app.MapPost("/api/key", (KeyCommand cmd) =>
{
    var bs = Rules.NormalizeBackspaces(cmd.Backspaces);
    if (bs > 0)
    {
        KeyboardControl.Backspace(bs);
    }
    if (cmd.Enter == true)
    {
        KeyboardControl.Enter();
    }
    var text = cmd.Text ?? string.Empty;
    if (text.Length > Rules.MaxTextLength)
    {
        return Results.BadRequest(new { error = $"text 过长（上限 {Rules.MaxTextLength} 字符）" });
    }
    if (text.Length > 0)
    {
        // 终极方案：一律剪贴板粘贴 —— 微信无论长短都吞逐字注入，粘贴 100% 不丢字
        KeyboardControl.TypeByClipboard(text);
    }
    return Results.Ok(new { ok = true, chars = text.Length, backspaces = bs, enter = cmd.Enter == true });
});

// 打字注入测试页（输入框自动聚焦，便于自检）
app.MapGet("/", () => Results.Content(TestPageHtml, "text/html; charset=utf-8"));

// 一键恢复：重启资源管理器，解除任务栏吞掉的鼠标捕获（Win11 双击任务栏 bug 的应急恢复）
app.MapPost("/api/recover", () =>
{
    try
    {
        Process.Start(new ProcessStartInfo("taskkill", "/F /IM explorer.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        });
        Thread.Sleep(800);
        Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
});

// ---------- 辅助 ----------

static string GetLanIp()
{
    try
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect("8.8.8.8", 65530); // UDP Connect 不发送数据，仅取本机出口 IP
        if (socket.LocalEndPoint is IPEndPoint ep)
        {
            return ep.Address.ToString();
        }
    }
    catch
    {
        // 忽略，走备用方案
    }
    return NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
        ?.Address.ToString() ?? "127.0.0.1";
}

app.Run();
