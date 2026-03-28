using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトチェックボックススタイルです。
/// </summary>
public class DefaultCheckboxStyle : IUIStyle<Checkbox>
{
	public Color BoxColor { get; set; } = Color.FromArgb(255, 50, 50, 50);
	public Color BoxHoveredColor { get; set; } = Color.FromArgb(255, 70, 70, 70);
	public Color BorderColor { get; set; } = Color.FromArgb(255, 100, 100, 100);
	public Color FocusedBorderColor { get; set; } = Color.FromArgb(255, 100, 150, 255);
	public Color CheckColor { get; set; } = Color.FromArgb(255, 100, 200, 100);
	public Color DisabledColor { get; set; } = Color.FromArgb(255, 30, 30, 30);
	public Color TextColor { get; set; } = Color.White;
	public Color DisabledTextColor { get; set; } = Color.FromArgb(255, 100, 100, 100);
	public int BorderWidth { get; set; } = 1;

	public void Apply(Checkbox element, UIElementState state)
	{
		var boxSize = element.BoxSize;
		var bgColor = state switch
		{
			_ when state.HasFlag(UIElementState.Disabled) => DisabledColor,
			_ when state.HasFlag(UIElementState.Hovered) => BoxHoveredColor,
			_ => BoxColor,
		};
		var borderColor = state.HasFlag(UIElementState.Focused) ? FocusedBorderColor : BorderColor;

		// チェックボックスの外枠
		var box = Shape.CreateRect(
			(0, 0),
			(boxSize, boxSize),
			bgColor,
			BorderWidth,
			borderColor
		);
		element.SetBackgroundNode(box);

		// チェックマーク（チェック時のみ）
		// "checkmark" という名前の子ノードを管理
		var existingCheck = FindCheckmark(element);
		if (element.IsChecked)
		{
			if (existingCheck == null)
			{
				var margin = 4;
				var check = Shape.CreateRect(
					(margin, margin),
					(boxSize - margin, boxSize - margin),
					CheckColor
				).Name("_checkmark");
				element.Insert(1, check);
			}
		}
		else
		{
			if (existingCheck != null)
			{
				element.Remove(existingCheck);
			}
		}

		// テキスト色
		element.Label.Color = state.HasFlag(UIElementState.Disabled) ? DisabledTextColor : TextColor;
	}

	private static Node? FindCheckmark(Checkbox element)
	{
		for (var i = 0; i < element.Count; i++)
		{
			if (element[i].Name == "_checkmark")
				return element[i];
		}
		return null;
	}
}
