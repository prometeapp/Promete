namespace Promete.Graphics;

public interface IFrameBufferProvider
{
    Texture2D CreateTexture(FrameBuffer frameBuffer);

    void Resize(FrameBuffer frameBuffer);

    /// <summary>
    /// フレームバッファをレンダリングします。
    /// アクティブなフレームバッファに対し、毎フレーム呼ばれます。
    /// </summary>
    void Render(FrameBuffer frameBuffer);
}
