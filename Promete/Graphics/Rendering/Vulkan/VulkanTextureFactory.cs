using System;
using System.Threading.Tasks;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan バックエンドにおける <see cref="TextureFactoryBase"/> の実装です。
/// </summary>
internal sealed class VulkanTextureFactory(PrometeApp app, VulkanResourceManager resources)
    : TextureFactoryBase
{
    protected override int UploadTexture(in TextureUploadRequest request)
    {
        app.ThrowIfNotMainThread();
        return resources.CreateTexture(
            request.Rgba,
            (uint)request.Size.X,
            (uint)request.Size.Y,
            request.Options
        );
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

    protected override void UpdateTexture(
        int handle,
        VectorInt offset,
        VectorInt size,
        ReadOnlySpan<byte> rgba
    )
    {
        app.ThrowIfNotMainThread();
        resources.UpdateTexture(handle, offset.X, offset.Y, (uint)size.X, (uint)size.Y, rgba);
    }

    protected override void DestroyTexture(int handle)
    {
        app.ThrowIfNotMainThread();
        resources.Destroy(handle);
    }
}
