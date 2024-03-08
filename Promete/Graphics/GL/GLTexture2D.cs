using System;
using System.Numerics;

namespace Promete.Graphics.GL;

/// <summary>
/// Wrap a handle of 2D texture.
/// </summary>
public class GLTexture2D : ITexture
{
	/// <summary>
	/// Get a OpenGL handle of this texture.
	/// </summary>
	public int Handle { get; }

	/// <summary>
	/// Get size of this texture.
	/// </summary>
	public Vector2 Size { get; }

	/// <summary>
	/// Get whether this texture has been destroyed.
	/// </summary>
	public bool IsDisposed { get; private set; }

	private readonly Silk.NET.OpenGL.GL gl;

	internal GLTexture2D(int handle, Vector2 size, Silk.NET.OpenGL.GL gl)
	{
		this.gl = gl;
		Handle = handle;
		Size = size;
	}

	/// <summary>
	/// この <see cref="GLTexture2D"/> を破棄します。
	/// </summary>
	public void Dispose()
	{
		if (IsDisposed) return;

		gl.DeleteTexture((uint)Handle);
		IsDisposed = true;
		GC.SuppressFinalize(this);
	}
}
