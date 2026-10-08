using System;
using System.Threading.Tasks;
using Promete.Graphics;
using Silk.NET.OpenGL;

namespace Promete.Windowing.GLDesktop;

public class GLTextureFactory(PrometeApp app) : TextureFactoryBase
{
    public GL? GL { get; set; }

    protected override unsafe int UploadTexture(in TextureUploadRequest request)
    {
        app.ThrowIfNotMainThread();
        var filter = request.Options.Filter switch
        {
            TextureFilterMode.Linear => GLEnum.Linear,
            _ => GLEnum.Nearest,
        };
        var wrap = request.Options.Address switch
        {
            TextureAddressMode.Repeat => GLEnum.Repeat,
            TextureAddressMode.Mirror => GLEnum.MirroredRepeat,
            _ => GLEnum.ClampToEdge,
        };

        fixed (byte* b = request.Rgba)
        {
            var texture = GL.GenTexture();
            GL.ActiveTexture(GLEnum.Texture0);
            GL.BindTexture(GLEnum.Texture2D, texture);

            GL.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
            GL.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
            GL.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
            GL.TexParameter(GLEnum.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);
            GL.TexImage2D(
                GLEnum.Texture2D,
                0,
                (int)GLEnum.Rgba,
                (uint)request.Size.X,
                (uint)request.Size.Y,
                0,
                GLEnum.Rgba,
                GLEnum.UnsignedByte,
                b
            );
            return (int)texture;
        }
    }

    protected override Task<int> UploadTextureAsync(
        byte[] rgba,
        VectorInt size,
        TextureOptions options
    )
    {
        return app.InvokeOnMainThreadAsync(
            () => UploadTexture(new TextureUploadRequest(rgba, size, options))
        );
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
