using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトパネルスタイルです。
/// </summary>
public class DefaultPanelStyle : IUIStyle<Panel>
{
	/// <summary>背景色。</summary>
	public Color BackgroundColor { get; set; } = Color.FromArgb(200, 40, 40, 40);

	/// <summary>枠線色。</summary>
	public Color BorderColor { get; set; } = Color.FromArgb(255, 80, 80, 80);

	/// <summary>枠線の幅。</summary>
	public int BorderWidth { get; set; } = 1;

	public void Apply(Panel element, UIElementState state)
	{
		var bg = Shape.CreateRect(
			(0, 0),
			element.Size,
			BackgroundColor,
			BorderWidth,
			BorderColor
		);
		element.SetBackgroundNode(bg);
	}
}
