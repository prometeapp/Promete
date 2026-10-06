using System;
using System.IO;
using Promete.Graphics.Imaging;
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
        return LoadFromImage(ImageDecoder.Decode(path));
    }

    public override Texture2D Load(Stream stream)
    {
        return LoadFromImage(ImageDecoder.Decode(stream));
    }

    public override Texture2D[] LoadSpriteSheet(
        string path,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        return LoadSpriteSheet(ImageDecoder.Decode(path), horizontalCount, verticalCount, size);
    }

    public override Texture2D[] LoadSpriteSheet(
        Stream stream,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        return LoadSpriteSheet(ImageDecoder.Decode(stream), horizontalCount, verticalCount, size);
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

    public override void Update(Texture2D texture, VectorInt offset, VectorInt size, byte[] bitmap)
    {
        if (size.X <= 0 || size.Y <= 0)
            return;

        app.ThrowIfNotMainThread();
        resources.UpdateTexture(
            texture.Handle,
            offset.X,
            offset.Y,
            (uint)size.X,
            (uint)size.Y,
            bitmap
        );
    }

    private Texture2D[] LoadSpriteSheet(
        RgbaImage bmp,
        int horizontalCount,
        int verticalCount,
        VectorInt size
    )
    {
        var width = (float)bmp.Width;
        var height = (float)bmp.Height;
        var handle = LoadFromImage(bmp).Handle;

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

        return textures;
    }

    private void DisposeTexture(Texture2D texture)
    {
        app.ThrowIfNotMainThread();
        resources.Destroy(texture.Handle);
    }
}
