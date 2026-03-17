using System;
using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer;

public abstract class NodeRendererBase
{
    /// <summary>
    /// ノードのレンダリングコマンドをキューに積みます。
    /// 新しいレンダラーはこちらをオーバーライドしてください。
    /// </summary>
    public virtual void Collect(Node node, RenderCommandQueue queue)
    {
        // デフォルト実装: 旧 Render() を LegacyRenderCommand にラップして積む
        // LegacyRenderCommand は直接 GL を呼ぶため、バッチには混ぜられない
        queue.Enqueue(new LegacyRenderCommand { Renderer = this, Node = node });
    }

    /// <summary>
    /// ノードを描画します。
    /// </summary>
    /// <remarks>
    /// このメソッドは後方互換のために残されています。
    /// 新しいレンダラーは <see cref="Collect"/> をオーバーライドしてください。
    /// </remarks>
    [Obsolete("Collect() をオーバーライドしてください。")]
    public virtual void Render(Node node) { }
}
