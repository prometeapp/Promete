using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing;

namespace Promete.Nodes.Renderer.GL;

/// <summary>
/// <see cref="MaskedContainer"/> のレンダリングを行います。
/// </summary>
public class GLMaskedContainerRenderer(
    PrometeApp app,
    IWindow window,
    GLMaskedContainerHelper maskHelper,
    ScissorStateTracker scissorTracker)
    : GLContainbleNodeRenderer(app, window, scissorTracker)
{
    public override void Collect(Node node, RenderCommandQueue queue)
    {
        if (node.IsDestroyed) return;
        var container = (MaskedContainer)node;

        // マスクなしの場合は通常のコンテナとして収集
        if (container.MaskTexture is not { } maskTexture)
        {
            base.Collect(node, queue);
            return;
        }

        if (container.UseAlphaMask)
        {
            queue.Enqueue(new BeginAlphaMaskCommand
            {
                Container = container,
                MaskTexture = maskTexture,
            });
            // BeginAlphaMaskCommandRunner が内部で子要素のレンダリングまで完結させる
        }
        else
        {
            queue.Enqueue(new BeginStencilMaskCommand
            {
                Container = container,
                MaskTexture = maskTexture,
            });

            if (container.IsTrimmable)
            {
                var (begin, end) = scissorTracker.Push(container, window);
                queue.Enqueue(begin);
                CollectChildren(container, queue);
                queue.Enqueue(end);
                scissorTracker.Pop();
            }
            else
            {
                CollectChildren(container, queue);
            }

            queue.Enqueue(new EndMaskCommand());
        }
    }
}
