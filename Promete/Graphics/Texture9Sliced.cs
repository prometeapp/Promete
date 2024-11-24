using System;

namespace Promete.Graphics;

/// <summary>
///     Wrap a handles of 9-sliced textures.
/// </summary>
public readonly struct Texture9Sliced : IDisposable
{
    public Texture2D TopLeft { get; }
    public Texture2D TopCenter { get; }
    public Texture2D TopRight { get; }
    public Texture2D MiddleLeft { get; }
    public Texture2D MiddleCenter { get; }
    public Texture2D MiddleRight { get; }
    public Texture2D BottomLeft { get; }
    public Texture2D BottomCenter { get; }
    public Texture2D BottomRight { get; }

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
    ///     Destroy this <see cref="Texture9Sliced" />.
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
