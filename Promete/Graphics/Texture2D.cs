using System;
using Silk.NET.OpenGL;

namespace Promete.Graphics;

/// <summary>
/// Wrap a handle of 2D texture.
/// </summary>
public readonly struct Texture2D : IDisposable
{
    /// <summary>
    /// Get a OpenGL handle of this texture.
    /// </summary>
    public int Handle { get; }

    /// <summary>
    /// Get size of this texture.
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
