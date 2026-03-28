using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトスライダースタイルです。
/// </summary>
public class DefaultSliderStyle : IUIStyle<Slider>
{
	public Color TrackColor { get; set; } = Color.FromArgb(255, 60, 60, 60);
	public Color TrackFillColor { get; set; } = Color.FromArgb(255, 80, 140, 220);
	public Color ThumbColor { get; set; } = Color.FromArgb(255, 200, 200, 200);
	public Color ThumbHoveredColor { get; set; } = Color.FromArgb(255, 230, 230, 230);
	public Color ThumbPressedColor { get; set; } = Color.FromArgb(255, 255, 255, 255);
	public Color DisabledTrackColor { get; set; } = Color.FromArgb(255, 40, 40, 40);
	public Color DisabledThumbColor { get; set; } = Color.FromArgb(255, 80, 80, 80);
	public Color FocusBorderColor { get; set; } = Color.FromArgb(255, 100, 150, 255);
	public int TrackBorderWidth { get; set; } = 1;
	public Color TrackBorderColor { get; set; } = Color.FromArgb(255, 80, 80, 80);

	public void Apply(Slider element, UIElementState state)
	{
		var isDisabled = state.HasFlag(UIElementState.Disabled);
		var trackH = element.TrackHeight;
		var trackY = (element.Size.Y - trackH) / 2;

		// トラック背景
		var trackColor = isDisabled ? DisabledTrackColor : TrackColor;
		var borderColor = state.HasFlag(UIElementState.Focused) ? FocusBorderColor : TrackBorderColor;
		var track = Shape.CreateRect(
			(0, trackY),
			(element.Size.X, trackY + trackH),
			trackColor,
			TrackBorderWidth,
			borderColor
		);
		element.SetBackgroundNode(track);

		// トラックのフィル部分
		var fillWidth = (int)(element.NormalizedValue * element.Size.X);
		if (fillWidth > 0)
		{
			var existingFill = FindNamedChild(element, "_track_fill");
			var fill = Shape.CreateRect(
				(0, trackY),
				(fillWidth, trackY + trackH),
				isDisabled ? DisabledTrackColor : TrackFillColor
			).Name("_track_fill");

			if (existingFill != null) element.Remove(existingFill);
			element.Insert(1, fill);
		}
		else
		{
			var existingFill = FindNamedChild(element, "_track_fill");
			if (existingFill != null) element.Remove(existingFill);
		}

		// つまみ
		var thumbW = element.ThumbSize.X;
		var thumbH = element.ThumbSize.Y;
		var thumbX = (int)(element.NormalizedValue * (element.Size.X - thumbW));
		var thumbY = (element.Size.Y - thumbH) / 2;

		var thumbColor = state switch
		{
			_ when isDisabled => DisabledThumbColor,
			_ when state.HasFlag(UIElementState.Pressed) => ThumbPressedColor,
			_ when state.HasFlag(UIElementState.Hovered) => ThumbHoveredColor,
			_ => ThumbColor,
		};

		var existingThumb = FindNamedChild(element, "_thumb");
		if (existingThumb != null) element.Remove(existingThumb);

		var thumb = Shape.CreateRect(
			(thumbX, thumbY),
			(thumbX + thumbW, thumbY + thumbH),
			thumbColor
		).Name("_thumb");
		element.Add(thumb);
	}

	private static Node? FindNamedChild(Slider element, string name)
	{
		for (var i = 0; i < element.Count; i++)
		{
			if (element[i].Name == name)
				return element[i];
		}
		return null;
	}
}
