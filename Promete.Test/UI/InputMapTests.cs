using FluentAssertions;
using Promete.Input;
using Promete.UI;

namespace Promete.Test.UI;

public class InputMapTests
{
    [Fact]
    public void AddAction_ShouldCreateIdleAction()
    {
        var map = new InputMap();
        var action = map.AddAction("jump");

        action.Name.Should().Be("jump");
        action.IsPressed.Should().BeFalse();
        action.IsJustPressed.Should().BeFalse();
    }

    [Fact]
    public void GetAction_ShouldReturnSameInstance()
    {
        var map = new InputMap();
        var created = map.AddAction("attack");

        map.GetAction("attack").Should().BeSameAs(created);
    }

    [Fact]
    public void Update_ShouldPressActionWhileBoundKeyIsHeld()
    {
        var map = new InputMap();
        var action = map.AddAction("submit");
        map.BindKey("submit", KeyCode.Enter).BindKey("submit", KeyCode.Space);

        map.Update(key => key == KeyCode.Space);
        action.IsPressed.Should().BeTrue();
        action.IsJustPressed.Should().BeTrue();

        map.Update(key => key == KeyCode.Space);
        action.IsPressed.Should().BeTrue();
        action.IsJustPressed.Should().BeFalse();

        map.Update(_ => false);
        action.IsPressed.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldIgnoreUnboundKeys()
    {
        var map = new InputMap();
        var action = map.AddAction("submit");
        map.BindKey("submit", KeyCode.Enter);

        map.Update(key => key == KeyCode.A);

        action.IsPressed.Should().BeFalse();
    }
}
