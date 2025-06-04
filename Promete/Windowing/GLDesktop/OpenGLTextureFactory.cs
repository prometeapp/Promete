using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Promete.Graphics;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;

namespace Promete.Windowing.GLDesktop;

public class OpenGLTextureFactory(GL gl, PrometeApp app) : TextureFactory
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
        return new Texture2D(GenerateTexture(bitmap, (uint)size.X, (uint)size.Y), size, DisposeTexture);
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
            for (var x = 0; x < horizontalCount; x++)
            {
                var (px, py) = (x * size.X, y * size.Y);
                if (px + size.X > img.Width) throw new ArgumentException(null, nameof(horizontalCount));

                if (py + size.Y > img.Height) throw new ArgumentException(null, nameof(verticalCount));

                using var cropped = img.Clone(ctx =>
                    ctx.Crop(new Rectangle(px, py, size.X, size.Y)));
                textures[y * horizontalCount + x] = LoadFromImageSharpImage(cropped);
            }

            return textures.ToArray();
        }
    }

    private unsafe int GenerateTexture(byte[] bitmap, uint width, uint height)
    {
        app.ThrowIfNotMainThread();
        fixed (byte* b = bitmap)
        {
            var texture = gl.GenTexture();
            gl.ActiveTexture(GLEnum.Texture0);
            gl.BindTexture(GLEnum.Texture2D, texture);

            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, width, height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, b);
            return (int)texture;
        }
    }

    private void DisposeTexture(Texture2D texture)
    {
        app.ThrowIfNotMainThread();
        gl.DeleteTexture((uint)texture.Handle);
    }
}
