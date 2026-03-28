using System.Drawing;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// Shape ベースのデフォルトモーダルスタイル��す。
/// 半透明オーバーレイとパネル背景を描画します。
/// </summary>
public class DefaultModalStyle : IUIStyle<Modal>
{
	/// <summary>オーバーレイ（背景）の色。</summary>
	public Color OverlayColor { get; set; } = Color.FromArgb(150, 0, 0, 0);

	/// <summary>モーダル本体の背景色。</summary>
	public Color BodyBackgroundColor { get; set; } = Color.FromArgb(255, 50, 50, 50);

	/// <summary>モーダル本体の枠線色。</summary>
	public Color BodyBorderColor { get; set; } = Color.FromArgb(255, 120, 120, 120);

	/// <summary>枠線の幅。</summary>
	public int BorderWidth { get; set; } = 1;

	public void Apply(Modal element, UIElementState state)
	{
		// 全画面オーバーレイ
		var overlay = Shape.CreateRect(
			(0, 0),
			element.Size,
			OverlayColor
		);
		element.SetBackgroundNode(overlay);

		// モーダル本体のスタイル
		element.Body.Style = new DefaultPanelStyle
		{
			BackgroundColor = BodyBackgroundColor,
			BorderColor = BodyBorderColor,
			BorderWidth = BorderWidth,
		};
	}
}
