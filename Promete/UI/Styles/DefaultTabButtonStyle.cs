using System;
using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトタブ見出しスタイルです。
/// ラベルの幅に左右の余白を加えた大きさにし、選択中のタブは色を変えて下線を引きます。
/// </summary>
public class DefaultTabButtonStyle : IUIStyle<TabButton>
{
    /// <summary>ラベルの左右の余白。</summary>
    public int HorizontalPadding { get; set; } = 16;

    /// <summary>タブの高さの最小値。</summary>
    public int Height { get; set; } = 30;

    /// <summary>選択されていないタブの背景色。</summary>
    public Color NormalColor { get; set; } = Color.FromArgb(255, 45, 45, 45);

    /// <summary>ホバー状態の背景色。</summary>
    public Color HoveredColor { get; set; } = Color.FromArgb(255, 65, 65, 65);

    /// <summary>選択中のタブの背景色。</summary>
    public Color SelectedColor { get; set; } = Color.FromArgb(255, 70, 70, 70);

    /// <summary>無効状態の背景色。</summary>
    public Color DisabledColor { get; set; } = Color.FromArgb(255, 30, 30, 30);

    /// <summary>選択中のタブの下線の色。</summary>
    public Color AccentColor { get; set; } = Color.FromArgb(255, 80, 140, 220);

    /// <summary>通常時の枠線色。</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

    /// <summary>フォーカス状態の枠線色。</summary>
    public Color FocusedBorderColor { get; set; } = Color.FromArgb(255, 100, 150, 255);

    /// <summary>選択中のタブのテキスト色。</summary>
    public Color SelectedTextColor { get; set; } = Color.White;

    /// <summary>選択されていないタブのテキスト色。</summary>
    public Color TextColor { get; set; } = Color.FromArgb(255, 180, 180, 180);

    /// <summary>無効状態のテキスト色。</summary>
    public Color DisabledTextColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

    /// <summary>下線の太さ。</summary>
    public int AccentHeight { get; set; } = 3;

    public void Apply(TabButton element, UIElementState state)
    {
        var isDisabled = state.HasFlag(UIElementState.Disabled);
        var labelSize = element.Label.Size;

        // 大きさ: ラベルの幅に左右の余白を加える
        var width = labelSize.X + HorizontalPadding * 2;
        var height = Math.Max(Height, labelSize.Y);
        element.Size = (width, height);
        element.Label.Location = new Vector(
            (width - labelSize.X) / 2f,
            (height - labelSize.Y) / 2f
        );
        element.Label.Color =
            isDisabled ? DisabledTextColor
            : element.IsSelected ? SelectedTextColor
            : TextColor;

        var background = state switch
        {
            _ when isDisabled => DisabledColor,
            _ when element.IsSelected => SelectedColor,
            _ when state.HasFlag(UIElementState.Hovered) => HoveredColor,
            _ => NormalColor,
        };
        var border = state.HasFlag(UIElementState.Focused) ? FocusedBorderColor : BorderColor;

        var shapes = new Container
        {
            Shape.CreateRect((0, 0), element.Size, background, 1, border),
        };
        if (element.IsSelected)
        {
            shapes.Add(Shape.CreateRect((0, height - AccentHeight), (width, height), AccentColor));
        }

        element.SetBackgroundNode(shapes);
    }
}
