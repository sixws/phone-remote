using PhoneRemoteServer;

namespace PhoneRemoteServer.Tests;

public class RulesTests
{
    [Theory]
    [InlineData(null, null, 0, 0)]
    [InlineData(10, -20, 10, -20)]
    [InlineData(9999, -9999, 200, -200)]   // 超限钳制
    [InlineData(-9999, 9999, -200, 200)]
    public void NormalizeMove_钳制到安全范围(int? dx, int? dy, int wantX, int wantY)
    {
        var (x, y) = Rules.NormalizeMove(dx, dy);
        Assert.Equal(wantX, x);
        Assert.Equal(wantY, y);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(120, 120)]
    [InlineData(999999, 10000)]
    [InlineData(-999999, -10000)]
    public void NormalizeScroll_钳制到安全范围(int? delta, int want)
    {
        Assert.Equal(want, Rules.NormalizeScroll(delta));
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(3, 3)]
    [InlineData(9999, 500)]   // 超上限钳制
    [InlineData(-5, 0)]       // 负数视为 0
    public void NormalizeBackspaces_钳制到安全范围(int? count, int want)
    {
        Assert.Equal(want, Rules.NormalizeBackspaces(count));
    }

    [Theory]
    [InlineData("left", true)]
    [InlineData("middle", true)]
    [InlineData("right", true)]
    [InlineData("LEFT", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidButton_只接受小写三键(string? b, bool want)
    {
        Assert.Equal(want, Rules.IsValidButton(b));
    }

    [Theory]
    [InlineData("down", true)]
    [InlineData("up", true)]
    [InlineData("click", true)]
    [InlineData("dblclick", true)]
    [InlineData("press", false)]
    [InlineData(null, false)]
    public void IsValidAction_只接受四种动作(string? a, bool want)
    {
        Assert.Equal(want, Rules.IsValidAction(a));
    }
}
