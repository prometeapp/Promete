using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトラジオグループスタイルです。
/// 項目を並べる間隔・余白・揃え位置と、グループの背景を決めます。
/// </summary>
public class DefaultRadioGroupStyle : IUIStyle<RadioGroup>
{
    /// <summary>縦に並べるときの項目の間隔。</summary>
    public int VerticalGap { get; set; } = 6;

    /// <summary>横に並べるときの項目の間隔。</summary>
    public int HorizontalGap { get; set; } = 20;

    /// <summary>グループの周囲の余白。</summary>
    public Padding Padding { get; set; } = Padding.None;

    /// <summary>並べる方向と直交する方向の揃え位置。</summary>
    public StackAlignment Align { get; set; } = StackAlignment.Start;

    /// <summary>グループの背景色。null の場合は背景を描きません。</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>背景の枠線色。</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(255, 80, 80, 80);

    /// <summary>背景の枠線の幅。</summary>
    public int BorderWidth { get; set; } = 1;

    public void Apply(RadioGroup element, UIElementState state)
    {
        var stack = element.Stack;
        stack.Gap = element.Direction == StackDirection.Vertical ? VerticalGap : HorizontalGap;
        stack.Padding = Padding;
        stack.Align = Align;

        element.SetBackgroundNode(
            BackgroundColor is { } color
                ? Shape.CreateRect((0, 0), element.Size, color, BorderWidth, BorderColor)
                : null
        );
    }
}
