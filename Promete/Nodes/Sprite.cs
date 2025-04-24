using System.Drawing;
using Promete.Graphics;

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

    /// <summary>
    /// スプライトのサイズをリセットし、テクスチャのサイズを使用するようにします。
    /// </summary>
    public void ResetSize()
    {
        _size = null;
    }
}
