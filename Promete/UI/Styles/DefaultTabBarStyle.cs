using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトタブバースタイルです。
/// 見出しの間隔と、ページ領域の背景・枠線を決めます。
/// </summary>
public class DefaultTabBarStyle : IUIStyle<TabBar>
{
    /// <summary>見出し (タブ) 同士の間隔。</summary>
    public int HeaderGap { get; set; } = 2;

    /// <summary>ページ領域の背景色。</summary>
    public Color BackgroundColor { get; set; } = Color.FromArgb(200, 40, 40, 40);

    /// <summary>ページ領域の枠線色。</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

    /// <summary>ページ領域の枠線の幅。</summary>
    public int BorderWidth { get; set; } = 1;

    public void Apply(TabBar element, UIElementState state)
    {
        element.Header.Gap = HeaderGap;

        // ページ領域は、見出しの下から
        var top = element.Header.Size.Y;
        var bottom = element.Size.Y;
        if (bottom <= top)
        {
            element.SetBackgroundNode(null);
            return;
        }

        element.SetBackgroundNode(
            Shape.CreateRect(
                (0, top),
                (element.Size.X, bottom),
                BackgroundColor,
                BorderWidth,
                BorderColor
            )
        );
    }
}
