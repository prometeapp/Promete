using System.Drawing;
using Promete.Graphics;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.UI.Styles;

/// <summary>
/// NineSliceSprite ベースのカスタムボタンスタイルです。
/// 状態ごとに異なる9スライステクスチャを設定できます。
/// </summary>
public class NineSliceButtonStyle : IUIStyle<Button>
{
	/// <summary>通常状態のテクスチャ。</summary>
	public Texture9Sliced? NormalTexture { get; set; }

	/// <summary>ホバー状態のテクスチャ。null の場合は NormalTexture を使用します。</summary>
	public Texture9Sliced? HoveredTexture { get; set; }

	/// <summary>押下状態のテクスチャ。null の場合は NormalTexture を使用します。</summary>
	public Texture9Sliced? PressedTexture { get; set; }

	/// <summary>無効状態のテクスチャ。null の場合は NormalTexture を使用します。</summary>
	public Texture9Sliced? DisabledTexture { get; set; }

	/// <summary>通常時のテキスト色。</summary>
	public Color TextColor { get; set; } = Color.White;

	/// <summary>無効時のテキスト色。</summary>
	public Color DisabledTextColor { get; set; } = Color.FromArgb(255, 120, 120, 120);

	public void Apply(Button element, UIElementState state)
	{
		var texture = state switch
		{
			_ when state.HasFlag(UIElementState.Disabled) => DisabledTexture ?? NormalTexture,
			_ when state.HasFlag(UIElementState.Pressed) => PressedTexture ?? NormalTexture,
			_ when state.HasFlag(UIElementState.Hovered) => HoveredTexture ?? NormalTexture,
			_ => NormalTexture,
		};

		if (texture is { } tex)
		{
			var sprite = new NineSliceSprite(tex);
			sprite.Width = element.Size.X;
			sprite.Height = element.Size.Y;
			element.SetBackgroundNode(sprite);
		}

		element.Label.Color = state.HasFlag(UIElementState.Disabled) ? DisabledTextColor : TextColor;
	}
}
