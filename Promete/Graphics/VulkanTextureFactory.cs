using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Promete.Graphics;

public class VulkanTextureFactory(PrometeApp app) : TextureFactory
{
	public override Texture2D Load(string path)
	{
		return LoadFromImageSharpImage(Image.Load(path));
	}

	public override Texture2D Load(Stream stream)
	{
		return LoadFromImageSharpImage(Image.Load(stream));
	}

	public override Texture2D[] LoadSpriteSheet(string path, int horizontalCount, int verticalCount, VectorInt size)
	{
		return LoadSpriteSheet(Image.Load(path), horizontalCount, verticalCount, size);
	}

	public override Texture2D[] LoadSpriteSheet(Stream stream, int horizontalCount, int verticalCount, VectorInt size)
	{
		return LoadSpriteSheet(Image.Load(stream), horizontalCount, verticalCount, size);
	}

	public override Texture2D Create(byte[] bitmap, VectorInt size)
	{
		return new Texture2D(GenerateTexture(bitmap, (uint)size.X, (uint)size.Y), size, this);
	}

	public override Texture2D Create(byte[,,] bitmap)
	{
		var width = bitmap.GetLength(0);
		var height = bitmap.GetLength(1);
		var arr = new byte[width * height * 4];
		for (int y = 0, i = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			for (var j = 0; j < 4; j++)
				arr[i++] = bitmap[x, y, j];
		}

		return Create(arr, (width, height));
	}

	public override void Delete(Texture2D texture)
	{
		// TODO: 実装する
	}

	public override Texture2D CreateSolid(System.Drawing.Color color, VectorInt size)
	{
		var arr = new byte[size.X, size.Y, 4];

		for (var y = 0; y < size.Y; y++)
		for (var x = 0; x < size.X; x++)
		{
			arr[x, y, 0] = color.R;
			arr[x, y, 1] = color.G;
			arr[x, y, 2] = color.B;
			arr[x, y, 3] = color.A;
		}

		return Create(arr);
	}

	internal override Texture2D LoadFromImageSharpImage(Image image)
	{
		using var img = image.CloneAs<Rgba32>();

		var rgbaBytes = MemoryMarshal.AsBytes(img.GetPixelMemoryGroup().ToArray()[0].Span).ToArray();
		image.Dispose();
		return Create(rgbaBytes, (img.Width, img.Height));
	}

	private Texture2D[] LoadSpriteSheet(Image bmp, int horizontalCount, int verticalCount, VectorInt size)
	{
		using (bmp)
		using (var img = bmp.CloneAs<Rgba32>())
		{
			var textures = new Texture2D[verticalCount * horizontalCount];

			for (var y = 0; y < verticalCount; y++)
			{
				for (var x = 0; x < horizontalCount; x++)
				{
					var (px, py) = (x * size.X, y * size.Y);
					if (px + size.X > img.Width)
					{
						throw new ArgumentException(null, nameof(horizontalCount));
					}

					if (py + size.Y > img.Height)
					{
						throw new ArgumentException(null, nameof(verticalCount));
					}

					using var cropped = img.Clone(ctx =>
						ctx.Crop(new Rectangle(px, py, size.X, size.Y)));
					textures[y * horizontalCount + x] = LoadFromImageSharpImage(cropped);
				}
			}

			return textures.ToArray();
		}
	}

	private unsafe int GenerateTexture(byte[] bitmap, uint width, uint height)
	{
		app.ThrowIfNotMainThread();
		// TODO: 実装する
		return 0;
	}
}
