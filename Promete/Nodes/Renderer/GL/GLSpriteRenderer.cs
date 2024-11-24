using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL;

public class GLSpriteRenderer(GLTextureRendererHelper helper) : NodeRendererBase
{
    public override void Render(Node node)
    {
        var sprite = (Sprite)node;
        if (sprite.Texture is not { } tex) return;
        helper.Draw(tex, sprite, sprite.TintColor);
    }
}
