using FluentAssertions;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// シーンが切り替わったときに、開いているモーダル・ポップアップと、フォーカスなどの状態が残らないことを検証します。
/// </summary>
public class SceneChangeTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Button _background;
    private int _backgroundClicks;

    public SceneChangeTests()
    {
        _background = new Button("background").Location(10, 10).Size(100, 30);
        _background.Clicked += () => _backgroundClicks++;
        _h.App.GlobalForeground.Add(_background);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    private static Modal CreateModal()
    {
        var modal = new Modal((300, 200));
        modal.Size = (800, 600);
        return modal;
    }

    [Fact]
    public void LoadScene_ShouldCloseOpenModal_AndRestoreInput()
    {
        var modal = CreateModal();
        modal.Show(_h.UI, _h.App.GlobalForeground);
        _h.Frame(2);
        _h.UI.CurrentModal.Should().BeSameAs(modal);

        _h.App.LoadScene<UITestScene>();
        _h.Frame(2);

        _h.UI.CurrentModal.Should().BeNull();
        modal.Parent.Should().BeNull();
        _h.Click(50, 20);
        _backgroundClicks.Should().Be(1);
    }

    [Fact]
    public void LoadScene_ShouldCloseAModalInTheOldScenesRoot()
    {
        // 最初のシーンを読み込み、その Root にモーダルを置く (デモでよくある形)
        _h.App.LoadScene<UITestScene>();
        var modal = CreateModal();
        modal.Show(_h.UI, _h.App.Root!);
        _h.Frame(2);
        _h.UI.CurrentModal.Should().BeSameAs(modal);

        _h.App.LoadScene<UITestScene>();
        _h.Frame(2);

        _h.UI.CurrentModal.Should().BeNull();
        _h.Click(50, 20);
        _backgroundClicks.Should().Be(1);
    }

    [Fact]
    public void LoadScene_ShouldClosePopupThatLivesInTheGlobalForeground()
    {
        var popup = new Popup();
        popup.Add(new Button("item").Size(100, 30));
        popup.ShowAt(_h.UI, (300, 200));
        _h.Frame(2);
        popup.IsOpen.Should().BeTrue();

        _h.App.LoadScene<UITestScene>();
        _h.Frame(2);

        popup.IsOpen.Should().BeFalse();
        popup.Parent.Should().BeNull();
    }

    [Fact]
    public void LoadScene_ShouldCloseAPopupAnchoredInTheOldScenesRoot()
    {
        _h.App.LoadScene<UITestScene>();
        var anchor = new Button("anchor").Location(300, 100).Size(100, 30);
        _h.App.Root!.Add(anchor);
        var popup = new Popup();
        popup.Add(new Button("item").Size(100, 30));
        popup.Show(_h.UI, anchor);
        _h.Frame(2);
        popup.IsOpen.Should().BeTrue();

        _h.App.LoadScene<UITestScene>();
        _h.Frame(2);

        popup.IsOpen.Should().BeFalse();
        popup.Parent.Should().BeNull();
        _h.UI.FocusedElement.Should().BeNull();
    }

    [Fact]
    public void LoadScene_ShouldNotRaiseClosed_SoHandlersCannotNestTheSceneChange()
    {
        var modalClosed = 0;
        var popupClosed = 0;
        var modal = CreateModal();
        modal.Closed += () => modalClosed++;
        modal.Show(_h.UI, _h.App.GlobalForeground);
        var popup = new Popup();
        popup.Add(new Button("item").Size(100, 30));
        popup.Closed += () => popupClosed++;
        popup.ShowAt(_h.UI, (300, 200));
        _h.Frame(2);

        _h.App.LoadScene<UITestScene>();

        modalClosed.Should().Be(0);
        popupClosed.Should().Be(0);
        modal.Parent.Should().BeNull();
        popup.Parent.Should().BeNull();
    }

    [Fact]
    public void ClosedHandlerThatLoadsAScene_ShouldNotStartTheNewSceneTwice()
    {
        UITestScene.StartCount = 0;
        var modal = CreateModal();
        modal.Closed += () => _h.App.LoadScene<UITestScene>();
        modal.Show(_h.UI, _h.App.GlobalForeground);
        _h.Frame(2);

        _h.App.LoadScene<UITestScene>();

        UITestScene.StartCount.Should().Be(1);
    }

    [Fact]
    public void LoadScene_ShouldClearFocusAndHover()
    {
        _h.UI.SetFocus(_background);
        _h.MoveMouse(50, 20);
        _background.IsHovered.Should().BeTrue();

        _h.App.LoadScene<UITestScene>();

        _h.UI.FocusedElement.Should().BeNull();
        _h.UI.HoveredElement.Should().BeNull();
    }
}
