using System;

namespace Promete.Graphics;

/// <summary>
/// Wrap a handles of 9-sliced textures.
/// </summary>
public readonly struct Texture9Sliced : IDisposable
{
	public ITexture TopLeft { get; }
	public ITexture TopCenter { get; }
	public ITexture TopRight { get; }
	public ITexture MiddleLeft { get; }
	public ITexture MiddleCenter { get; }
	public ITexture MiddleRight { get; }
	public ITexture BottomLeft { get; }
	public ITexture BottomCenter { get; }
	public ITexture BottomRight { get; }

	public VectorInt Size { get; }

	internal Texture9Sliced(ITexture[] textures, VectorInt size)
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
	/// Destroy this <see cref="Texture9Sliced"/>.
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
