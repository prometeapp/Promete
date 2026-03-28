using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトテキスト入力スタイルです。
/// </summary>
public class DefaultTextInputStyle : IUIStyle<TextInput>
{
	public Color BackgroundColor { get; set; } = Color.FromArgb(255, 30, 30, 30);
	public Color BorderColor { get; set; } = Color.FromArgb(255, 100, 100, 100);
	public Color FocusedBorderColor { get; set; } = Color.FromArgb(255, 100, 150, 255);
	public Color DisabledBackgroundColor { get; set; } = Color.FromArgb(255, 20, 20, 20);
	public Color TextColor { get; set; } = Color.White;
	public Color PlaceholderColor { get; set; } = Color.FromArgb(255, 120, 120, 120);
	public Color DisabledTextColor { get; set; } = Color.FromArgb(255, 80, 80, 80);
	public Color CursorColor { get; set; } = Color.White;
	public int BorderWidth { get; set; } = 1;
	public int CursorWidth { get; set; } = 2;

	public void Apply(TextInput element, UIElementState state)
	{
		var isDisabled = state.HasFlag(UIElementState.Disabled);
		var isFocused = state.HasFlag(UIElementState.Focused);

		// 背景
		var bgColor = isDisabled ? DisabledBackgroundColor : BackgroundColor;
		var borderColor = isFocused ? FocusedBorderColor : BorderColor;
		var bg = Shape.CreateRect(
			(0, 0),
			element.Size,
			bgColor,
			BorderWidth,
			borderColor
		);
		element.SetBackgroundNode(bg);

		// テキスト色
		element.TextNode.Color = isDisabled ? DisabledTextColor : TextColor;
		element.PlaceholderNode.Color = PlaceholderColor;

		// カーソル
		var existingCursor = FindNamedChild(element, "_cursor");
		if (existingCursor != null) element.Remove(existingCursor);

		if (element.IsCursorVisible)
		{
			// カーソル位置をテキストの文字幅から計算
			var cursorX = element.PaddingLeft + EstimateCursorX(element);
			var cursorY = 4;
			var cursorH = element.Size.Y - 8;

			var cursor = Shape.CreateRect(
				(cursorX, cursorY),
				(cursorX + CursorWidth, cursorY + cursorH),
				CursorColor
			).Name("_cursor");
			element.Add(cursor);
		}
	}

	private static int EstimateCursorX(TextInput element)
	{
		// テキストノードのサイズから1文字あたりの幅を推定
		var text = element.Text;
		if (string.IsNullOrEmpty(text) || element.CursorPosition == 0)
			return 0;

		var textWidth = element.TextNode.Size.X;
		if (textWidth <= 0 || text.Length == 0)
			return 0;

		var charWidth = textWidth / text.Length;
		return (int)(charWidth * element.CursorPosition);
	}

	private static Node? FindNamedChild(TextInput element, string name)
	{
		for (var i = 0; i < element.Count; i++)
		{
			if (element[i].Name == name)
				return element[i];
		}
		return null;
	}
}
