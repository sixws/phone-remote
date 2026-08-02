namespace PhoneRemoteServer;

/// <summary>参数校验与换算规则（纯逻辑，可单元测试）</summary>
public static class Rules
{
    public const int MaxMoveDelta = 200;      // 单次相对移动上限（像素）
    public const int MaxScrollDelta = 10000;  // 单次滚轮量上限
    public const int MaxTextLength = 5000;    // 单次注入文字长度上限
    public const int MaxBackspaces = 500;     // 单次退格数量上限

    public static (int Dx, int Dy) NormalizeMove(int? dx, int? dy) => (
        Math.Clamp(dx ?? 0, -MaxMoveDelta, MaxMoveDelta),
        Math.Clamp(dy ?? 0, -MaxMoveDelta, MaxMoveDelta));

    public static int NormalizeScroll(int? delta) =>
        Math.Clamp(delta ?? 0, -MaxScrollDelta, MaxScrollDelta);

    public static int NormalizeBackspaces(int? count) =>
        Math.Clamp(count ?? 0, 0, MaxBackspaces);

    public static bool IsValidButton(string? b) => b is "left" or "middle" or "right";

    public static bool IsValidAction(string? a) => a is "down" or "up" or "click" or "dblclick";
}
