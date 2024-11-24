using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;

namespace Promete.Graphics;

/// <summary>
/// テクスチャを生成するファクトリです。
/// </summary>
public abstract class TextureFactory
{
    /// <summary>
    /// 指定したパスからテクスチャを読み込みます。
    /// </summary>
    public abstract Texture2D Load(string path);

    /// <summary>
    /// 指定したストリームからテクスチャを読み込みます。
    /// </summary>
    public abstract Texture2D Load(Stream stream);

    /// <summary>
    /// 指定したパスからテクスチャを読み込み、切り抜きます。
    /// </summary>
    public abstract Texture2D[] LoadSpriteSheet(string path, int horizontalCount, int verticalCount, VectorInt size);

    /// <summary>
    /// 指定したストリームからテクスチャを読み込み、切り抜きます。
    /// </summary>
    public abstract Texture2D[] LoadSpriteSheet(Stream stream, int horizontalCount, int verticalCount, VectorInt size);

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    public abstract Texture2D Create(byte[] bitmap, VectorInt size);

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    public abstract Texture2D Create(byte[,,] bitmap);

    /// <summary>
    /// 指定した色の単色テクスチャを生成します。
    /// </summary>
    public abstract Texture2D CreateSolid(Color color, VectorInt size);

    /// <summary>
    /// [内部的に使用。] ImageSharp の Image からテクスチャを生成します。
    /// </summary>
    internal abstract Texture2D LoadFromImageSharpImage(Image image);

    /// <summary>
    /// 指定したパスから 9 スライステクスチャを読み込みます。
    /// </summary>
    public virtual Texture9Sliced Load9Sliced(string path, int left, int top, int right, int bottom)
    {
        return Load9Sliced(Image.Load(path), left, top, right, bottom);
    }

    /// <summary>
    /// 指定したストリームから 9 スライステクスチャを読み込みます。
    /// </summary>
    public virtual Texture9Sliced Load9Sliced(Stream stream, int left, int top, int right, int bottom)
    {
        return Load9Sliced(Image.Load(stream), left, top, right, bottom);
    }

    protected virtual Texture9Sliced Load9Sliced(Image bitmap, int left, int top, int right, int bottom)
    {
        using var img = bitmap.CloneAs<Rgba32>();
        bitmap.Dispose();

        var size = (img.Width, img.Height);

        if (left > img.Width)
            throw new ArgumentException(null, nameof(left));
        if (top > img.Height)
            throw new ArgumentException(null, nameof(top));
        if (right > img.Width - left)
            throw new ArgumentException(null, nameof(right));
        if (bottom > img.Height - top)
            throw new ArgumentException(null, nameof(bottom));

        var atlas = new[]
        {
            new Rectangle(0, 0, left, top),
            new Rectangle(left, 0, img.Width - left - right, top),
            new Rectangle(img.Width - right, 0, right, top),
            new Rectangle(0, top, left, img.Height - top - bottom),
            new Rectangle(left, top, img.Width - left - right, img.Height - top - bottom),
            new Rectangle(img.Width - right, top, right, img.Height - top - bottom),
            new Rectangle(0, img.Height - bottom, left, bottom),
            new Rectangle(left, img.Height - bottom, img.Width - left - right, bottom),
            new Rectangle(img.Width - right, img.Height - bottom, right, bottom)
        };

        var texture = atlas.Select(rect =>
        {
            using var locked = img.Clone(ctx => ctx.Crop(rect));
            return LoadFromImageSharpImage(locked);
        }).ToArray();

        return new Texture9Sliced(texture, size);
    }
}
