using System;
using Promete.Graphics;
using Silk.NET.OpenGL;

namespace Promete.Windowing.GLDesktop;

public class GLTextureFactory(PrometeApp app) : TextureFactoryBase
{
    public GL? GL { get; set; }

    protected override unsafe int UploadTexture(ReadOnlySpan<byte> rgba, VectorInt size)
    {
        app.ThrowIfNotMainThread();
        fixed (byte* b = rgba)
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
                (uint)size.X,
                (uint)size.Y,
                0,
                GLEnum.Rgba,
                GLEnum.UnsignedByte,
                b
            );
            return (int)texture;
        }
    }

    protected override unsafe void UpdateTexture(
        int handle,
        VectorInt offset,
        VectorInt size,
        ReadOnlySpan<byte> rgba
    )
    {
        app.ThrowIfNotMainThread();
        fixed (byte* b = rgba)
        {
            GL.ActiveTexture(GLEnum.Texture0);
            GL.BindTexture(GLEnum.Texture2D, (uint)handle);
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

    protected override void DestroyTexture(int handle)
    {
        app.ThrowIfNotMainThread();
        GL.DeleteTexture((uint)handle);
    }
}
