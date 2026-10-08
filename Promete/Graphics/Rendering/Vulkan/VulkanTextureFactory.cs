using System;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan バックエンドにおける <see cref="TextureFactoryBase"/> の実装です。
/// </summary>
internal sealed class VulkanTextureFactory(PrometeApp app, VulkanResourceManager resources)
    : TextureFactoryBase
{
    protected override int UploadTexture(ReadOnlySpan<byte> rgba, VectorInt size)
    {
        app.ThrowIfNotMainThread();
        return resources.CreateTexture(rgba, (uint)size.X, (uint)size.Y);
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
