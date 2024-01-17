using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;

namespace Promete.Graphics;

public abstract class TextureFactory
{
	public abstract ITexture Load(string path);
	public abstract ITexture Load(Stream stream);
	public abstract ITexture[] LoadSpriteSheet(string path, int horizontalCount, int verticalCount, VectorInt size);
	public abstract ITexture[] LoadSpriteSheet(Stream stream, int horizontalCount, int verticalCount, VectorInt size);
	public abstract ITexture Create(byte[] bitmap, VectorInt size);
	public abstract ITexture Create(byte[,,] bitmap);
	public abstract ITexture CreateSolid(Color color, VectorInt size);
	internal abstract ITexture LoadFromImageSharpImage(Image image);

	public virtual Texture9Sliced Load9Sliced(string path, int left, int top, int right, int bottom)
	{
		return Load9Sliced(Image.Load(path), left, top, right, bottom);
	}

	public virtual Texture9Sliced Load9Sliced(Stream stream, int left, int top, int right, int bottom)
	{
		return Load9Sliced(Image.Load(stream), left, top, right, bottom);
	}

	protected virtual Texture9Sliced Load9Sliced(Image bitmap, int left, int top, int right, int bottom)
	{
		using var img = bitmap.CloneAs<Rgba32>();
		bitmap.Dispose();

		var size = (img.Width, img.Height);

		if (left > img.Width)
			throw new ArgumentException(null, nameof(left));
		if (top > img.Height)
			throw new ArgumentException(null, nameof(top));
		if (right > img.Width - left)
			throw new ArgumentException(null, nameof(right));
		if (bottom > img.Height - top)
			throw new ArgumentException(null, nameof(bottom));

		var atlas = new[]
		{
			new Rectangle(0, 0, left, top),
			new Rectangle(left, 0, img.Width - left - right, top),
			new Rectangle(img.Width - right, 0, right, top),
			new Rectangle(0, top, left, img.Height - top - bottom),
			new Rectangle(left, top, img.Width - left - right, img.Height - top - bottom),
			new Rectangle(img.Width - right, top, right, img.Height - top - bottom),
			new Rectangle(0, img.Height - bottom, left, bottom),
			new Rectangle(left, img.Height - bottom, img.Width - left - right, bottom),
			new Rectangle(img.Width - right, img.Height - bottom, right, bottom),
		};

		var texture = atlas.Select(rect =>
		{
			using var locked = img.Clone(ctx => ctx.Crop(rect));
			return LoadFromImageSharpImage(locked);
		}).ToArray();

		return new Texture9Sliced(texture, size);
	}
}
