using System.Drawing;
using Promete.Graphics;

namespace Promete.Nodes;

/// <summary>
/// 9スライス方式でテクスチャを描画するノードです。
/// </summary>
public class NineSliceSprite(Texture9Sliced texture, Color? tintColor = default) : Node
{
    /// <summary>
    /// テクスチャを取得または設定します。
    /// </summary>
    public Texture9Sliced Texture { get; set; } = texture;

    /// <summary>
    /// 色合いを取得または設定します。
    /// </summary>
    public Color TintColor { get; set; } = tintColor ?? Color.White;
}
