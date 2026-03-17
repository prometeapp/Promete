using System;
using System.Collections.Generic;
using Promete.Nodes.Renderer.Commands;
using Promete.Windowing;

namespace Promete.Nodes.Renderer.GL;

/// <summary>
/// コレクトフェーズ（GL呼び出しなし）でシザー矩形をCPUサイドのスタックで管理します。
/// </summary>
public class ScissorStateTracker
{
    private readonly record struct ScissorState(int X, int Y, int Width, int Height, bool Enabled);

    private readonly Stack<ScissorState> _stack = new();

    /// <summary>
    /// フレーム開始時にスタックをリセットします。
    /// </summary>
    public void Clear()
    {
        _stack.Clear();
    }

    /// <summary>
    /// ノードのシザー矩形を計算してスタックにプッシュし、コマンドペアを返します。
    /// </summary>
    /// <param name="node">シザー対象のノード。</param>
    /// <param name="window">ウィンドウ情報（スケール・サイズ取得用）。</param>
    /// <returns>
    /// (beginCmd, endCmd) のペア。
    /// beginCmd は今から有効にするシザー矩形、endCmd はポップ時に復元する情報を持ちます。
    /// </returns>
    public (BeginScissorCommand Begin, EndScissorCommand End) Push(ContainableNode node, IWindow window)
    {
        // 現在のスタックトップを「親」状態として取得
        var parent = _stack.TryPeek(out var p) ? p : new ScissorState(0, 0, 0, 0, false);

        // ノードのワールド座標・サイズからシザー矩形を計算
        var left = (VectorInt)node.AbsoluteLocation;
        var size = (VectorInt)(node.Size * node.AbsoluteScale);

        if (left.X < 0) left.X = 0;
        if (left.Y < 0) left.Y = 0;

        if (left.X + size.X > window.ActualWidth)
            size.X = left.X + size.X - window.ActualWidth;

        if (left.Y + size.Y > window.ActualHeight)
            size.Y = left.Y + size.Y - window.ActualHeight;

        // OpenGL の Scissor は左下原点なので Y を反転
        var flippedY = window.ActualHeight - left.Y - size.Y;

        var sx = left.X * window.Scale;
        var sy = flippedY * window.Scale;
        var sw = size.X * window.Scale;
        var sh = size.Y * window.Scale;

        // 親のシザーが有効なら積集合を取る
        if (parent.Enabled)
        {
            var right = Math.Min(sx + sw, parent.X + parent.Width);
            var top = Math.Min(sy + sh, parent.Y + parent.Height);
            sx = Math.Max(sx, parent.X);
            sy = Math.Max(sy, parent.Y);
            sw = Math.Max(0, right - sx);
            sh = Math.Max(0, top - sy);
        }

        var newState = new ScissorState((int)sx, (int)sy, (int)sw, (int)sh, true);
        _stack.Push(newState);

        var begin = new BeginScissorCommand
        {
            X = newState.X,
            Y = newState.Y,
            Width = newState.Width,
            Height = newState.Height,
        };

        var end = new EndScissorCommand
        {
            X = parent.X,
            Y = parent.Y,
            Width = parent.Width,
            Height = parent.Height,
            WasEnabled = parent.Enabled,
        };

        return (begin, end);
    }

    /// <summary>
    /// スタックから現在のシザー状態をポップします。
    /// </summary>
    public void Pop()
    {
        if (_stack.Count > 0) _stack.Pop();
    }
}
