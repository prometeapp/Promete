using Promete.Nodes.Renderer.Commands;
using Promete.Windowing;

namespace Promete.Nodes.Renderer.GL;

public class GLContainbleNodeRenderer(PrometeApp app, IWindow window, ScissorStateTracker scissorTracker)
    : NodeRendererBase
{
    public override void Collect(Node node, RenderCommandQueue queue)
    {
        if (node.IsDestroyed) return;
        var container = (ContainableNode)node;

        if (container.isTrimmable)
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
    }

    protected void CollectChildren(ContainableNode container, RenderCommandQueue queue)
    {
        foreach (var child in container.sortedChildren)
            app.CollectNode(child, queue);
    }
}
