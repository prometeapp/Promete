using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// <see cref="TabBar"/> の見出し (タブ) 1つ分です。
/// 大きさはスタイルが決めます。
/// </summary>
public class TabButton : UIElement
{
    private readonly Text _label;
    private IUIStyle<TabButton> _style;
    private bool _isSelected;

    /// <summary>
    /// TabButton の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="text">タブに表示するテキスト。</param>
    /// <param name="style">適用するスタイル。null の場合は DefaultTabButtonStyle が使用されます。</param>
    public TabButton(string text = "", IUIStyle<TabButton>? style = null)
    {
        _style = style ?? new DefaultTabButtonStyle();
        _label = new Text(text);
        Add(_label);
        Size = (80, 30);
    }

    /// <summary>選択されているタブかどうか。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        internal set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>タブのテキスト内容を取得または設定します。</summary>
    public string TextContent
    {
        get => _label.Content;
        set
        {
            _label.Content = value;
            NotifyStyleDirty();
        }
    }

    /// <summary>
    /// ラベルの Text ノードを取得します。
    /// フォントや色の直接制御に使用できます。
    /// </summary>
    public Text Label => _label;

    /// <summary>スタイルを取得または設定します。</summary>
    public IUIStyle<TabButton> Style
    {
        get => _style;
        set
        {
            _style = value;
            NotifyStyleDirty();
        }
    }

    protected override void ApplyStyle(UIElementState state)
    {
        _style.Apply(this, state);
    }
}
