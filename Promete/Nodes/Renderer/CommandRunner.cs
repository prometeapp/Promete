using System;
using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer;

/// <summary>
/// レンダリングコマンドを実行するランナーの基底クラスです。
/// </summary>
public abstract class CommandRunner
{
    /// <summary>このランナーが処理するコマンドの型。</summary>
    public abstract Type CommandType { get; }

    internal abstract void ExecuteUntyped(IRenderCommand command);
}

/// <summary>
/// 特定のコマンド型 <typeparamref name="T"/> を実行するランナーの基底クラスです。
/// </summary>
/// <typeparam name="T">処理対象のコマンド型。</typeparam>
public abstract class CommandRunner<T> : CommandRunner where T : IRenderCommand
{
    public override Type CommandType => typeof(T);

    internal override void ExecuteUntyped(IRenderCommand command) => Execute((T)command);

    /// <summary>コマンドを実行します。</summary>
    public abstract void Execute(T command);
}
