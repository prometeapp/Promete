using System;
using System.Collections.Generic;
using Promete.Internal;

namespace Promete.Graphics;

/// <summary>
/// フレームバッファを管理するクラスです。
/// </summary>
public class FrameBufferManager
{
    internal HashSet<FrameBuffer> ActiveFrameBuffers { get; } = [];

    private readonly IFrameBufferProvider? _frameBufferProvider;


    public FrameBufferManager(PrometeApp app)
    {
        _frameBufferProvider = app.TryGetPlugin<IFrameBufferProvider>(out var provider) ? provider : null;
        if (_frameBufferProvider == null)
        {
            LogHelper.Warn("FrameBuffer is not supported on this backend.");
            return;
        }

        app.Window.Render += RenderAll;
        app.Window.Update += UpdateAll;
    }

    private void RenderAll()
    {
        if (_frameBufferProvider == null) return;

        foreach (var frameBuffer in ActiveFrameBuffers)
        {
            frameBuffer.BeforeRender();
            _frameBufferProvider.Render(frameBuffer);
        }
    }

    private void UpdateAll()
    {
        if (_frameBufferProvider == null) return;

        foreach (var frameBuffer in ActiveFrameBuffers)
        {
            frameBuffer.Update();
        }
    }
}

