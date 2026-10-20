using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// 項目の一覧から1つを選ぶドロップダウンUI要素です。
/// クリックまたは Enter / Space で項目の一覧を <see cref="Popup"/> として開きます。
/// </summary>
/// <remarks>
/// 一覧が画面に収まらない場合や、項目が <see cref="MaxVisibleItems"/> を超える場合は、スクロールして選びます。<br />
/// 一覧を開くには、UIManager が一度この要素を処理している必要があります。
/// </remarks>
public class Dropdown : UIElement
{
    private const int ListPadding = 2;

    private readonly ObservableCollection<string> _items = [];
    private readonly Text _label;
    private IUIStyle<Dropdown> _style;
    private string _placeholder;
    private int _selectedIndex = -1;
    private Popup? _popup;

    /// <summary>
    /// Dropdown の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="placeholder">項目が選択されていないときに表示する文字列。</param>
    /// <param name="style">適用するスタイル。null の場合は DefaultDropdownStyle が使用されます。</param>
    public Dropdown(string placeholder = "選択してください", IUIStyle<Dropdown>? style = null)
    {
        _style = style ?? new DefaultDropdownStyle();
        _placeholder = placeholder;
        _label = new Text(placeholder);
        Add(_label);
        Size = (200, 36);
        Clicked += Open;
        _items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>選択された項目が変わったときに発火します。引数は新しい選択位置です (未選択は -1)。</summary>
    public event Action<int>? SelectionChanged;

    /// <summary>
    /// 項目の一覧。開いている間に変更すると、一覧は閉じます (行の位置と項目がずれて、別の項目が選ばれるのを防ぐため)。
    /// 項目を挿入・削除して位置がずれても、選択中の項目はそのまま選ばれたままです (選択位置は追従します)。
    /// 選択中の項目を取り除くと、未選択になります。
    /// </summary>
    public IList<string> Items => _items;

    /// <summary>選択されている項目の位置を取得または設定します。未選択は -1 で、範囲外の値は -1 になります。</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var index = value >= 0 && value < _items.Count ? value : -1;
            if (_selectedIndex == index)
                return;

            _selectedIndex = index;
            SelectionChanged?.Invoke(index);
            NotifyStyleDirty();
        }
    }

    /// <summary>選択されている項目。未選択の場合は null。</summary>
    public string? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    /// <summary>項目が選択されていないときに表示する文字列を取得または設定します。</summary>
    public string Placeholder
    {
        get => _placeholder;
        set => _placeholder = value;
    }

    /// <summary>一度に表示する項目の最大数。これを超える項目はスクロールして選びます。</summary>
    public int MaxVisibleItems { get; set; } = 8;

    /// <summary>項目1つの高さ。</summary>
    public int ItemHeight { get; set; } = 28;

    /// <summary>項目のスタイル。</summary>
    public IUIStyle<Button> ItemStyle { get; set; } =
        new DefaultButtonStyle
        {
            NormalColor = Color.FromArgb(255, 45, 45, 45),
            HoveredColor = Color.FromArgb(255, 75, 75, 75),
            PressedColor = Color.FromArgb(255, 35, 35, 35),
            BorderColor = Color.FromArgb(255, 45, 45, 45),
        };

    /// <summary>選択中の項目のスタイル。</summary>
    public IUIStyle<Button> SelectedItemStyle { get; set; } =
        new DefaultButtonStyle
        {
            NormalColor = Color.FromArgb(255, 55, 85, 130),
            HoveredColor = Color.FromArgb(255, 70, 105, 155),
            PressedColor = Color.FromArgb(255, 40, 65, 105),
            BorderColor = Color.FromArgb(255, 55, 85, 130),
        };

    /// <summary>一覧が開いているかどうか。</summary>
    public bool IsOpen => _popup is { IsOpen: true };

    /// <summary>現在の表示を担当するラベル。フォントや色の直接制御に使用できます。</summary>
    public Text Label => _label;

    /// <summary>スタイルを取得または設定します。</summary>
    public IUIStyle<Dropdown> Style
    {
        get => _style;
        set
        {
            _style = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>開いている一覧のポップアップ。閉じている場合は null。</summary>
    internal Popup? CurrentPopup => IsOpen ? _popup : null;

    /// <summary>
    /// 項目を末尾に追加します。
    /// </summary>
    /// <param name="item">追加する項目。</param>
    /// <returns>メソッドチェーン用の自身。</returns>
    public Dropdown AddItem(string item)
    {
        _items.Add(item);
        return this;
    }

    /// <summary>
    /// 項目の一覧を開きます。項目がない場合、無効な場合、開いている場合は何もしません。
    /// </summary>
    public void Open()
    {
        var manager = Manager;
        if (manager == null || IsOpen || _items.Count == 0 || !IsEnabled)
            return;

        var rowWidth = Size.X - ListPadding * 2;
        var visibleRows = ComputeVisibleRows(manager, rowWidth);

        var popup = new Popup { Side = PopupSide.Bottom, Align = PopupAlign.Start };
        var rows = new List<Button>();
        ScrollView? scrollView = null;
        popup.Add(BuildList(rowWidth, visibleRows, rows, out scrollView));
        popup.FocusTarget = rows[_selectedIndex >= 0 ? _selectedIndex : 0];
        popup.Closed += OnPopupClosed;

        _popup = popup;
        popup.Show(manager, this);
        NotifyStyleDirty();

        if (scrollView != null)
            ScrollToItem(scrollView, _selectedIndex >= 0 ? _selectedIndex : 0, visibleRows);
    }

    /// <summary>
    /// 項目の一覧を閉じます。
    /// </summary>
    public void Close()
    {
        _popup?.Close();
    }

    protected override void ApplyStyle(UIElementState state)
    {
        _style.Apply(this, state);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        var text = SelectedItem ?? _placeholder;
        if (_label.Content != text)
            _label.Content = text;

        // ラベルを左寄せ・上下中央に配置 (右端は矢印のために空ける)
        var labelSize = _label.Size;
        _label.Location = new Vector(10, (Size.Y - labelSize.Y) / 2f);
    }

    /// <summary>
    /// 一覧に表示する行数を決めます。項目数と MaxVisibleItems に加え、画面に収まる行数で制限します。
    /// </summary>
    private int ComputeVisibleRows(UIManager manager, int rowWidth)
    {
        var rows = Math.Max(1, Math.Min(_items.Count, MaxVisibleItems));
        var placement = PopupPlacement.Compute(
            GetHitRect(),
            new Vector(rowWidth + ListPadding * 2, rows * ItemHeight + ListPadding * 2),
            manager.ViewSize
        );
        var fit = (int)((placement.AvailableSize.Y - ListPadding * 2) / ItemHeight);
        return Math.Max(1, Math.Min(rows, fit));
    }

    /// <summary>
    /// 項目の一覧の見た目を作ります。表示する行数より項目が多い場合は ScrollView で包みます。
    /// </summary>
    private StackContainer BuildList(
        int rowWidth,
        int visibleRows,
        List<Button> rows,
        out ScrollView? scrollView
    )
    {
        var rowStack = new StackContainer(StackDirection.Vertical);
        for (var i = 0; i < _items.Count; i++)
        {
            var index = i;
            var row = new DropdownItem(_items[i])
            {
                Style = i == _selectedIndex ? SelectedItemStyle : ItemStyle,
            };
            row.Size = (rowWidth, ItemHeight);
            row.Clicked += () => SelectAndClose(index);
            rowStack.Add(row);
            rows.Add(row);
        }

        var list = new StackContainer(StackDirection.Vertical, padding: ListPadding);
        if (_items.Count <= visibleRows)
        {
            scrollView = null;
            list.Add(rowStack);
            return list;
        }

        var view = new ScrollView { ContentSize = (rowWidth, _items.Count * ItemHeight) };
        view.Size = (rowWidth, visibleRows * ItemHeight);
        view.Content.Add(rowStack);
        for (var i = 0; i < rows.Count; i++)
        {
            var index = i;
            rows[i].GotFocus += () => ScrollToItem(view, index, visibleRows);
        }

        scrollView = view;
        list.Add(view);
        return list;
    }

    /// <summary>
    /// 指定した位置の項目が、ScrollView の表示範囲に入るようにスクロールします。
    /// </summary>
    private void ScrollToItem(ScrollView view, int index, int visibleRows)
    {
        var top = index * ItemHeight;
        var bottom = top + ItemHeight;
        var viewHeight = visibleRows * ItemHeight;

        // ScrollView のスクロール位置は、内容を上へ動かした量を負の値で表す
        var scrolled = -view.ScrollOffset.Y;

        if (top < scrolled)
            view.ScrollTo(0, -top);
        else if (bottom > scrolled + viewHeight)
            view.ScrollTo(0, -(bottom - viewHeight));
    }

    private void SelectAndClose(int index)
    {
        SelectedIndex = index;
        Close();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 開いている一覧の行は、開いた時点の位置を覚えているので、項目が変わったら閉じる
        Close();

        // 挿入・削除で選択位置がずれても、同じ項目を選んだままにする。選択中の項目が取り除かれた場合は未選択に戻す
        var previous = _selectedIndex;
        _selectedIndex = SelectionAdjuster.Adjust(_selectedIndex, e);
        if (_selectedIndex == previous)
            return;

        NotifyStyleDirty();
        if (_selectedIndex < 0)
            SelectionChanged?.Invoke(-1);
    }

    private void OnPopupClosed()
    {
        _popup = null;
        NotifyStyleDirty();
    }

    /// <summary>
    /// 一覧の1行です。ラベルを左寄せで表示します。
    /// </summary>
    private sealed class DropdownItem : Button
    {
        public DropdownItem(string text)
            : base(text) { }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var labelSize = Label.Size;
            Label.Location = new Vector(8, (Size.Y - labelSize.Y) / 2f);
        }
    }
}
