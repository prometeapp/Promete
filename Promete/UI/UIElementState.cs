using System;

namespace Promete.UI;

/// <summary>
/// UI要素のインタラクション状態を表すフラグ列挙体です。
/// </summary>
[Flags]
public enum UIElementState
{
	/// <summary>通常状態。</summary>
	Normal = 0,

	/// <summary>マウスカーソルが要素上にある状態。</summary>
	Hovered = 1 << 0,

	/// <summary>要素が押下されている状態。</summary>
	Pressed = 1 << 1,

	/// <summary>要素にフォーカスがある状態。</summary>
	Focused = 1 << 2,

	/// <summary>要素が無効化されている状態。</summary>
	Disabled = 1 << 3,
}
