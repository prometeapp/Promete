using System;
using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトスクロールビュースタイルです。
/// </summary>
public class DefaultScrollViewStyle : IUIStyle<ScrollView>
{
	public Color BackgroundColor { get; set; } = Color.FromArgb(200, 30, 30, 30);
	public Color BorderColor { get; set; } = Color.FromArgb(255, 70, 70, 70);
	public Color ScrollbarTrackColor { get; set; } = Color.FromArgb(100, 50, 50, 50);
	public Color ScrollbarThumbColor { get; set; } = Color.FromArgb(200, 120, 120, 120);
	public int BorderWidth { get; set; } = 1;
	public int ScrollbarWidth { get; set; } = 8;

	public void Apply(ScrollView element, UIElementState state)
	{
		// 背景
		var bg = Shape.CreateRect(
			(0, 0),
			element.Size,
			BackgroundColor,
			BorderWidth,
			BorderColor
		);
		element.SetBackgroundNode(bg);

		// 垂直スクロールバー
		var existingBar = FindNamedChild(element, "_vscrollbar");
		if (existingBar != null) element.Remove(existingBar);

		if (element.VerticalScrollEnabled && element.ContentSize.Y > element.Size.Y)
		{
			var barX = element.Size.X - ScrollbarWidth;
			var viewRatio = (float)element.Size.Y / element.ContentSize.Y;
			var thumbHeight = Math.Max(20, (int)(element.Size.Y * viewRatio));
			var thumbY = (int)(element.NormalizedVerticalScroll * (element.Size.Y - thumbHeight));

			var scrollbar = new Container();
			scrollbar.Name("_vscrollbar");

			var track = Shape.CreateRect(
				(barX, 0),
				(barX + ScrollbarWidth, element.Size.Y),
				ScrollbarTrackColor
			);
			scrollbar.Add(track);

			var thumb = Shape.CreateRect(
				(barX, thumbY),
				(barX + ScrollbarWidth, thumbY + thumbHeight),
				ScrollbarThumbColor
			);
			scrollbar.Add(thumb);

			element.Add(scrollbar);
		}
	}

	private static Node? FindNamedChild(ScrollView element, string name)
	{
		for (var i = 0; i < element.Count; i++)
		{
			if (element[i].Name == name)
				return element[i];
		}
		return null;
	}
}
