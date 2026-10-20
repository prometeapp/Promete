using System;
using System.Collections.Generic;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// 上部の見出し (タブ) で、複数のページ (<see cref="Container"/>) を切り替えるタブバーです。
/// </summary>
/// <remarks>
/// <see cref="AddTab"/> で、見出しの文字列とページを組にして追加します。
/// 選択されているページだけが表示され、選択されていないページは非表示になります。
/// 非表示のページの中の UI 要素は、クリックにもフォーカス移動にも反応しません。<br />
/// ページは、見出しの下、<see cref="ContentPadding"/> の内側の左上を原点として配置されます。
/// タブバーの大きさは <see cref="Node.Size"/> で指定します。
/// </remarks>
public class TabBar : UIElement
{
    private readonly List<Container> _pages = [];
    private readonly List<TabButton> _tabs = [];
    private readonly StackContainer _header = new(StackDirection.Horizontal);
    private IUIStyle<TabBar> _style;
    private IUIStyle<TabButton> _tabStyle = new DefaultTabButtonStyle();
    private Padding _contentPadding = new(8);
    private int _selectedIndex = -1;
    private int _styledHeaderHeight;

    /// <summary>
    /// TabBar の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="style">適用するスタイル。null の場合は DefaultTabBarStyle が使用されます。</param>
    public TabBar(IUIStyle<TabBar>? style = null)
    {
        _style = style ?? new DefaultTabBarStyle();
        IsFocusable = false;
        Add(_header);
        Size = (400, 300);
    }

    /// <summary>選択されているタブが変わったときに発火します。引数は新しい選択位置です (タブがなくなった場合は -1)。</summary>
    public event Action<int>? SelectionChanged;

    /// <summary>ページの一覧。タブの順に並びます。</summary>
    public IReadOnlyList<Container> Pages => _pages;

    /// <summary>タブの数。</summary>
    public int TabCount => _pages.Count;

    /// <summary>
    /// 選択されているタブの位置を取得または設定します。タブがない場合は -1 です。
    /// 範囲外の値は無視されます。
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= _pages.Count || value == _selectedIndex)
                return;

            Select(value, SelectedPage);
        }
    }

    /// <summary>選択されているページ。タブがない場合は null。</summary>
    public Container? SelectedPage =>
        _selectedIndex >= 0 && _selectedIndex < _pages.Count ? _pages[_selectedIndex] : null;

    /// <summary>ページ領域の内側の余白を取得または設定します。</summary>
    public Padding ContentPadding
    {
        get => _contentPadding;
        set
        {
            _contentPadding = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// 見出しを並べる <see cref="StackContainer"/>。
    /// スタイルが、見出しの間隔などを設定するために使います。
    /// </summary>
    public StackContainer Header => _header;

    /// <summary>各見出し (<see cref="TabButton"/>) のスタイルを取得または設定します。</summary>
    public IUIStyle<TabButton> TabStyle
    {
        get => _tabStyle;
        set
        {
            _tabStyle = value;
            foreach (var tab in _tabs)
                tab.Style = value;
        }
    }

    /// <summary>タブバーのスタイルを取得または設定します。</summary>
    public IUIStyle<TabBar> Style
    {
        get => _style;
        set
        {
            _style = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// タブを末尾に追加します。最初に追加したタブは、自動で選択されます。
    /// </summary>
    /// <param name="title">見出しの文字列。</param>
    /// <param name="page">タブを選んだときに表示するページ。</param>
    /// <returns>メソッドチェーン用の自身。</returns>
    public TabBar AddTab(string title, Container page)
    {
        var tab = new TabButton(title, _tabStyle);
        tab.Clicked += () => SelectedIndex = _tabs.IndexOf(tab);

        _tabs.Add(tab);
        _pages.Add(page);
        _header.Add(tab);
        Add(page);
        page.IsVisible = false;

        if (_selectedIndex < 0)
            Select(0, null);
        else
            NotifyStyleDirty();

        return this;
    }

    /// <summary>
    /// 指定した位置のタブとページを取り除きます。ページは、タブバーの子ノードから外れます。
    /// 選択中のタブを取り除いた場合は、隣のタブが選択されます。
    /// </summary>
    /// <param name="index">取り除くタブの位置。</param>
    public void RemoveTab(int index)
    {
        if (index < 0 || index >= _pages.Count)
            return;

        var page = _pages[index];
        var tab = _tabs[index];
        _pages.RemoveAt(index);
        _tabs.RemoveAt(index);
        _header.Remove(tab);
        Remove(page);
        page.IsVisible = true;

        if (_pages.Count == 0)
        {
            _selectedIndex = -1;
            SelectionChanged?.Invoke(-1);
        }
        else if (index == _selectedIndex)
        {
            Select(Math.Min(index, _pages.Count - 1), page);
        }
        else if (index < _selectedIndex)
        {
            _selectedIndex--;
        }

        NotifyStyleDirty();
    }

    /// <summary>
    /// 指定した位置の見出し (<see cref="TabButton"/>) を取得します。
    /// </summary>
    /// <param name="index">タブの位置。</param>
    public TabButton GetTabButton(int index) => _tabs[index];

    protected override void ApplyStyle(UIElementState state)
    {
        _style.Apply(this, state);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // ページを、見出しの下の余白の内側に配置する
        var headerHeight = _header.Size.Y;
        var origin = new Vector(_contentPadding.Left, headerHeight + _contentPadding.Top);
        foreach (var page in _pages)
        {
            if (page.Location != origin)
                page.Location = origin;
        }

        // 見出しの高さが変わったら、背景を作り直す
        if (headerHeight != _styledHeaderHeight)
        {
            _styledHeaderHeight = headerHeight;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// 指定した位置のタブを選択します。
    /// </summary>
    /// <param name="index">選択するタブの位置。</param>
    /// <param name="previousPage">直前に表示していたページ。そのページの中にフォーカスがあった場合は、選んだタブの見出しに移します。</param>
    private void Select(int index, Container? previousPage)
    {
        _selectedIndex = index;

        for (var i = 0; i < _pages.Count; i++)
        {
            _pages[i].IsVisible = i == index;
            _tabs[i].IsSelected = i == index;
        }

        // 非表示になったページの中にフォーカスがあった場合は、選んだタブの見出しに移す
        var focused = Manager?.FocusedElement;
        if (focused != null && previousPage != null && IsDescendantOf(focused, previousPage))
            Manager!.SetFocus(_tabs[index]);

        SelectionChanged?.Invoke(index);
    }

    private static bool IsDescendantOf(Node node, Node ancestor)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (current == ancestor)
                return true;
        }

        return false;
    }
}
