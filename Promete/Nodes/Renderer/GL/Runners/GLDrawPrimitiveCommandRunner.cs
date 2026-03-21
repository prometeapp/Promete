using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="DrawPrimitiveCommand"/> でプリミティブ図形を描画するランナーです。
/// </summary>
public class GLDrawPrimitiveCommandRunner(GLPrimitiveRendererHelper helper)
    : CommandRunner<DrawPrimitiveCommand>
{
    public override void Execute(DrawPrimitiveCommand command)
    {
        helper.Draw(command.WorldVertices, command.ShapeType,
            command.Color, command.LineWidth, command.LineColor);
    }
}
