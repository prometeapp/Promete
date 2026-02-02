using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL;

public class GLPieSpriteRenderer(GLPieSpriteRendererHelper helper) : NodeRendererBase
{
    public override void Render(Node node)
    {
        var pieSprite = (PieSprite)node;
        if (pieSprite.Texture is not { } tex) return;
        helper.Draw(tex, pieSprite, pieSprite.TintColor, pieSprite.StartPercent, pieSprite.Percent);
    }
}
