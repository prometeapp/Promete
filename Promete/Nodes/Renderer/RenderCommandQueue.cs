using System;
using System.Collections.Generic;
using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer;

/// <summary>
/// ノード走査フェーズで収集したレンダリングコマンドを管理し、
/// 登録された <see cref="CommandRunner{T}"/> を通じて実行するキューです。
/// バックエンド非依存で、ランナーの登録によって拡張できます。
/// </summary>
public class RenderCommandQueue
{
    private readonly record struct TrimState(int X, int Y, int Width, int Height, bool Enabled);

    private readonly Stack<DrawTextureBatchedCommand> _batchPool = new();
    private readonly Stack<List<IRenderCommand>> _listPool = new();
    private readonly Dictionary<Type, CommandRunner> _runners = new();
    private readonly Stack<List<IRenderCommand>> _scopeStack = new();
    private readonly Stack<TrimState> _trimStack = new();
    private List<IRenderCommand> _commands = [];

    /// <summary>
    /// trueのとき、<see cref="ProcessAndFlush"/> はコマンドを実行せずキューを破棄します。
    /// GL呼び出しを除いたループコストの計測に使用します。
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// コマンド型に対応するランナーを登録します。
    /// </summary>
    public void RegisterRunner<T>(CommandRunner<T> runner) where T : IRenderCommand
    {
        _runners[typeof(T)] = runner;
    }

    /// <summary>
    /// 複数のランナーをまとめて登録します。
    /// </summary>
    public void RegisterRunnerRange(params CommandRunner[] runners)
    {
        foreach (var runner in runners)
            _runners[runner.CommandType] = runner;
    }

    /// <summary>
    /// <see cref="DrawTextureCommand"/> をキューに追加します。ボックス化なしでバッチに集約されます。
    /// </summary>
    public void Enqueue(DrawTextureCommand texCmd)
    {
        if (_commands.Count > 0
            && _commands[^1] is DrawTextureBatchedCommand batch
            && batch.Texture.Handle == texCmd.Texture.Handle)
        {
            batch.Add(texCmd);
            return;
        }

        var newBatch = _batchPool.Count > 0 ? _batchPool.Pop() : new DrawTextureBatchedCommand();
        newBatch.Reset(texCmd);
        _commands.Add(newBatch);
    }

    /// <summary>
    /// コマンドをキューに追加します。
    /// </summary>
    public void Enqueue(IRenderCommand command)
    {
        _commands.Add(command);
    }

    /// <summary>
    /// トリム矩形をスタックにプッシュし、親との積集合を計算して BeginTrimCommand をキューに追加します。
    /// </summary>
    /// <param name="node">トリム対象のノード。</param>
    /// <param name="ctx">レンダリングコンテキスト。</param>
    public void PushTrim(ContainableNode node, RenderContext ctx)
    {
        var parent = _trimStack.TryPeek(out var p) ? p : new TrimState(0, 0, 0, 0, false);

        var left = (VectorInt)node.AbsoluteLocation;
        var size = (VectorInt)(node.Size * node.AbsoluteScale);

        if (left.X < 0) left.X = 0;
        if (left.Y < 0) left.Y = 0;

        if (left.X + size.X > ctx.ActualWidth)
            size.X = left.X + size.X - ctx.ActualWidth;

        if (left.Y + size.Y > ctx.ActualHeight)
            size.Y = left.Y + size.Y - ctx.ActualHeight;

        // OpenGL の Scissor は左下原点なので Y を反転
        var flippedY = ctx.ActualHeight - left.Y - size.Y;

        var sx = left.X * ctx.WindowScale;
        var sy = flippedY * ctx.WindowScale;
        var sw = size.X * ctx.WindowScale;
        var sh = size.Y * ctx.WindowScale;

        // 親のトリムが有効なら積集合を取る
        if (parent.Enabled)
        {
            var right = Math.Min(sx + sw, parent.X + parent.Width);
            var top = Math.Min(sy + sh, parent.Y + parent.Height);
            sx = Math.Max(sx, parent.X);
            sy = Math.Max(sy, parent.Y);
            sw = Math.Max(0, right - sx);
            sh = Math.Max(0, top - sy);
        }

        var newState = new TrimState((int)sx, (int)sy, (int)sw, (int)sh, true);
        _trimStack.Push(newState);

        Enqueue(new BeginTrimCommand
        {
            X = newState.X,
            Y = newState.Y,
            Width = newState.Width,
            Height = newState.Height,
        });
    }

    /// <summary>
    /// トリムスタックからポップし、EndTrimCommand をキューに追加します。
    /// </summary>
    public void PopTrim()
    {
        if (_trimStack.Count > 0) _trimStack.Pop();

        var parent = _trimStack.TryPeek(out var p) ? p : new TrimState(0, 0, 0, 0, false);
        Enqueue(new EndTrimCommand
        {
            X = parent.X,
            Y = parent.Y,
            Width = parent.Width,
            Height = parent.Height,
            WasEnabled = parent.Enabled,
        });
    }

    /// <summary>
    /// キューをクリアします。フレーム開始時に呼び出してください。
    /// </summary>
    public void Clear()
    {
        ReturnBatchesToPool(_commands);
        _commands.Clear();
        _trimStack.Clear();
    }

    /// <summary>
    /// FBOなどのサブレンダー用にスコープを開始します。
    /// 現在の <see cref="_commands"/> を退避し、新しいリストに切り替えます。
    /// </summary>
    public void PushScope()
    {
        _scopeStack.Push(_commands);
        _commands = _listPool.Count > 0 ? _listPool.Pop() : [];
    }

    /// <summary>
    /// スコープ内のコマンドを実行して終了します。外側のコマンドリストに影響しません。
    /// </summary>
    public void PopScopeAndFlush()
    {
        var scopeCommands = _commands;
        _commands = _scopeStack.Pop();

        foreach (var cmd in scopeCommands)
        {
            if (_runners.TryGetValue(cmd.GetType(), out var runner))
                runner.ExecuteUntyped(cmd);
        }

        ReturnBatchesToPool(scopeCommands);
        scopeCommands.Clear();
        _listPool.Push(scopeCommands);
    }

    /// <summary>
    /// キューのコマンドを順に実行します。
    /// </summary>
    public void ProcessAndFlush()
    {
        // ローカルキャプチャすることで、ランナー内でPushScope/PopScopeAndFlushが
        // 呼ばれても外側のイテレーションに影響しない
        var commands = _commands;
        foreach (var cmd in commands)
        {
            if (DryRun) continue;
            if (_runners.TryGetValue(cmd.GetType(), out var runner))
                runner.ExecuteUntyped(cmd);
        }

        ReturnBatchesToPool(commands);
        commands.Clear();
    }

    private void ReturnBatchesToPool(List<IRenderCommand> commands)
    {
        foreach (var cmd in commands)
            if (cmd is DrawTextureBatchedCommand batch)
                _batchPool.Push(batch);
    }
}
