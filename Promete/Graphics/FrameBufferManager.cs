using System.Collections.Generic;
using Promete.Internal;

namespace Promete.Graphics;

/// <summary>
/// フレームバッファを管理するクラスです。
/// </summary>
public class FrameBufferManager
{
    internal HashSet<FrameBuffer> ActiveFrameBuffers { get; } = [];

    public FrameBufferManager(PrometeApp app)
    {
        app.Render += RenderAll;
        app.Update += UpdateAll;
    }

    private void RenderAll()
    {
        foreach (var frameBuffer in ActiveFrameBuffers)
        {
            frameBuffer.BeforeRender();
            if (frameBuffer.AutoRender) frameBuffer.Render();
        }
    }

    private void UpdateAll()
    {
        foreach (var frameBuffer in ActiveFrameBuffers)
        {
            frameBuffer.Update();
        }
    }
}
