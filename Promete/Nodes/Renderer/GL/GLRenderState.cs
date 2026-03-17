using System.Collections.Generic;

namespace Promete.Nodes.Renderer.GL;

/// <summary>
/// GL描画フェーズで複数のランナー間を跨いで共有する描画状態です。
/// </summary>
public class GLRenderState
{
    /// <summary>ステンシルテスト有効状態のスタック（BeginStencilMask/EndMask で使用）</summary>
    public Stack<bool> StencilStateStack { get; } = new();
}
