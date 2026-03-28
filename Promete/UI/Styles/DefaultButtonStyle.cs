using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトボタンスタイルです。
/// テクスチャ不要で、各状態に応じた矩形と枠線を描画します。
/// </summary>
public class DefaultButtonStyle : IUIStyle<Button>
{
	/// <summary>通常状態の背景色。</summary>
	public Color NormalColor { get; set; } = Color.FromArgb(255, 60, 60, 60);

	/// <summary>ホバー状態の背景色。</summary>
	public Color HoveredColor { get; set; } = Color.FromArgb(255, 80, 80, 80);

	/// <summary>押下状態の背景色。</summary>
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

	/// <summary>無効状態のテキスト��。</summary>
	public Color DisabledTextColor { get; set; } = Color.FromArgb(255, 100, 100, 100);

	public void Apply(Button element, UIElementState state)
	{
		var bgColor = state switch
		{
			_ when state.HasFlag(UIElementState.Disabled) => DisabledColor,
			_ when state.HasFlag(UIElementState.Pressed) => PressedColor,
			_ when state.HasFlag(UIElementState.Hovered) => HoveredColor,
			_ => NormalColor,
		};

		var borderColor = state.HasFlag(UIElementState.Focused) ? FocusedBorderColor : BorderColor;

		var bg = Shape.CreateRect(
			(0, 0),
			element.Size,
			bgColor,
			BorderWidth,
			borderColor
		);
		element.SetBackgroundNode(bg);

		// テキスト色の更新
		if (element.Label != null)
		{
			element.Label.Color = state.HasFlag(UIElementState.Disabled) ? DisabledTextColor : TextColor;
		}
	}
}
