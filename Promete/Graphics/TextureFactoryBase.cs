using System;
using System.IO;
using System.Linq;
using Promete.Graphics.Imaging;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace Promete.Graphics;

/// <summary>
/// テクスチャを生成するファクトリです。
/// </summary>
public abstract class TextureFactoryBase
{
    /// <summary>
    /// 指定したパスからテクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public abstract Texture2D Load(string path);

    /// <summary>
    /// 指定したストリームからテクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public abstract Texture2D Load(Stream stream);

    /// <summary>
    /// 指定したパスからテクスチャを読み込み、切り抜きます。
    /// </summary>
    /// <returns></returns>
    public abstract Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    );

    /// <summary>
    /// 指定したストリームからテクスチャを読み込み、切り抜きます。
    /// </summary>
    /// <returns></returns>
    public abstract Texture2D[] LoadSpriteSheet(
        Stream stream,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    );

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    /// <returns></returns>
    public abstract Texture2D Create(byte[] bitmap, VectorInt size);

    /// <summary>
    /// ビットマップのデータからテクスチャを生成します。
    /// </summary>
    /// <returns></returns>
    public abstract Texture2D Create(byte[,,] bitmap);

    /// <summary>
    /// 指定した色の単色テクスチャを生成します。
    /// </summary>
    /// <returns></returns>
    public abstract Texture2D CreateSolid(Color color, VectorInt size);

    /// <summary>
    /// 既存のテクスチャの一部の領域を、指定したビットマップで書き換えます。
    /// グリフアトラスのように、1 枚のテクスチャへ内容を追記していく用途で使用します。
    /// </summary>
    /// <param name="texture">書き換え対象のテクスチャ。</param>
    /// <param name="offset">書き換える領域の左上位置。</param>
    /// <param name="size">書き換える領域のサイズ。</param>
    /// <param name="bitmap">RGBA8888 形式のビットマップデータ。</param>
    public abstract void Update(Texture2D texture, VectorInt offset, VectorInt size, byte[] bitmap);

    /// <summary>
    /// デコード済みの画像からテクスチャを生成します。
    /// </summary>
    internal Texture2D LoadFromImage(RgbaImage image)
    {
        return Create(image.Pixels, (image.Width, image.Height));
    }

    /// <summary>
    /// 指定したパスから 9 スライステクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public virtual Texture9Sliced Load9Sliced(string path, int left, int top, int right, int bottom)
    {
        return Load9Sliced(ImageDecoder.Decode(path), left, top, right, bottom);
    }

    /// <summary>
    /// 指定したストリームから 9 スライステクスチャを読み込みます。
    /// </summary>
    /// <returns></returns>
    public virtual Texture9Sliced Load9Sliced(
        Stream stream,
        int left,
        int top,
        int right,
        int bottom
    )
    {
        return Load9Sliced(ImageDecoder.Decode(stream), left, top, right, bottom);
    }

    private Texture9Sliced Load9Sliced(RgbaImage img, int left, int top, int right, int bottom)
    {
        var size = (img.Width, img.Height);

        if (left > img.Width)
            throw new ArgumentException(null, nameof(left));
        if (top > img.Height)
            throw new ArgumentException(null, nameof(top));
        if (right > img.Width - left)
            throw new ArgumentException(null, nameof(right));
        if (bottom > img.Height - top)
            throw new ArgumentException(null, nameof(bottom));

        var atlas = new Rectangle[]
        {
            new(0, 0, left, top),
            new(left, 0, img.Width - left - right, top),
            new(img.Width - right, 0, right, top),
            new(0, top, left, img.Height - top - bottom),
            new(left, top, img.Width - left - right, img.Height - top - bottom),
            new(img.Width - right, top, right, img.Height - top - bottom),
            new(0, img.Height - bottom, left, bottom),
            new(left, img.Height - bottom, img.Width - left - right, bottom),
            new(img.Width - right, img.Height - bottom, right, bottom),
        };

        var texture = atlas
            .Select(rect => LoadFromImage(img.Crop(rect.X, rect.Y, rect.Width, rect.Height)))
            .ToArray();

        return new Texture9Sliced(texture, size);
    }
}
