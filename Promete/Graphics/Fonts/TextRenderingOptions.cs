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
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// 縁取りの色を取得または設定します。
    /// </summary>
    public Color? BorderColor { get; set; }

    /// <summary>
    /// 縁取りの太さを取得または設定します。
    /// </summary>
    public int BorderThickness { get; set; } = 1;

    /// <summary>
    /// 行の高さの倍率を取得または設定します。
    /// </summary>
    public float LineSpacing { get; set; } = 1;

    /// <summary>
    /// 文字間に追加する余白を取得または設定します。
    /// </summary>
    public float LetterSpacing { get; set; }

    /// <summary>
    /// カーニングを適用するかどうかを取得または設定します。
    /// </summary>
    public bool UseKerning { get; set; } = true;

    /// <summary>
    /// テキストを折り返す方法を取得または設定します。
    /// </summary>
    public WrapMode WrapMode { get; set; } = WrapMode.None;

    /// <summary>
    /// 縦方向の位置を取得または設定します。
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <summary>
    /// 横方向の位置を取得または設定します。
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// テキストを配置する領域のサイズを取得または設定します。
    /// (0, 0) を指定した場合は、テキストが収まる範囲に自動整形します。
    /// </summary>
    public VectorInt Size { get; set; }

    /// <summary>
    /// PTML記法を用いたリッチテキストを有効化するかどうかを取得または設定します。
    /// </summary>
    public bool UseRichText { get; set; }

    object ICloneable.Clone()
    {
        return Clone();
    }

    public TextRenderingOptions Clone()
    {
        return new TextRenderingOptions
        {
            TextColor = TextColor,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness,
            LineSpacing = LineSpacing,
            LetterSpacing = LetterSpacing,
            UseKerning = UseKerning,
            WrapMode = WrapMode,
            VerticalAlignment = VerticalAlignment,
            HorizontalAlignment = HorizontalAlignment,
            Size = Size,
            UseRichText = UseRichText,
        };
    }
}
