using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトドロップダウンスタイルです。
/// ボタンと同じ配色の背景に、右端の「▼」の矢印を描画します。
/// </summary>
public class DefaultDropdownStyle : IUIStyle<Dropdown>
{
    /// <summary>通常状態の背景色。</summary>
    public Color NormalColor { get; set; } = Color.FromArgb(255, 60, 60, 60);

    /// <summary>ホバー状態の背景色。</summary>
    public Color HoveredColor { get; set; } = Color.FromArgb(255, 80, 80, 80);

    /// <summary>押下状態、または開いている間の背景色。</summary>
    public Color PressedColor { get; set; } = Color.FromArgb(255, 40, 40, 40);

    /// <summary>無効状態の背景色。</summary>
    public Color DisabledColor { get; set; } = Color.FromArgb(255, 30, 30, 30);

    /// <summary>フォーカス状態の枠線色。</summary>
    public Color FocusedBorderColor { get; set; } = Color.FromArgb(255, 100, 150, 255);

    /// <summary>通常時の枠線色。</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

    /// <summary>枠線の幅。</summary>
    public int BorderWidth { get; set; } = 1;

    /// <summary>通常状態のテキスト色。</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>項目が選択されていないとき (プレースホルダー表示) のテキスト色。</summary>
    public Color PlaceholderColor { get; set; } = Color.FromArgb(255, 160, 160, 160);

    /// <summary>無効状態のテキスト色。</summary>
    public Color DisabledTextColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

    /// <summary>矢印の色。</summary>
    public Color ArrowColor { get; set; } = Color.FromArgb(255, 200, 200, 200);

    public void Apply(Dropdown element, UIElementState state)
    {
        var bgColor = state switch
        {
            _ when state.HasFlag(UIElementState.Disabled) => DisabledColor,
            _ when state.HasFlag(UIElementState.Pressed) || element.IsOpen => PressedColor,
            _ when state.HasFlag(UIElementState.Hovered) => HoveredColor,
            _ => NormalColor,
        };
        var borderColor = state.HasFlag(UIElementState.Focused) ? FocusedBorderColor : BorderColor;

        var width = element.Size.X;
        var height = element.Size.Y;
        var arrowX = width - 20;
        var arrowY = height / 2;

        var background = new Container
        {
            Shape.CreateRect((0, 0), element.Size, bgColor, BorderWidth, borderColor),
            Shape.CreateTriangle(
                arrowX,
                arrowY - 3,
                arrowX + 10,
                arrowY - 3,
                arrowX + 5,
                arrowY + 3,
                state.HasFlag(UIElementState.Disabled) ? DisabledTextColor : ArrowColor
            ),
        };
        element.SetBackgroundNode(background);

        element.Label.Color = state switch
        {
            _ when state.HasFlag(UIElementState.Disabled) => DisabledTextColor,
            _ when element.SelectedItem == null => PlaceholderColor,
            _ => TextColor,
        };
    }
}
