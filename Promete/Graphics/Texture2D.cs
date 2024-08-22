using System;

namespace Promete.Graphics;

/// <summary>
/// Wrap a handle of 2D texture.
/// </summary>
public readonly struct Texture2D : IDisposable
{
	/// <summary>
	/// Get a handle of this texture.
	/// </summary>
	public int Handle { get; }

	/// <summary>
	/// Get size of this texture.
	/// </summary>
	public VectorInt Size { get; }

	private readonly TextureFactory factory;

	internal Texture2D(int handle, VectorInt size, TextureFactory factory)
	{
		this.factory = factory;
		Handle = handle;
		Size = size;
	}

	/// <summary>
	/// この <see cref="Texture2D"/> を破棄します。
	/// </summary>
	public void Dispose()
	{
		factory.Delete(this);
	}
}
