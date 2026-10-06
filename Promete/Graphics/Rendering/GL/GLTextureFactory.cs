using System;
using System.IO;
using Promete.Graphics;
using Promete.Graphics.Imaging;
using Silk.NET.OpenGL;
using Color = System.Drawing.Color;

namespace Promete.Windowing.GLDesktop;

public class GLTextureFactory(PrometeApp app) : TextureFactoryBase
{
    public GL? GL { get; set; }

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
        return new Texture2D(
            GenerateTexture(bitmap, (uint)size.X, (uint)size.Y),
            size,
            DisposeTexture
        );
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

    public override unsafe void Update(
        Texture2D texture,
        VectorInt offset,
        VectorInt size,
        byte[] bitmap
    )
    {
        if (size.X <= 0 || size.Y <= 0)
            return;

        app.ThrowIfNotMainThread();
        fixed (byte* b = bitmap)
        {
            GL.ActiveTexture(GLEnum.Texture0);
            GL.BindTexture(GLEnum.Texture2D, (uint)texture.Handle);
            GL.TexSubImage2D(
                GLEnum.Texture2D,
                0,
                offset.X,
                offset.Y,
                (uint)size.X,
                (uint)size.Y,
                GLEnum.Rgba,
                GLEnum.UnsignedByte,
                b
            );
        }
    }

    private unsafe int GenerateTexture(byte[] bitmap, uint width, uint height)
    {
        app.ThrowIfNotMainThread();
        fixed (byte* b = bitmap)
        {
            var texture = GL.GenTexture();
            GL.ActiveTexture(GLEnum.Texture0);
            GL.BindTexture(GLEnum.Texture2D, texture);

            GL.TexParameter(
                GLEnum.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest
            );
            GL.TexParameter(
                GLEnum.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest
            );
            GL.TexParameter(
                GLEnum.Texture2D,
                TextureParameterName.TextureWrapS,
                (int)GLEnum.ClampToEdge
            );
            GL.TexParameter(
                GLEnum.Texture2D,
                TextureParameterName.TextureWrapT,
                (int)GLEnum.ClampToEdge
            );
            GL.TexImage2D(
                GLEnum.Texture2D,
                0,
                (int)GLEnum.Rgba,
                width,
                height,
                0,
                GLEnum.Rgba,
                GLEnum.UnsignedByte,
                b
            );
            return (int)texture;
        }
    }

    private void DisposeTexture(Texture2D texture)
    {
        app.ThrowIfNotMainThread();
        GL.DeleteTexture((uint)texture.Handle);
    }
}
