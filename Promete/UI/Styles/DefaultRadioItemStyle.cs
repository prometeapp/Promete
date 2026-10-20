using System;
using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトラジオ項目スタイルです。
/// 左に円形のマーカー、右にラベルを並べ、項目全体の大きさもここで決めます。
/// </summary>
public class DefaultRadioItemStyle : IUIStyle<RadioItem>
{
    /// <summary>マーカーの直径。</summary>
    public int MarkerSize { get; set; } = 16;

    /// <summary>マーカーとラベルの間隔。</summary>
    public int Spacing { get; set; } = 8;

    /// <summary>マーカーの内側の色。</summary>
    public Color MarkerColor { get; set; } = Color.FromArgb(255, 30, 30, 30);

    /// <summary>ホバー状態でのマーカーの内側の色。</summary>
    public Color MarkerHoveredColor { get; set; } = Color.FromArgb(255, 55, 55, 55);

    /// <summary>マーカーの枠線の色。</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(255, 130, 130, 130);

    /// <summary>フォーカス状態でのマーカーの枠線の色。</summary>
    public Color FocusedBorderColor { get; set; } = Color.FromArgb(255, 100, 150, 255);

    /// <summary>選択されたときにマーカーの中心に描く点の色。</summary>
    public Color SelectedColor { get; set; } = Color.FromArgb(255, 80, 140, 220);

    /// <summary>無効状態でのマーカーの枠線と点の色。</summary>
    public Color DisabledColor { get; set; } = Color.FromArgb(255, 70, 70, 70);

    /// <summary>テキストの色。</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>無効状態でのテキストの色。</summary>
    public Color DisabledTextColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

    public void Apply(RadioItem element, UIElementState state)
    {
        var isDisabled = state.HasFlag(UIElementState.Disabled);
        var labelSize = element.Label.Size;

        // 項目全体の大きさ: マーカーとラベルを横に並べる
        var height = Math.Max(MarkerSize, labelSize.Y);
        element.Size = (MarkerSize + Spacing + labelSize.X, height);
        element.Label.Location = new Vector(MarkerSize + Spacing, (height - labelSize.Y) / 2f);
        element.Label.Color = isDisabled ? DisabledTextColor : TextColor;

        // マーカー: 枠線の円、内側の円、選択時の点
        var radius = MarkerSize / 2f;
        var centerY = height / 2f;
        var border = state switch
        {
            _ when isDisabled => DisabledColor,
            _ when state.HasFlag(UIElementState.Focused) => FocusedBorderColor,
            _ => BorderColor,
        };
        var inner =
            state.HasFlag(UIElementState.Hovered) && !isDisabled ? MarkerHoveredColor : MarkerColor;

        var marker = new Container
        {
            CircleShape.Filled(radius, centerY, radius, border),
            CircleShape.Filled(radius, centerY, radius - 1.5f, inner),
        };
        if (element.IsSelected)
        {
            marker.Add(
                CircleShape.Filled(
                    radius,
                    centerY,
                    radius - 4f,
                    isDisabled ? DisabledColor : SelectedColor
                )
            );
        }

        element.SetBackgroundNode(marker);
    }
}
