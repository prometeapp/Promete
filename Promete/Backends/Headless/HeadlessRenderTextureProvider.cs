using System;
using System.Drawing;
using Promete.Graphics;

namespace Promete.Backends.Headless;

public class HeadlessRenderTextureProvider : IRenderTextureProvider
{
    public RenderTexture Create(VectorInt size)
        => new RenderTexture(size, default, this);

    public IDisposable BeginCapture(RenderTexture renderTexture, Color? clearColor = null)
        => NullDisposable.Instance;

    public void Resize(RenderTexture renderTexture, VectorInt newSize)
        => renderTexture.Texture = default;

    public void Release(RenderTexture renderTexture) { }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
