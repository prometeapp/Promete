using System;
using System.Drawing;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;

namespace Promete.Graphics;

/// <summary>
/// <see cref="Tilemap" /> が扱えるタイルを定義します。
/// </summary>
public interface ITile
{
    /// <summary>
    /// 指定した位置に描画するテクスチャを取得します。
    /// <para>
    /// 既定の実装は <see cref="NotSupportedException" /> をスローします (v2.3~)。
    /// テクスチャを描画するタイルは、このメソッドを実装してください。
    /// <see cref="Collect" /> をオーバーライドして独自に描画するタイルでは、実装は不要です。
    /// </para>
    /// </summary>
    /// <param name="map">このタイルを持つ <see cref="Tilemap" />。</param>
    /// <param name="tileLocation">タイル座標。</param>
    /// <exception cref="NotSupportedException">このタイルはテクスチャを持ちません。</exception>
    public Texture2D GetTexture(Tilemap map, VectorInt tileLocation) =>
        throw new NotSupportedException(
            "このタイルはテクスチャを持ちません。Collect をオーバーライドしてください。"
        );

    /// <summary>
    /// このタイルの描画コマンドをキューに積みます (v2.3~)。
    /// <para>
    /// 既定の実装は、<see cref="GetTexture" /> が返すテクスチャを描画する <see cref="DrawTextureCommand" /> を積みます。
    /// テクスチャ以外を描画したい場合は、このメソッドをオーバーライドしてください。
    /// </para>
    /// </summary>
    /// <param name="queue">描画コマンドを積むキュー。</param>
    /// <param name="map">このタイルを持つ <see cref="Tilemap" />。</param>
    /// <param name="tileLocation">タイル座標。</param>
    /// <param name="tint">タイルに適用する色。</param>
    public void Collect(RenderCommandQueue queue, Tilemap map, VectorInt tileLocation, Color tint)
    {
        queue.Enqueue(
            new DrawTextureCommand
            {
                Texture = GetTexture(map, tileLocation),
                ModelMatrix = map.ModelMatrix,
                TintColor = tint,
                Width = map.TileSize.X,
                Height = map.TileSize.Y,
                Pivot = tileLocation * map.TileSize,
            }
        );
    }

    /// <summary>
    /// この <see cref="ITile" /> を破棄します。
    /// </summary>
    public void Destroy();
}
