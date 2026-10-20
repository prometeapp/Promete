using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.UI.Elements;

/// <summary>
/// <see cref="RadioGroup"/> の項目1つ分です。
/// 選択マーカーとラベルで構成され、マーカーとラベルの並びや大きさはスタイルが決めます。
/// </summary>
public class RadioItem : UIElement
{
    private readonly Text _label;
    private IUIStyle<RadioItem> _style;
    private bool _isSelected;

    /// <summary>
    /// RadioItem の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="text">項目に表示するテキスト。</param>
    /// <param name="style">適用するスタイル。null の場合は DefaultRadioItemStyle が使用されます。</param>
    public RadioItem(string text = "", IUIStyle<RadioItem>? style = null)
    {
        _style = style ?? new DefaultRadioItemStyle();
        _label = new Text(text);
        Add(_label);
        Size = (100, 20);
    }

    /// <summary>選択されているかどうか。</summary>
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

    /// <summary>項目のテキスト内容を取得または設定します。</summary>
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
    public IUIStyle<RadioItem> Style
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
