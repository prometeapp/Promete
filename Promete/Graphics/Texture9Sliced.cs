using System;

namespace Promete.Graphics;

/// <summary>
/// 9スライステクスチャのハンドルをラップします。
/// </summary>
public readonly struct Texture9Sliced : IDisposable
{
    /// <summary>
    /// 左上部分のテクスチャを取得します。
    /// </summary>
    public Texture2D TopLeft { get; }

    /// <summary>
    /// 上部中央のテクスチャを取得します。
    /// </summary>
    public Texture2D TopCenter { get; }

    /// <summary>
    /// 右上部分のテクスチャを取得します。
    /// </summary>
    public Texture2D TopRight { get; }

    /// <summary>
    /// 左中央部分のテクスチャを取得します。
    /// </summary>
    public Texture2D MiddleLeft { get; }

    /// <summary>
    /// 中央部分のテクスチャを取得します。
    /// </summary>
    public Texture2D MiddleCenter { get; }

    /// <summary>
    /// 右中央部分のテクスチャを取得します。
    /// </summary>
    public Texture2D MiddleRight { get; }

    /// <summary>
    /// 左下部分のテクスチャを取得します。
    /// </summary>
    public Texture2D BottomLeft { get; }

    /// <summary>
    /// 下部中央のテクスチャを取得します。
    /// </summary>
    public Texture2D BottomCenter { get; }

    /// <summary>
    /// 右下部分のテクスチャを取得します。
    /// </summary>
    public Texture2D BottomRight { get; }

    /// <summary>
    /// このテクスチャのサイズを取得します。
    /// </summary>
    public VectorInt Size { get; }

    internal Texture9Sliced(Texture2D[] textures, VectorInt size)
    {
        TopLeft = textures[0];
        TopCenter = textures[1];
        TopRight = textures[2];
        MiddleLeft = textures[3];
        MiddleCenter = textures[4];
        MiddleRight = textures[5];
        BottomLeft = textures[6];
        BottomCenter = textures[7];
        BottomRight = textures[8];
        Size = size;
    }

    /// <summary>
    /// この <see cref="Texture9Sliced" /> を破棄します。
    /// </summary>
    public void Dispose()
    {
        TopLeft.Dispose();
        TopCenter.Dispose();
        TopRight.Dispose();
        MiddleLeft.Dispose();
        MiddleCenter.Dispose();
        MiddleRight.Dispose();
        BottomLeft.Dispose();
        BottomCenter.Dispose();
        BottomRight.Dispose();
    }
}
