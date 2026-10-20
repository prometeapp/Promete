using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

public class DropdownTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Dropdown _dropdown;
    private readonly Button _other;
    private int _otherClicks;

    public DropdownTests()
    {
        _dropdown = new Dropdown("選んで").Location(100, 100).Size(200, 36);
        _dropdown.AddItem("A").AddItem("B").AddItem("C");
        _other = new Button("other").Location(500, 300).Size(80, 30);
        _other.Clicked += () => _otherClicks++;
        _h.App.GlobalForeground.Add(_dropdown);
        _h.App.GlobalForeground.Add(_other);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    private Vector View => _h.App.View.Size;

    /// <summary>ドロップダウンの中央をクリックして開きます。</summary>
    private void OpenByClick()
    {
        _h.Click(200, 118);
        _h.Frame(2);
    }

    /// <summary>開いている一覧の、n 番目の行の中央の座標を返します (スクロールなし)。</summary>
    private (int X, int Y) RowCenter(int index)
    {
        var popup = _dropdown.CurrentPopup!;
        return ((int)popup.Location.X + 100, (int)popup.Location.Y + 2 + index * 28 + 14);
    }

    [Fact]
    public void ChangingItemsWhileOpen_ShouldCloseTheList_SoNoWrongItemIsPicked()
    {
        OpenByClick();
        _dropdown.IsOpen.Should().BeTrue();

        _dropdown.Items.Insert(0, "X");

        _dropdown.IsOpen.Should().BeFalse();
        _dropdown.SelectedIndex.Should().Be(-1);

        // 開き直すと、新しい項目の並びで選べる
        OpenByClick();
        var (x, y) = RowCenter(1);
        _h.Click(x, y);
        _dropdown.SelectedItem.Should().Be("A");
    }

    [Fact]
    public void RemovingTheSelectedItem_ShouldClearSelection_AndStillBeOpenable()
    {
        var raised = new List<int>();
        _dropdown.SelectedIndex = 2;
        _dropdown.SelectionChanged += raised.Add;

        _dropdown.Items.RemoveAt(2);

        _dropdown.SelectedIndex.Should().Be(-1);
        _dropdown.SelectedItem.Should().BeNull();
        raised.Should().Equal(-1);

        OpenByClick();
        _dropdown.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void RemovingAnItemBeforeTheSelection_ShouldKeepTheSameItemSelected()
    {
        var raised = new List<int>();
        _dropdown.SelectedIndex = 2;
        _dropdown.SelectionChanged += raised.Add;

        _dropdown.Items.RemoveAt(0);

        // 選択中の項目 (C) はそのままで、位置だけが前にずれる
        _dropdown.SelectedItem.Should().Be("C");
        _dropdown.SelectedIndex.Should().Be(1);
        raised.Should().BeEmpty();
    }

    [Fact]
    public void InsertingAnItemBeforeTheSelection_ShouldKeepTheSameItemSelected()
    {
        _dropdown.SelectedIndex = 1;

        _dropdown.Items.Insert(0, "X");

        _dropdown.SelectedItem.Should().Be("B");
        _dropdown.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void InsertingAnItemAfterTheSelection_ShouldNotMoveIt()
    {
        _dropdown.SelectedIndex = 0;

        _dropdown.Items.Insert(2, "X");

        _dropdown.SelectedItem.Should().Be("A");
        _dropdown.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void ClearingItems_ShouldClearSelectionAndRaiseEvent()
    {
        var raised = new List<int>();
        _dropdown.SelectedIndex = 1;
        _dropdown.SelectionChanged += raised.Add;

        _dropdown.Items.Clear();

        _dropdown.SelectedIndex.Should().Be(-1);
        raised.Should().Equal(-1);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var dropdown = new Dropdown();

        dropdown.SelectedIndex.Should().Be(-1);
        dropdown.SelectedItem.Should().BeNull();
        dropdown.IsOpen.Should().BeFalse();
        dropdown.IsFocusable.Should().BeTrue();
        dropdown.MaxVisibleItems.Should().Be(8);
        dropdown.Size.Should().Be(new VectorInt(200, 36));
    }

    [Fact]
    public void Label_ShouldShowPlaceholderThenSelectedItem()
    {
        _dropdown.Label.Content.Should().Be("選んで");

        _dropdown.SelectedIndex = 1;
        _h.Frame();

        _dropdown.Label.Content.Should().Be("B");
    }

    [Fact]
    public void SelectedIndex_ShouldRaiseEventOnlyOnChange_AndClampOutOfRange()
    {
        var raised = new List<int>();
        _dropdown.SelectionChanged += raised.Add;

        _dropdown.SelectedIndex = 2;
        _dropdown.SelectedIndex = 2;
        _dropdown.SelectedIndex = 99;

        raised.Should().Equal(2, -1);
        _dropdown.SelectedItem.Should().BeNull();
    }

    [Fact]
    public void Click_ShouldOpenListBelowDropdown()
    {
        OpenByClick();

        _dropdown.IsOpen.Should().BeTrue();
        var popup = _dropdown.CurrentPopup!;
        popup.Location.Should().Be(new Vector(100, 136));
        popup.Size.Should().Be(new VectorInt(200, 3 * 28 + 4));
    }

    [Fact]
    public void ClickingItem_ShouldSelectAndClose()
    {
        var raised = new List<int>();
        _dropdown.SelectionChanged += raised.Add;
        OpenByClick();

        var (x, y) = RowCenter(1);
        _h.Click(x, y);

        _dropdown.SelectedItem.Should().Be("B");
        raised.Should().Equal(1);
        _dropdown.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Open_ShouldFocusFirstItem_WhenNothingSelected()
    {
        OpenByClick();

        _h.UI.FocusedElement.Should().NotBeNull();
        _h.UI.FocusedElement!.Parent.Should().NotBeNull();
        _h.UI.FocusedElement!.Location.Y.Should().Be(0);
    }

    [Fact]
    public void Open_ShouldFocusSelectedItem()
    {
        _dropdown.SelectedIndex = 2;
        OpenByClick();

        _h.UI.FocusedElement!.Location.Y.Should().Be(2 * 28);
    }

    [Fact]
    public void Keyboard_EnterOpens_DownMoves_EnterSelects()
    {
        _h.UI.SetFocus(_dropdown);

        _h.PressKey(KeyCode.Enter);
        _dropdown.IsOpen.Should().BeTrue();

        _h.PressKey(KeyCode.Down);
        _h.PressKey(KeyCode.Enter);

        _dropdown.SelectedItem.Should().Be("B");
        _dropdown.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Close_ShouldReturnFocusToDropdown()
    {
        _h.UI.SetFocus(_dropdown);
        _h.PressKey(KeyCode.Space);
        _dropdown.IsOpen.Should().BeTrue();

        _h.PressKey(KeyCode.Enter);

        _h.UI.FocusedElement.Should().BeSameAs(_dropdown);
    }

    [Fact]
    public void Escape_ShouldCloseWithoutChangingSelection()
    {
        _dropdown.SelectedIndex = 0;
        OpenByClick();

        _h.PressKey(KeyCode.Escape);

        _dropdown.IsOpen.Should().BeFalse();
        _dropdown.SelectedIndex.Should().Be(0);
    }

    [Fact]
    public void OutsideClick_ShouldCloseWithoutChangingSelection_AndNotReachElementBelow()
    {
        OpenByClick();

        _h.Click(540, 315);

        _dropdown.IsOpen.Should().BeFalse();
        _dropdown.SelectedIndex.Should().Be(-1);
        _otherClicks.Should().Be(0);
    }

    [Fact]
    public void ClickingDropdownWhileOpen_ShouldOnlyClose()
    {
        OpenByClick();

        _h.Click(200, 118);

        _dropdown.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Open_ShouldDoNothing_WhenEmptyOrDisabled()
    {
        var empty = new Dropdown().Location(100, 200).Size(200, 36);
        _h.App.GlobalForeground.Add(empty);
        _h.Frame(2);
        _h.Click(200, 218);
        empty.IsOpen.Should().BeFalse();

        _dropdown.IsEnabled = false;
        _h.Frame();
        OpenByClick();
        _dropdown.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void List_ShouldOpenAbove_WhenDropdownIsNearBottom()
    {
        _dropdown.Location = (100, View.Y - 50);
        _h.Frame(2);

        OpenByClick2();

        var popup = _dropdown.CurrentPopup!;
        popup.Location.Y.Should().BeLessThan(_dropdown.Location.Y);
        (popup.Location.Y + popup.Size.Y).Should().BeLessThanOrEqualTo(View.Y);

        void OpenByClick2()
        {
            _h.Click(200, (int)_dropdown.Location.Y + 18);
            _h.Frame(2);
        }
    }

    [Fact]
    public void ManyItems_ShouldScroll_AndLimitVisibleRows()
    {
        for (var i = 0; i < 20; i++)
            _dropdown.AddItem($"item{i}");
        _dropdown.MaxVisibleItems = 5;
        OpenByClick();

        var popup = _dropdown.CurrentPopup!;
        popup.Size.Y.Should().Be(5 * 28 + 4);

        // 見えていない行までキーで移動すると、フォーカス行が見えるようにスクロールされる
        for (var i = 0; i < 8; i++)
            _h.PressKey(KeyCode.Down);

        var scrollView = FindScrollView(popup);
        scrollView.ScrollOffset.Y.Should().BeLessThan(0);
        var focused = _h.UI.FocusedElement!;
        var rowTop = focused.Location.Y + scrollView.ScrollOffset.Y;
        rowTop.Should().BeInRange(0f, 5f * 28 - 28);
    }

    [Fact]
    public void ManyItems_ShouldReduceVisibleRows_ToFitOnScreen()
    {
        for (var i = 0; i < 40; i++)
            _dropdown.AddItem($"item{i}");
        _dropdown.MaxVisibleItems = 100;
        _dropdown.Location = (100, View.Y / 2 - 18);
        _h.Frame(2);

        _h.Click(200, (int)_dropdown.Location.Y + 18);
        _h.Frame(2);

        var popup = _dropdown.CurrentPopup!;
        ((float)popup.Size.Y).Should().BeLessThanOrEqualTo(popup.AvailableSize.Y);
        popup.Location.Y.Should().BeGreaterThanOrEqualTo(0);
        (popup.Location.Y + popup.Size.Y).Should().BeLessThanOrEqualTo(View.Y);
    }

    private static ScrollView FindScrollView(Node node) =>
        TryFindScrollView(node)
        ?? throw new InvalidOperationException("ScrollView が見つかりません。");

    private static ScrollView? TryFindScrollView(Node node)
    {
        if (node is ScrollView scrollView)
            return scrollView;

        if (node is Container container)
        {
            for (var i = 0; i < container.Count; i++)
            {
                var found = TryFindScrollView(container[i]);
                if (found != null)
                    return found;
            }
        }

        return null;
    }
}
