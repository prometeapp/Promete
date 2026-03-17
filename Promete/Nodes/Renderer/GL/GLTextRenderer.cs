using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer.GL;

public class GLTextRenderer : NodeRendererBase
{
    public override void Collect(Node node, RenderCommandQueue queue)
    {
        var text = (Text)node;
        if (text.RenderedTexture is not { } tex) return;

        queue.Enqueue(new DrawTextureCommand
        {
            Texture = tex,
            ModelMatrix = text.ModelMatrix,
            TintColor = System.Drawing.Color.White,
            Width = text.Size.X,
            Height = text.Size.Y,
        });
    }
}
