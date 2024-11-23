using System;

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

	private readonly Silk.NET.OpenGL.GL? _gl;

	internal Texture2D(int handle, VectorInt size, Silk.NET.OpenGL.GL gl)
	{
		_gl = gl;
		Handle = handle;
		Size = size;
	}

	/// <summary>
	/// この <see cref="Texture2D"/> を破棄します。
	/// </summary>
	public void Dispose()
	{
		_gl?.DeleteTexture((uint)Handle);
	}
}
