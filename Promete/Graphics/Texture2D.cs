using System;
using Silk.NET.OpenGL;

namespace Promete.Graphics;

/// <summary>
/// 2Dテクスチャのハンドルをラップします。
/// </summary>
public readonly struct Texture2D : IDisposable
{
    /// <summary>
    /// このテクスチャのOpenGLハンドルを取得します。
    /// </summary>
    public int Handle { get; }

    /// <summary>
    /// このテクスチャのサイズを取得します。
    /// </summary>
    public VectorInt Size { get; }

    /// <summary>
    /// このテクスチャの左上のUV座標を取得します。
    /// </summary>
    public Vector UvStart { get; }

    /// <summary>
    /// このテクスチャの右下のUV座標を取得します。
    /// </summary>
    public Vector UvEnd { get; }

    private readonly Action<Texture2D> _onDispose;

    internal Texture2D(int handle, VectorInt size, Action<Texture2D> onDispose)
        : this(handle, size, onDispose, (0, 0), (1, 1))
    {
    }

    internal Texture2D(int handle, VectorInt size, Action<Texture2D> onDispose, Vector uvStart, Vector uvEnd)
    {
        Handle = handle;
        Size = size;
        _onDispose = onDispose;
        UvStart = uvStart;
        UvEnd = uvEnd;
    }

    /// <summary>
    /// この <see cref="Texture2D" /> を破棄します。
    /// </summary>
    public void Dispose()
    {
        _onDispose?.Invoke(this);
    }
}
