using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class RadioGroupTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly RadioGroup _group;

    public RadioGroupTests()
    {
        _group = new RadioGroup(["低", "中", "高"]).Location(100, 100);
        _h.App.GlobalForeground.Add(_group);
        _h.Frame(3);
    }

    public void Dispose() => _h.Dispose();

    private void ClickItem(int index)
    {
        var center = _group.GetRadioItem(index).GetHitRect().Center;
        _h.Click((int)center.X, (int)center.Y);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var group = new RadioGroup();

        group.SelectedIndex.Should().Be(-1);
        group.SelectedItem.Should().BeNull();
        group.Direction.Should().Be(StackDirection.Vertical);
        group.IsFocusable.Should().BeFalse();
        group.Items.Should().BeEmpty();
    }

    [Fact]
    public void Items_ShouldCreateOneFocusableRadioItemEach()
    {
        _group.Items.Count.Should().Be(3);
        for (var i = 0; i < 3; i++)
        {
            var item = _group.GetRadioItem(i);
            item.IsFocusable.Should().BeTrue();
            item.TextContent.Should().Be(_group.Items[i]);
        }
    }

    [Fact]
    public void AddingItems_ShouldRebuildRadioItems()
    {
        _group.AddItem("最高");
        _h.Frame(2);

        _group.GetRadioItem(3).TextContent.Should().Be("最高");
    }

    [Fact]
    public void ClickingItem_ShouldSelectOnlyThatItem_AndRaiseEvent()
    {
        var raised = new List<int>();
        _group.SelectionChanged += raised.Add;

        ClickItem(1);

        _group.SelectedIndex.Should().Be(1);
        _group.SelectedItem.Should().Be("中");
        raised.Should().Equal(1);
        _group.GetRadioItem(0).IsSelected.Should().BeFalse();
        _group.GetRadioItem(1).IsSelected.Should().BeTrue();
        _group.GetRadioItem(2).IsSelected.Should().BeFalse();
    }

    [Fact]
    public void ClickingSelectedItemAgain_ShouldNotRaiseEvent()
    {
        ClickItem(1);
        var raised = 0;
        _group.SelectionChanged += _ => raised++;

        ClickItem(1);

        raised.Should().Be(0);
    }

    [Fact]
    public void Keyboard_EnterAndSpaceSelectFocusedItem()
    {
        _h.UI.SetFocus(_group.GetRadioItem(0));
        _h.PressKey(KeyCode.Enter);
        _group.SelectedIndex.Should().Be(0);

        _h.PressKey(KeyCode.Down);
        _h.UI.FocusedElement.Should().BeSameAs(_group.GetRadioItem(1));
        _group.SelectedIndex.Should().Be(0);

        _h.PressKey(KeyCode.Space);
        _group.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void SelectedIndex_ShouldBecomeMinusOne_WhenOutOfRange()
    {
        _group.SelectedIndex = 1;

        _group.SelectedIndex = 99;

        _group.SelectedIndex.Should().Be(-1);
    }

    [Fact]
    public void Vertical_ShouldStackItemsDownward()
    {
        var first = _group.GetRadioItem(0);
        var second = _group.GetRadioItem(1);

        second.Location.Y.Should().BeGreaterThan(first.Location.Y);
        second.Location.X.Should().Be(first.Location.X);
    }

    [Fact]
    public void Horizontal_ShouldStackItemsRightward_AndReactToDirectionChange()
    {
        _group.Direction = StackDirection.Horizontal;
        _h.Frame(3);

        var first = _group.GetRadioItem(0);
        var second = _group.GetRadioItem(1);
        second.Location.X.Should().BeGreaterThan(first.Location.X);
        second.Location.Y.Should().Be(first.Location.Y);
    }

    [Fact]
    public void GroupStyle_ShouldControlGap()
    {
        _group.Style = new DefaultRadioGroupStyle { VerticalGap = 40 };
        _h.Frame(3);

        var first = _group.GetRadioItem(0);
        var second = _group.GetRadioItem(1);
        (second.Location.Y - first.Location.Y).Should().Be(first.Size.Y + 40);
    }

    [Fact]
    public void GroupStyle_ShouldControlGapOfHorizontalDirectionSeparately()
    {
        _group.Direction = StackDirection.Horizontal;
        _group.Style = new DefaultRadioGroupStyle { HorizontalGap = 50 };
        _h.Frame(3);

        var first = _group.GetRadioItem(0);
        var second = _group.GetRadioItem(1);
        (second.Location.X - first.Location.X).Should().Be(first.Size.X + 50);
    }

    [Fact]
    public void ItemStyle_ShouldControlItemSize()
    {
        _group.ItemStyle = new DefaultRadioItemStyle { MarkerSize = 30, Spacing = 10 };
        _h.Frame(3);

        var item = _group.GetRadioItem(0);
        item.Size.X.Should().Be(30 + 10 + item.Label.Size.X);
        item.Size.Y.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public void Group_ShouldSizeToItems()
    {
        _group.Size.Should().Be(_group.Stack.Size);
        _group.Size.Y.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DisablingGroup_ShouldDisableItemsAndIgnoreClicks()
    {
        _group.IsEnabled = false;
        _h.Frame(2);

        ClickItem(1);

        _group.SelectedIndex.Should().Be(-1);
        _group.GetRadioItem(1).IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void RemovingSelectedItem_ShouldClearSelectionAndRaiseEvent()
    {
        _group.SelectedIndex = 2;
        var raised = new List<int>();
        _group.SelectionChanged += raised.Add;

        _group.Items.RemoveAt(2);

        _group.SelectedIndex.Should().Be(-1);
        raised.Should().Equal(-1);
    }

    [Fact]
    public void RemovingEarlierItem_ShouldKeepSelectionIndexInRange()
    {
        _group.SelectedIndex = 0;

        _group.Items.RemoveAt(2);

        _group.SelectedIndex.Should().Be(0);
        _group.GetRadioItem(0).IsSelected.Should().BeTrue();
    }
}
