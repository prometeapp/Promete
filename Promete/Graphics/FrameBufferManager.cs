using System.Collections.Generic;

namespace Promete.Graphics;

/// <summary>
/// フレームバッファを管理するクラスです。
/// </summary>
public class FrameBufferManager
{
    public FrameBufferManager(PrometeApp app)
    {
        app.Render += RenderAll;
        app.Update += UpdateAll;
    }

    internal HashSet<FrameBuffer> ActiveFrameBuffers { get; } = [];

    private void RenderAll()
    {
        foreach (var frameBuffer in ActiveFrameBuffers)
        {
            frameBuffer.BeforeRender();
            if (frameBuffer.AutoRender)
                frameBuffer.Render();
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
