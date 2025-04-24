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

    private readonly Action<Texture2D> onDispose;

    internal Texture2D(int handle, VectorInt size, Action<Texture2D> onDispose)
    {
        Handle = handle;
        Size = size;
        this.onDispose = onDispose;
    }

    /// <summary>
    /// この <see cref="Texture2D" /> を破棄します。
    /// </summary>
    public void Dispose()
    {
        onDispose?.Invoke(this);
    }
}
