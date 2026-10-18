using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

public class StackLayoutFocusTests : IDisposable
{
    private readonly PrometeApp _app;
    private readonly UIManager _uiManager;

    public StackLayoutFocusTests()
    {
        _app = PrometeApp
            .Create()
            .Use<Keyboard>()
            .Use<Mouse>()
            .Use<UIManager>()
            .BuildWithHeadless();
        _app.OnStart();
        _uiManager = _app.GetPlugin<UIManager>();
    }

    public void Dispose()
    {
        _app.OnDestroy();
    }

    [Fact]
    public void MoveFocusNext_ShouldTraverseNestedStacksInTreeOrder()
    {
        var a = new Button("a").Size(50, 20);
        var b = new Button("b").Size(50, 20);
        var c = new Button("c").Size(50, 20);
        var d = new Button("d").Size(50, 20);

        var card = new Panel { AutoSize = true };
        card.Add(new StackContainer(StackDirection.Vertical, gap: 4, padding: 4) { b, c });
        var screen = new StackContainer(StackDirection.Vertical, gap: 8)
        {
            a,
            new StackContainer(StackDirection.Horizontal, gap: 8) { card, d },
        };
        _app.GlobalForeground.Add(screen);
        _app.OnRender();
        _uiManager.OnUpdate();

        _uiManager.SetFocus(a);
        var visited = new List<UIElement> { a };
        for (var i = 0; i < 3; i++)
        {
            _uiManager.MoveFocusNext();
            visited.Add(_uiManager.FocusedElement!);
        }

        UIElement[] expected = [a, b, c, d];
        visited.Select(v => Array.IndexOf(expected, v)).Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void AutoSizePanel_ShouldWrapStackContent()
    {
        var content = new StackContainer(StackDirection.Vertical, gap: 4, padding: 4)
        {
            new Button("a").Size(50, 20),
            new Button("b").Size(50, 20),
        };
        var card = new Panel { AutoSize = true };
        card.Add(content);

        card.Size.Should().Be(content.Size);
        content.Size.Should().Be(new VectorInt(58, 52));
    }
}
