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
    private readonly List<IRenderCommand> _commands = [];
    private readonly Dictionary<Type, CommandRunner> _runners = new();

    /// <summary>
    /// コマンド型に対応するランナーを登録します。
    /// </summary>
    public void RegisterRunner<T>(CommandRunner<T> runner) where T : IRenderCommand
    {
        _runners[typeof(T)] = runner;
    }

    /// <summary>
    /// コマンドをキューに追加します。
    /// <see cref="DrawTextureCommand"/> は自動的に <see cref="DrawTextureBatchedCommand"/> へ集約されます。
    /// </summary>
    public void Enqueue(IRenderCommand command)
    {
        if (command is DrawTextureCommand texCmd)
        {
            // 末尾が同テクスチャのバッチコマンドならマージ
            if (_commands.Count > 0
                && _commands[^1] is DrawTextureBatchedCommand batch
                && batch.Texture.Handle == texCmd.Texture.Handle)
            {
                batch.Add(texCmd);
                return;
            }
            _commands.Add(new DrawTextureBatchedCommand(texCmd));
            return;
        }

        _commands.Add(command);
    }

    /// <summary>
    /// キューをクリアします。フレーム開始時に呼び出してください。
    /// </summary>
    public void Clear() => _commands.Clear();

    /// <summary>
    /// キューのコマンドを順に実行します。
    /// </summary>
    public void ProcessAndFlush()
    {
        foreach (var cmd in _commands)
        {
            if (_runners.TryGetValue(cmd.GetType(), out var runner))
                runner.ExecuteUntyped(cmd);
        }
        _commands.Clear();
    }
}
