using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトポップアップスタイルです。
/// </summary>
public class DefaultPopupStyle : IUIStyle<Popup>
{
    /// <summary>背景色。</summary>
    public Color BackgroundColor { get; set; } = Color.FromArgb(255, 45, 45, 45);

    /// <summary>枠線色。</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(255, 120, 120, 120);

    /// <summary>枠線の幅。</summary>
    public int BorderWidth { get; set; } = 1;

    public void Apply(Popup element, UIElementState state)
    {
        var bg = Shape.CreateRect((0, 0), element.Size, BackgroundColor, BorderWidth, BorderColor);
        element.SetBackgroundNode(bg);
    }
}
