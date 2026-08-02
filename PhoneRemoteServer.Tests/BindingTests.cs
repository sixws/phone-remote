using System.Text.Json;
using PhoneRemoteServer;

namespace PhoneRemoteServer.Tests;

/// <summary>验证请求体与最小 API 绑定所用 JSON 结构一致（与 Web 默认一致：不区分大小写）</summary>
public class BindingTests
{
    // 与 ASP.NET Core 最小 API 默认行为一致的序列化选项
    private static readonly JsonSerializerOptions WebLike = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void MouseCommand_移动命令可反序列化()
    {
        var cmd = JsonSerializer.Deserialize<MouseCommand>("""{"type":"move","dx":12,"dy":-5}""", WebLike);
        Assert.NotNull(cmd);
        Assert.Equal("move", cmd!.Type);
        Assert.Equal(12, cmd.Dx);
        Assert.Equal(-5, cmd.Dy);
    }

    [Fact]
    public void MouseCommand_按键命令可反序列化()
    {
        var cmd = JsonSerializer.Deserialize<MouseCommand>("""{"type":"button","button":"left","action":"click"}""", WebLike);
        Assert.Equal("button", cmd!.Type);
        Assert.Equal("left", cmd.Button);
        Assert.Equal("click", cmd.Action);
    }

    [Fact]
    public void MouseCommand_滚动命令可反序列化()
    {
        var cmd = JsonSerializer.Deserialize<MouseCommand>("""{"type":"scroll","delta":120}""", WebLike);
        Assert.Equal("scroll", cmd!.Type);
        Assert.Equal(120, cmd.Delta);
    }

    [Fact]
    public void KeyCommand_文字命令可反序列化()
    {
        var cmd = JsonSerializer.Deserialize<KeyCommand>("""{"text":"你好，世界"}""", WebLike);
        Assert.Equal("你好，世界", cmd!.Text);
        Assert.Null(cmd.Backspaces);
        Assert.Null(cmd.Enter);
    }

    [Fact]
    public void KeyCommand_退格与回车可反序列化()
    {
        var cmd = JsonSerializer.Deserialize<KeyCommand>("""{"text":"好","backspaces":3,"enter":true}""", WebLike);
        Assert.Equal("好", cmd!.Text);
        Assert.Equal(3, cmd.Backspaces);
        Assert.True(cmd.Enter);
    }
}