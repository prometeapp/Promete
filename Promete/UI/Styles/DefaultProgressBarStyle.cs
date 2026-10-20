using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトプログレスバースタイルです。
/// </summary>
public class DefaultProgressBarStyle : IUIStyle<ProgressBar>
{
    /// <summary>背景 (未達成の部分) の色。</summary>
    public Color TrackColor { get; set; } = Color.FromArgb(255, 40, 40, 40);

    /// <summary>達成した部分の色。</summary>
    public Color FillColor { get; set; } = Color.FromArgb(255, 80, 140, 220);

    /// <summary>無効状態での達成した部分の色。</summary>
    public Color DisabledFillColor { get; set; } = Color.FromArgb(255, 80, 80, 80);

    /// <summary>枠線の色。</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

    /// <summary>枠線の幅。</summary>
    public int BorderWidth { get; set; } = 1;

    public void Apply(ProgressBar element, UIElementState state)
    {
        var width = element.Size.X;
        var height = element.Size.Y;

        var background = new Container
        {
            Shape.CreateRect((0, 0), element.Size, TrackColor, BorderWidth, BorderColor),
        };

        // 枠線の内側に、達成した割合だけ塗る
        var innerWidth = width - BorderWidth * 2;
        var fillWidth = (int)(innerWidth * element.Fraction);
        if (fillWidth > 0 && height > BorderWidth * 2)
        {
            var fillColor = state.HasFlag(UIElementState.Disabled) ? DisabledFillColor : FillColor;
            background.Add(
                Shape.CreateRect(
                    (BorderWidth, BorderWidth),
                    (BorderWidth + fillWidth, height - BorderWidth),
                    fillColor
                )
            );
        }

        element.SetBackgroundNode(background);
    }
}
