using System.Drawing;
using Promete.Graphics;
using Promete.Nodes.Renderer;
using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes;

/// <summary>
/// テクスチャを表示するスプライトクラスです。
/// </summary>
public class Sprite(Texture2D? texture = null, Color? tintColor = default) : Node
{
    private VectorInt? _size;

    /// <summary>
    /// スプライトに使用するテクスチャを取得または設定します。
    /// </summary>
    public Texture2D? Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            UpdateModelMatrix();
        }
    }

    /// <summary>
    /// スプライトの色調を取得または設定します。
    /// </summary>
    public Color TintColor { get; set; } = tintColor ?? Color.White;

    /// <summary>
    /// スプライトのサイズを取得または設定します。
    /// </summary>
    public override VectorInt Size
    {
        get => _size ?? Texture?.Size ?? (0, 0);
        set => _size = value;
    }

    private Texture2D? _texture = texture;

    internal override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        if (Texture is not { } tex) return;

        queue.Enqueue(new DrawTextureCommand
        {
            Texture = tex,
            ModelMatrix = ModelMatrix,
            TintColor = TintColor,
            Width = Size.X,
            Height = Size.Y,
            Material = Material,
        });
    }

    /// <summary>
    /// スプライトのサイズをリセットし、テクスチャのサイズを使用するようにします。
    /// </summary>
    public void ResetSize()
    {
        _size = null;
    }
}
