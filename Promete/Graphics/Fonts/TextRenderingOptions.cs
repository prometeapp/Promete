using System;
using System.Drawing;

namespace Promete.Graphics.Fonts;

/// <summary>
/// テキストを描画する際のオプション。このクラスは継承できません。
/// </summary>
public sealed class TextRenderingOptions : ICloneable
{
	/// <summary>
	/// テキストの色を取得または設定します。
	/// </summary>
	public Color TextColor { get; set; }

	/// <summary>
	/// 境界線の色を取得または設定します。
	/// </summary>
	public Color? BorderColor { get; set; }

	/// <summary>
	/// 境界線の太さを取得または設定します。
	/// </summary>
	public int BorderThickness { get; set; } = 1;

	/// <summary>
	/// 行の高さを取得または設定します。
	/// </summary>
	public float LineSpacing { get; set; } = 1;

	/// <summary>
	/// 文字を自動的に折り返すかどうかを取得または設定します。
	/// </summary>
	public bool WordWrap { get; set; }

	/// <summary>
	/// 縦方向の位置を取得または設定します。
	/// </summary>
	public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

	/// <summary>
	/// 横方向の位置を取得または設定します。
	/// </summary>
	public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

	/// <summary>
	/// レンダリング時のテクスチャデータのサイズを取得または設定します。
	/// (0, 0)を指定した場合は、テキストが収まる範囲に自動整形します。
	/// </summary>
	public VectorInt Size { get; set; }

	/// <summary>
	/// PTML記法を用いたリッチテキストを有効化するかどうかを取得または設定します。
	/// </summary>
	public bool UseRichText { get; set; }

	/// <summary>
	/// アンチエイリアシングを有効化するかどうかを取得また設定します。
	/// </summary>
	public bool UseAntialiasing { get; set; } = true;

	public TextRenderingOptions Clone()
	{
		return new TextRenderingOptions
		{
			TextColor = TextColor,
			BorderColor = BorderColor,
			BorderThickness = BorderThickness,
			LineSpacing = LineSpacing,
			WordWrap = WordWrap,
			VerticalAlignment = VerticalAlignment,
			HorizontalAlignment = HorizontalAlignment,
			Size = Size,
			UseRichText = UseRichText,
			UseAntialiasing = UseAntialiasing,
		};
	}

	object ICloneable.Clone() => Clone();
}
