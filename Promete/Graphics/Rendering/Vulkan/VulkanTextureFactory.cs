using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan バックエンドにおける <see cref="TextureFactoryBase"/> の実装です。
/// </summary>
internal sealed class VulkanTextureFactory(PrometeApp app, VulkanResourceManager resources)
    : TextureFactoryBase
{
    public override Texture2D Load(string path)
    {
        return LoadFromImageSharpImage(Image.Load(path));
    }

    public override Texture2D Load(Stream stream)
    {
        return LoadFromImageSharpImage(Image.Load(stream));
    }

    public override Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        return LoadSpriteSheet(Image.Load(path), horizontalCount, verticalCount, size);
    }

    public override Texture2D[] LoadSpriteSheet(
        Stream stream,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        return LoadSpriteSheet(Image.Load(stream), horizontalCount, verticalCount, size);
    }

    public override Texture2D Create(byte[] bitmap, VectorInt size)
    {
        app.ThrowIfNotMainThread();
        var id = resources.CreateTexture(bitmap, (uint)size.X, (uint)size.Y);
        return new Texture2D(id, size, DisposeTexture);
    }

    public override Texture2D Create(byte[,,] bitmap)
    {
        var width = bitmap.GetLength(0);
        var height = bitmap.GetLength(1);
        var arr = new byte[width * height * 4];
        for (int y = 0, i = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        for (var j = 0; j < 4; j++)
            arr[i++] = bitmap[x, y, j];

        return Create(arr, (width, height));
    }

    public override Texture2D CreateSolid(Color color, VectorInt size)
    {
        var arr = new byte[size.X * size.Y * 4];
        for (var i = 0; i < arr.Length; i += 4)
        {
            arr[i + 0] = color.R;
            arr[i + 1] = color.G;
            arr[i + 2] = color.B;
            arr[i + 3] = color.A;
        }

        return Create(arr, size);
    }

    internal override Texture2D LoadFromImageSharpImage(Image image)
    {
        using var img = image.CloneAs<Rgba32>();

        var rgbaBytes = MemoryMarshal
            .AsBytes(img.GetPixelMemoryGroup().ToArray()[0].Span)
            .ToArray();
        image.Dispose();
        return Create(rgbaBytes, (img.Width, img.Height));
    }

    private Texture2D[] LoadSpriteSheet(
        Image bmp,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        var width = (float)bmp.Width;
        var height = (float)bmp.Height;
        var handle = LoadFromImageSharpImage(bmp).Handle;

        var textures = new Texture2D[verticalCount * horizontalCount];
        for (var y = 0; y < verticalCount; y++)
        {
            for (var x = 0; x < horizontalCount; x++)
            {
                var px = x * size.X;
                var py = y * size.Y;

                if (px + size.X > width)
                    throw new ArgumentException(null, nameof(horizontalCount));
                if (py + size.Y > height)
                    throw new ArgumentException(null, nameof(verticalCount));

                var uvStart = new Vector(px / width, py / height);
                var uvEnd = new Vector((px + size.X) / width, (py + size.Y) / height);

                textures[(y * horizontalCount) + x] = new Texture2D(
                    handle,
                    size,
                    DisposeTexture,
                    uvStart,
                    uvEnd
                );
            }
        }

        bmp.Dispose();
        return textures;
    }

    private void DisposeTexture(Texture2D texture)
    {
        app.ThrowIfNotMainThread();
        resources.Destroy(texture.Handle);
    }
}
