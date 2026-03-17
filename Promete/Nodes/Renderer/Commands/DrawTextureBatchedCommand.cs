using System.Collections.Generic;
using Promete.Graphics;

namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// 同テクスチャの <see cref="DrawTextureCommand"/> を集約したバッチ描画コマンドです。
/// <see cref="RenderCommandQueue"/> が <see cref="DrawTextureCommand"/> を受け取った際に
/// 自動的に生成・マージします。
/// </summary>
internal sealed class DrawTextureBatchedCommand : IRenderCommand
{
    /// <summary>バッチ内の全アイテムで共通のテクスチャ。</summary>
    public Texture2D Texture { get; }

    /// <summary>バッチ内の個別描画アイテム。</summary>
    public List<DrawTextureCommand> Items { get; } = [];

    public DrawTextureBatchedCommand(DrawTextureCommand first)
    {
        Texture = first.Texture;
        Items.Add(first);
    }

    /// <summary>同テクスチャのコマンドをバッチに追加します。</summary>
    internal void Add(DrawTextureCommand cmd) => Items.Add(cmd);
}
