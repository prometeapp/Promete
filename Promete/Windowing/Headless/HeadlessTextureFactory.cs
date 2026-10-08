using System;
using Promete.Graphics;

namespace Promete.Windowing.Headless;

public class HeadlessTextureFactory : TextureFactoryBase
{
    private int _nextHandle = 1;

    protected override int UploadTexture(in TextureUploadRequest request)
    {
        return _nextHandle++;
    }

    protected override void UpdateTexture(
        int handle,
        VectorInt offset,
        VectorInt size,
        ReadOnlySpan<byte> rgba
    ) { }

    protected override void DestroyTexture(int handle) { }
}
