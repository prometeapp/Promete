using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL;

public class GLPieSpriteRenderer(GLPieSpriteRendererHelper helper) : NodeRendererBase
{
    // PieSprite は専用シェーダーを使用するため、Collect() は基底クラスのデフォルト実装
    // （LegacyRenderCommand 経由）を使用する。
#pragma warning disable CS0672, CS0618
    public override void Render(Node node)
    {
        var pieSprite = (PieSprite)node;
        if (pieSprite.Texture is not { } tex) return;
        helper.Draw(tex, pieSprite, pieSprite.TintColor, pieSprite.StartPercent, pieSprite.Percent);
    }
#pragma warning restore CS0672, CS0618
}
