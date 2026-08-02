namespace PhoneRemoteServer;

/// <summary>POST /api/mouse 请求体</summary>
public record MouseCommand(string Type, int? Dx, int? Dy, string? Button, string? Action, int? Delta);

/// <summary>
/// POST /api/key 请求体：
/// 先发送 backspaces 个退格键（手机删字同步），再输入 text（实时镜像），可选按 enter（回车同步）。
/// </summary>
public record KeyCommand(string? Text, int? Backspaces, bool? Enter);
