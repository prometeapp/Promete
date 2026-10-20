using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// 項目の一覧から1つを選ぶラジオボタンのグループです。
/// 項目は <see cref="StackContainer"/> で縦または横に並び、並べ方や見た目はスタイルが決めます。
/// </summary>
/// <remarks>
/// 各項目 (<see cref="RadioItem"/>) が個別にフォーカスを受け取ります。
/// 項目の選択は、クリック、またはフォーカス中の Enter / Space で行います。
/// </remarks>
public class RadioGroup : UIElement
{
    private readonly ObservableCollection<string> _items = [];
    private readonly List<RadioItem> _radioItems = [];
    private readonly StackContainer _stack;
    private IUIStyle<RadioGroup> _style;
    private IUIStyle<RadioItem> _itemStyle = new DefaultRadioItemStyle();
    private int _selectedIndex = -1;

    /// <summary>
    /// RadioGroup の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="items">最初の項目。</param>
    /// <param name="direction">項目を並べる方向。</param>
    /// <param name="style">適用するスタイル。null の場合は DefaultRadioGroupStyle が使用されます。</param>
    public RadioGroup(
        IEnumerable<string>? items = null,
        StackDirection direction = StackDirection.Vertical,
        IUIStyle<RadioGroup>? style = null
    )
    {
        _style = style ?? new DefaultRadioGroupStyle();
        _stack = new StackContainer(direction);
        IsFocusable = false;
        AutoSize = true;
        Add(_stack);

        if (items != null)
        {
            foreach (var item in items)
                _items.Add(item);
            RebuildItems();
        }

        _items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>選択された項目が変わったときに発火します。引数は新しい選択位置です (未選択は -1)。</summary>
    public event Action<int>? SelectionChanged;

    /// <summary>項目の一覧。変更すると、項目の表示も更新されます。</summary>
    public IList<string> Items => _items;

    /// <summary>項目を並べる方向を取得または設定します。</summary>
    public StackDirection Direction
    {
        get => _stack.Direction;
        set
        {
            if (_stack.Direction == value)
                return;

            _stack.Direction = value;
            NotifyStyleDirty();
        }
    }

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
            SyncSelection();
            SelectionChanged?.Invoke(index);
        }
    }

    /// <summary>選択されている項目。未選択の場合は null。</summary>
    public string? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    /// <summary>
    /// 項目を並べる <see cref="StackContainer"/>。
    /// スタイルが、項目の間隔・余白・揃え位置を設定するために使います。
    /// </summary>
    public StackContainer Stack => _stack;

    /// <summary>各項目 (<see cref="RadioItem"/>) のスタイルを取得または設定します。</summary>
    public IUIStyle<RadioItem> ItemStyle
    {
        get => _itemStyle;
        set
        {
            _itemStyle = value;
            foreach (var item in _radioItems)
                item.Style = value;
        }
    }

    /// <summary>グループのスタイルを取得または設定します。</summary>
    public IUIStyle<RadioGroup> Style
    {
        get => _style;
        set
        {
            _style = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// 項目を末尾に追加します。
    /// </summary>
    /// <param name="item">追加する項目。</param>
    /// <returns>メソッドチェーン用の自身。</returns>
    public RadioGroup AddItem(string item)
    {
        _items.Add(item);
        return this;
    }

    /// <summary>
    /// 指定した位置の項目の <see cref="RadioItem"/> を取得します。
    /// </summary>
    /// <param name="index">項目の位置。</param>
    public RadioItem GetRadioItem(int index) => _radioItems[index];

    protected override void ApplyStyle(UIElementState state)
    {
        _style.Apply(this, state);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // グループを無効にすると、全ての項目が操作できなくなる
        foreach (var item in _radioItems)
            item.IsEnabled = IsEnabled;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var previous = _selectedIndex;
        RebuildItems();

        // 項目が減って選択位置が範囲外になった場合は、未選択に戻す
        if (_selectedIndex >= _items.Count)
            _selectedIndex = -1;
        SyncSelection();
        if (_selectedIndex != previous)
            SelectionChanged?.Invoke(_selectedIndex);
    }

    private void RebuildItems()
    {
        foreach (var item in _radioItems)
            _stack.Remove(item);
        _radioItems.Clear();

        for (var i = 0; i < _items.Count; i++)
        {
            var index = i;
            var item = new RadioItem(_items[i], _itemStyle) { IsEnabled = IsEnabled };
            item.Clicked += () => SelectedIndex = index;
            _stack.Add(item);
            _radioItems.Add(item);
        }

        SyncSelection();
        NotifyStyleDirty();
    }

    private void SyncSelection()
    {
        for (var i = 0; i < _radioItems.Count; i++)
            _radioItems[i].IsSelected = i == _selectedIndex;
    }
}
