using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer.GL;

public class GLSpriteRenderer : NodeRendererBase
{
    public override void Collect(Node node, RenderCommandQueue queue)
    {
        var sprite = (Sprite)node;
        if (sprite.Texture is not { } tex) return;

        queue.Enqueue(new DrawTextureCommand
        {
            Texture = tex,
            ModelMatrix = sprite.ModelMatrix,
            TintColor = sprite.TintColor,
            Width = sprite.Size.X,
            Height = sprite.Size.Y,
        });
    }
}
